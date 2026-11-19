using UnityEngine;

namespace Silence
{
    // Aggressive distance-based visual culling; collision and gameplay objects remain live.
    public sealed class DistrictCuller : MonoBehaviour
    {
        public Transform visualRoot;
        public float showWithin = 125f;
        private Renderer[] renderers;
        private float nextCheck;
        private bool shown = true;
        private void Start()
        {
            if (!visualRoot) visualRoot = transform;
            renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        }
        private void Update()
        {
            if (Time.time < nextCheck || !QuestRig.Local) return;
            nextCheck = Time.time + 0.45f;
            bool visible = Vector3.Distance(QuestRig.Local.transform.position, transform.position) < showWithin;
            if (visible == shown) return;
            shown = visible;
            foreach (var renderer in renderers) if (renderer) renderer.enabled = shown;
        }
    }
}
