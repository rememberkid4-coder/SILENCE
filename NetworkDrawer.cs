using Unity.Netcode;
using UnityEngine;

namespace Silence
{
    public sealed class NetworkDrawer : InteractiveObject
    {
        public Transform drawer;
        public Vector3 openOffset = new Vector3(0, 0, -.65f);
        private Vector3 closedPosition;
        private readonly NetworkVariable<bool> opened = new NetworkVariable<bool>();
        private void Awake() => closedPosition = drawer ? drawer.localPosition : Vector3.zero;
        private void Update()
        {
            if (!drawer) return;
            drawer.localPosition = Vector3.Lerp(drawer.localPosition,
                closedPosition + (opened.Value ? openOffset : Vector3.zero),
                1f - Mathf.Exp(-8f * Time.deltaTime));
        }
        public override void Interact(NetworkAvatar actor) => ToggleRpc();
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void ToggleRpc(RpcParams rpcParams = default)
        {
            if (!WorldSecurity.PlayerNear(rpcParams.Receive.SenderClientId, transform.position, 3f)) return;
            opened.Value = !opened.Value;
            SoundBus.Emit(transform.position, 7, SoundType.Door);
        }
    }
}
