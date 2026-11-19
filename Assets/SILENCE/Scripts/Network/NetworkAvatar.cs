using Unity.Netcode;
using UnityEngine;

namespace Silence
{
    // Visible avatar for other players; the local player always uses their native XR rig.
    public sealed class NetworkAvatar : NetworkBehaviour
    {
        public static NetworkAvatar Local { get; private set; }
        public Transform visualHead, visualLeft, visualRight;
        public Renderer[] hideForOwner;
        private readonly NetworkVariable<Vector3> headPos = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Quaternion> headRot = new NetworkVariable<Quaternion>();
        private readonly NetworkVariable<Vector3> leftPos = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Quaternion> leftRot = new NetworkVariable<Quaternion>();
        private readonly NetworkVariable<Vector3> rightPos = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Quaternion> rightRot = new NetworkVariable<Quaternion>();
        private readonly NetworkVariable<int> carriedFuses = new NetworkVariable<int>();
        private readonly NetworkVariable<int> recordings = new NetworkVariable<int>();
        private float transmitAt;
        private float lastSoundAt;

        public int Fuses => carriedFuses.Value;
        public int Recordings => recordings.Value;
        public Vector3 AuthoritativeHead => headPos.Value;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Local = this;
                foreach (var renderer in hideForOwner) if (renderer) renderer.enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Local == this) Local = null;
        }

        private void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner && QuestRig.Local != null && Time.time >= transmitAt)
            {
                transmitAt = Time.time + 0.065f;
                var rig = QuestRig.Local;
                SubmitPoseRpc(rig.HeadWorldPosition, rig.head.rotation,
                    rig.leftHand.position, rig.leftHand.rotation,
                    rig.rightHand.position, rig.rightHand.rotation);
            }
            if (IsOwner) return;
            const float blend = 15f;
            float t = 1f - Mathf.Exp(-blend * Time.deltaTime);
            if (visualHead) { visualHead.position = Vector3.Lerp(visualHead.position, headPos.Value, t); visualHead.rotation = Quaternion.Slerp(visualHead.rotation, headRot.Value, t); }
            if (visualLeft) { visualLeft.position = Vector3.Lerp(visualLeft.position, leftPos.Value, t); visualLeft.rotation = Quaternion.Slerp(visualLeft.rotation, leftRot.Value, t); }
            if (visualRight) { visualRight.position = Vector3.Lerp(visualRight.position, rightPos.Value, t); visualRight.rotation = Quaternion.Slerp(visualRight.rotation, rightRot.Value, t); }
        }

        [Rpc(SendTo.Server)]
        private void SubmitPoseRpc(Vector3 head, Quaternion hRot, Vector3 lHand, Quaternion lRot, Vector3 rHand, Quaternion rRot)
        {
            // Restrict invalid remote poses; precise movement validation needs further tuning.
            if (Vector3.Distance(head, headPos.Value) > 8f && headPos.Value != Vector3.zero) return;
            if (Vector3.Distance(lHand, head) > 2.6f || Vector3.Distance(rHand, head) > 2.6f) return;
            if (float.IsNaN(head.x) || float.IsInfinity(head.x) ||
                float.IsNaN(head.y) || float.IsInfinity(head.y) ||
                float.IsNaN(head.z) || float.IsInfinity(head.z)) return;
            headPos.Value = head; headRot.Value = hRot;
            leftPos.Value = lHand; leftRot.Value = lRot;
            rightPos.Value = rHand; rightRot.Value = rRot;
            transform.position = head;
        }

        public void EmitNoise(float intensity, SoundType kind)
        {
            if (!IsOwner || Time.time - lastSoundAt < 0.14f) return;
            lastSoundAt = Time.time;
            EmitNoiseRpc(Mathf.Clamp(intensity, 0, 40), kind, QuestRig.Local.HeadWorldPosition);
        }

        [Rpc(SendTo.Server)]
        private void EmitNoiseRpc(float intensity, SoundType kind, Vector3 position)
        {
            if (Vector3.Distance(position, headPos.Value) > 3f) return;
            SoundBus.Emit(position, Mathf.Clamp(intensity, 0, 40), kind);
        }

        public void WarpOnServer(Vector3 checkpoint)
        {
            if (!IsServer) return;
            headPos.Value = checkpoint + Vector3.up * 1.6f;
            transform.position = headPos.Value;
        }

        public void AddFuse()
        {
            if (IsServer) carriedFuses.Value = Mathf.Min(10, carriedFuses.Value + 1);
        }
        public bool ConsumeFuses(int count)
        {
            if (!IsServer || carriedFuses.Value < count) return false;
            carriedFuses.Value -= count;
            return true;
        }
        public void AddRecording()
        {
            if (IsServer) recordings.Value = Mathf.Min(50, recordings.Value + 1);
        }
    }
}
