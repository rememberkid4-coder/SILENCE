using UnityEngine;

namespace Silence
{
    public sealed class VRInteractor : MonoBehaviour
    {
        public QuestRig rig;
        public Light flashlight;
        public float reach = 3.6f;
        private NetworkProp heldLeft, heldRight;
        private Vector3 previousLeft, previousRight;
        private float lastSend;
        private void Start()
        {
            if (!rig) rig = QuestRig.Local;
            if (rig) { previousLeft = rig.leftHand.position; previousRight = rig.rightHand.position; }
        }

        private void Update()
        {
            if (!rig || rig.IsPaused || NetworkAvatar.Local == null) return;
            if (rig.RightPrimaryDown && flashlight) flashlight.enabled = !flashlight.enabled;
            if (rig.RightTriggerDown) TryInteract(rig.rightHand);
            if (rig.LeftTriggerDown) TryInteract(rig.leftHand);
            UpdateHand(rig.leftHand, rig.LeftGripDown, rig.LeftGripPressed, ref heldLeft, ref previousLeft);
            UpdateHand(rig.rightHand, rig.RightGripDown, rig.RightGripPressed, ref heldRight, ref previousRight);
        }

        private void TryInteract(Transform hand)
        {
            if (Physics.SphereCast(hand.position, 0.07f, hand.forward, out var hit, reach,
                ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<InteractiveObject>();
                if (target && target.CanInteract && !(target is NetworkProp))
                    target.Interact(NetworkAvatar.Local);
            }
        }

        private void UpdateHand(Transform hand, bool began, bool pressed, ref NetworkProp held,
            ref Vector3 previous)
        {
            if (began && held == null)
            {
                Collider[] nearby = Physics.OverlapSphere(hand.position, 0.27f, ~0, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                foreach (var collider in nearby)
                {
                    var candidate = collider.GetComponentInParent<NetworkProp>();
                    if (!candidate || !candidate.CanInteract || candidate.IsHeld) continue;
                    float dist = Vector3.Distance(hand.position, candidate.transform.position);
                    if (dist >= best) continue;
                    best = dist; held = candidate;
                }
                held?.Grab();
            }
            if (held != null)
            {
                if (!pressed)
                {
                    Vector3 velocity = (hand.position - previous) / Mathf.Max(0.01f, Time.deltaTime);
                    held.Release(Vector3.ClampMagnitude(velocity, 8f));
                    held = null;
                }
                else if (Time.time >= lastSend)
                {
                    held.Place(hand.position, hand.rotation);
                    lastSend = Time.time + 0.035f;
                }
            }
            previous = hand.position;
        }
    }
}
