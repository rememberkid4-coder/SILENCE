using System;
using System.IO;
using UnityEngine;
using Unity.Netcode;

namespace Silence
{
    [Serializable]
    public sealed class ProgressSave
    {
        public int chapter;
        public bool power;
        public float x, y, z;
        public int recordings;
        public string version = "slice-1";
    }

    public sealed class GameDirector : NetworkBehaviour
    {
        public static GameDirector Instance { get; private set; }
        public Transform[] checkpointPositions;
        public Transform[] chapterEntrances;
        public string[] chapters = { "01 / AWAKENING", "02 / THE STREETS", "03 / THE TOWER",
            "04 / APARTMENTS", "05 / BELOW", "06 / WARD ZERO", "07 / EMPTY MALL",
            "08 / THE INTERSTATE", "09 / PRECINCT", "10 / THE TRANSMISSION" };
        private readonly NetworkVariable<int> chapter = new NetworkVariable<int>();
        private readonly NetworkVariable<bool> power = new NetworkVariable<bool>();
        private readonly NetworkVariable<bool> campaignComplete = new NetworkVariable<bool>();
        public bool PowerRestored => power.Value;
        public int Chapter => chapter.Value;
        public bool Finished => campaignComplete.Value;
        private string SavePath => Path.Combine(Application.persistentDataPath, "silence_campaign.json");

        private void Awake() { Instance = this; }
        public override void OnNetworkSpawn()
        {
            chapter.OnValueChanged += ChapterChanged;
            power.OnValueChanged += PowerChanged;
            campaignComplete.OnValueChanged += FinishedChanged;
            if (IsServer) Load();
            WristDisplay.Instance?.Notify("RADIO: ...if anyone hears this... follow the signal...", 12f);
        }
        public override void OnNetworkDespawn()
        {
            chapter.OnValueChanged -= ChapterChanged;
            power.OnValueChanged -= PowerChanged;
            campaignComplete.OnValueChanged -= FinishedChanged;
        }
        private void ChapterChanged(int oldValue, int newValue)
        {
            string title = newValue < chapters.Length ? chapters[newValue] : "UNKNOWN";
            WristDisplay.Instance?.Notify(title, 7f);
        }
        private void PowerChanged(bool oldValue, bool newValue)
        {
            if (newValue) WristDisplay.Instance?.Notify("Power restored. The locked doors release.", 8f);
        }
        private void FinishedChanged(bool oldValue, bool newValue)
        {
            if (newValue) WristDisplay.Instance?.Notify(
                "TRANSMISSION FOUND: They were listening to us all along. You are not alone. / END", 20f);
        }

        public void RestorePower()
        {
            if (!IsServer) return;
            power.Value = true;
            if (chapter.Value >= 9) campaignComplete.Value = true;
            Save();
        }
        public void ReachChapter(int index)
        {
            ReachChapterRpc(index);
        }
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void ReachChapterRpc(int index, RpcParams rpcParams = default)
        {
            if (index < 0 || index >= chapters.Length || index > chapter.Value + 1) return;
            if (chapterEntrances != null && index < chapterEntrances.Length && chapterEntrances[index] &&
                !WorldSecurity.PlayerNear(rpcParams.Receive.SenderClientId, chapterEntrances[index].position, 17f)) return;
            if (index > chapter.Value) { chapter.Value = index; Save(); }
            if (index == 9 && power.Value)
            {
                // Ending unlock requires the communications equipment to be powered.
                campaignComplete.Value = true;
                Save();
            }
        }
        public void CatchPlayer(ulong clientId)
        {
            if (!IsServer) return;
            Vector3 spawn = CheckpointLocation();
            WorldSecurity.Avatar(clientId)?.WarpOnServer(spawn);
            ResetPlayerRpc(clientId, spawn);
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetPlayerRpc(ulong clientId, Vector3 destination)
        {
            if (!NetworkManager.Singleton || NetworkManager.Singleton.LocalClientId != clientId) return;
            QuestRig.Local?.TeleportToCheckpoint(destination);
            WristDisplay.Instance?.Notify("The Listener found you. Returning to shelter.", 7f);
        }
        public Vector3 CheckpointLocation()
        {
            int index = Mathf.Min(chapter.Value, checkpointPositions == null ? 0 : checkpointPositions.Length - 1);
            return index >= 0 && checkpointPositions[index] ? checkpointPositions[index].position : new Vector3(0, 0.5f, 0);
        }
        public void Save()
        {
            if (!IsServer) return;
            try
            {
                var p = CheckpointLocation();
                var save = new ProgressSave { chapter = chapter.Value, power = power.Value, x = p.x, y = p.y, z = p.z };
                File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            }
            catch (Exception ex) { Debug.LogWarning("Save failed: " + ex.Message); }
        }
        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var data = JsonUtility.FromJson<ProgressSave>(File.ReadAllText(SavePath));
                chapter.Value = Mathf.Clamp(data.chapter, 0, chapters.Length - 1);
                power.Value = data.power;
            }
            catch (Exception ex) { Debug.LogWarning("Load failed: " + ex.Message); }
        }
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class ChapterTrigger : MonoBehaviour
    {
        public int chapterIndex;
        private float lastTime;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<QuestRig>() == null || Time.time - lastTime < 2f) return;
            lastTime = Time.time;
            GameDirector.Instance?.ReachChapter(chapterIndex);
        }
    }
}
