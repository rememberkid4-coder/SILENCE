using UnityEngine;

namespace Silence
{
    // Physically moves the body opposite to anchored controller movement.
    public sealed class Climbable : MonoBehaviour { }

    public sealed class ClimbInteractor : MonoBehaviour
    {
        public QuestRig rig;
        private int gripHand = -1;
        private Vector3 anchor;
        private void Start() { if (!rig) rig = GetComponent<QuestRig>(); }
        private void LateUpdate()
        {
            if (!rig || rig.IsPaused) { Release(); return; }
            if (gripHand < 0)
            {
                if (rig.LeftGripDown && CheckHand(rig.leftHand)) Begin(0, rig.leftHand);
                else if (rig.RightGripDown && CheckHand(rig.rightHand)) Begin(1, rig.rightHand);
            }
            if (gripHand < 0) return;
            bool pressed = gripHand == 0 ? rig.LeftGripPressed : rig.RightGripPressed;
            if (!pressed) { Release(); return; }
            Transform hand = gripHand == 0 ? rig.leftHand : rig.rightHand;
            Vector3 delta = anchor - hand.position;
            rig.ClimbMove(delta);
        }
        private bool CheckHand(Transform hand)
        {
            foreach (Collider c in Physics.OverlapSphere(hand.position, .22f))
                if (c.GetComponentInParent<Climbable>()) return true;
            return false;
        }
        private void Begin(int hand, Transform t)
        {
            gripHand = hand; anchor = t.position;
            rig.IsClimbing = true;
        }
        private void Release()
        {
            gripHand = -1;
            if (rig) rig.IsClimbing = false;
        }
    }
}
