using UnityEngine;

namespace Silence
{
    public sealed class WristDisplay : MonoBehaviour
    {
        public static WristDisplay Instance { get; private set; }
        public TextMesh text;
        public Transform viewer;
        private string message = "SILENCE...";
        private float until;
        private void Awake() { Instance = this; }
        public void Notify(string value, float seconds = 4f)
        {
            message = value;
            until = Time.time + seconds;
        }
        private void LateUpdate()
        {
            if (!text) return;
            string status = RelaySession.Instance ? RelaySession.Instance.Status : "Offline";
            int chapter = GameDirector.Instance ? GameDirector.Instance.Chapter + 1 : 1;
            int fuses = NetworkAvatar.Local ? NetworkAvatar.Local.Fuses : 0;
            text.text = "SILENCE... / " + chapter.ToString("00") + "\n" +
                        "Fuses " + fuses + " | " + (Time.time < until ? message : status) + "\n" +
                        "Right A: torch / Left B: menu";
            if (viewer) transform.rotation = Quaternion.LookRotation(transform.position - viewer.position, Vector3.up);
        }
    }
}
