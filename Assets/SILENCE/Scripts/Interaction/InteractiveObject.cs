using UnityEngine;
using Unity.Netcode;

namespace Silence
{
    // The ray interaction and physical gripping share the same target selection system.
    public abstract class InteractiveObject : NetworkBehaviour
    {
        [TextArea] public string prompt = "Interact";
        public virtual bool CanInteract => IsSpawned;
        public virtual void Interact(NetworkAvatar actor) { }
    }

    public sealed class NetworkDoor : InteractiveObject
    {
        public Transform hinge;
        public float openAngle = 95f;
        public bool requiresPower;
        public bool startsOpen;
        private readonly NetworkVariable<bool> opened = new NetworkVariable<bool>();
        private Quaternion closedRotation;
        private void Awake() { closedRotation = hinge ? hinge.localRotation : transform.localRotation; }
        public override void OnNetworkSpawn()
        {
            if (IsServer) opened.Value = startsOpen;
        }
        private void Update()
        {
            Transform target = hinge ? hinge : transform;
            target.localRotation = Quaternion.Slerp(target.localRotation,
                closedRotation * Quaternion.Euler(0, opened.Value ? openAngle : 0, 0), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
        public override void Interact(NetworkAvatar actor)
        {
            if (!actor || !IsSpawned) return;
            ToggleDoorRpc();
        }
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void ToggleDoorRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!WorldSecurity.PlayerNear(sender, transform.position, 3.5f)) return;
            if (requiresPower && GameDirector.Instance != null && !GameDirector.Instance.PowerRestored) return;
            opened.Value = !opened.Value;
            SoundBus.Emit(transform.position, 14, SoundType.Door);
        }
    }

    public sealed class Pickup : InteractiveObject
    {
        public enum Type { Fuse, Recording }
        public Type itemType;
        [TextArea] public string recordingText = "The radios keep whispering. No one answers.";
        private readonly NetworkVariable<bool> taken = new NetworkVariable<bool>();
        private Renderer[] renderers;
        private Collider[] colliders;
        private void Awake() { renderers = GetComponentsInChildren<Renderer>(); colliders = GetComponentsInChildren<Collider>(); }
        private void Update()
        {
            foreach (var r in renderers) if (r) r.enabled = !taken.Value;
            foreach (var c in colliders) if (c) c.enabled = !taken.Value;
        }
        public override bool CanInteract => base.CanInteract && !taken.Value;
        public override void Interact(NetworkAvatar actor) => CollectRpc();
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void CollectRpc(RpcParams rpcParams = default)
        {
            if (taken.Value) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!WorldSecurity.PlayerNear(sender, transform.position, 2.8f)) return;
            var player = WorldSecurity.Avatar(sender);
            if (!player) return;
            taken.Value = true;
            if (itemType == Type.Fuse) player.AddFuse();
            else player.AddRecording();
            CollectMessageRpc(sender, itemType == Type.Fuse ? "Fuse acquired" : recordingText);
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void CollectMessageRpc(ulong client, string message)
        {
            if (NetworkManager.Singleton.LocalClientId == client)
                WristDisplay.Instance?.Notify(message, 8f);
        }
    }

    public sealed class PowerPanel : InteractiveObject
    {
        public int fusesRequired = 2;
        public override void Interact(NetworkAvatar actor) => InstallFusesRpc();
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void InstallFusesRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!WorldSecurity.PlayerNear(sender, transform.position, 3f)) return;
            if (GameDirector.Instance == null || GameDirector.Instance.PowerRestored) return;
            var player = WorldSecurity.Avatar(sender);
            if (player == null || !player.ConsumeFuses(fusesRequired)) { FailureRpc(sender); return; }
            GameDirector.Instance.RestorePower();
            SoundBus.Emit(transform.position, 28f, SoundType.Machinery);
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void FailureRpc(ulong client)
        {
            if (NetworkManager.Singleton.LocalClientId == client)
                WristDisplay.Instance?.Notify("Need " + fusesRequired + " fuses", 4f);
        }
    }

    public static class WorldSecurity
    {
        public static NetworkAvatar Avatar(ulong clientId)
        {
            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) return null;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
            return client.PlayerObject ? client.PlayerObject.GetComponent<NetworkAvatar>() : null;
        }
        public static bool PlayerNear(ulong clientId, Vector3 point, float radius)
        {
            var a = Avatar(clientId);
            return a && Vector3.Distance(a.AuthoritativeHead, point) < radius;
        }
    }
}
