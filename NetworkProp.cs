using UnityEngine;
using Unity.Netcode;

namespace Silence
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NetworkProp : InteractiveObject
    {
        private readonly NetworkVariable<ulong> holder = new NetworkVariable<ulong>(ulong.MaxValue);
        private readonly NetworkVariable<Vector3> netPos = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Quaternion> netRot = new NetworkVariable<Quaternion>();
        private Rigidbody body;
        private float publishAt;
        private float nextMove;
        public bool IsHeld => holder.Value != ulong.MaxValue;
        private void Awake() { body = GetComponent<Rigidbody>(); }

        public override void OnNetworkSpawn()
        {
            body.isKinematic = !IsServer;
            if (IsServer) { netPos.Value = transform.position; netRot.Value = transform.rotation; }
        }
        private void FixedUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer)
            {
                if (Time.time < publishAt) return;
                publishAt = Time.time + 0.075f;
                netPos.Value = transform.position;
                netRot.Value = transform.rotation;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, netPos.Value, 0.7f);
                transform.rotation = Quaternion.Slerp(transform.rotation, netRot.Value, 0.7f);
            }
        }
        public void Grab() => GrabRpc();
        public void Place(Vector3 position, Quaternion rotation) => MoveHeldRpc(position, rotation);
        public void Release(Vector3 velocity) => ReleaseRpc(velocity);

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void GrabRpc(RpcParams rpcParams = default)
        {
            ulong user = rpcParams.Receive.SenderClientId;
            if (IsHeld || !WorldSecurity.PlayerNear(user, transform.position, 2.8f)) return;
            holder.Value = user;
            body.isKinematic = true;
            SoundBus.Emit(transform.position, 5, SoundType.Impact);
        }
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void MoveHeldRpc(Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
        {
            if (holder.Value != rpcParams.Receive.SenderClientId) return;
            if (Time.time < nextMove || Vector3.Distance(position, transform.position) > 1.2f ||
                !WorldSecurity.PlayerNear(holder.Value, position, 3f)) return;
            nextMove = Time.time + 0.025f;
            body.MovePosition(position);
            body.MoveRotation(rotation);
        }
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void ReleaseRpc(Vector3 velocity, RpcParams rpcParams = default)
        {
            if (holder.Value != rpcParams.Receive.SenderClientId) return;
            holder.Value = ulong.MaxValue;
            body.isKinematic = false;
            body.linearVelocity = Vector3.ClampMagnitude(velocity, 8f);
            SoundBus.Emit(transform.position, Mathf.Clamp(velocity.magnitude * 3f, 3, 25), SoundType.Distraction);
        }
    }
}
