using UnityEngine;
using UnityEngine.XR;

namespace Silence
{
    public sealed class QuestPerformance : MonoBehaviour
    {
        public int targetFps = 72;
        public float eyeScale = 1f;
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFps;
            QualitySettings.shadowDistance = 18f;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            if (XRSettings.enabled) XRSettings.eyeTextureResolutionScale = Mathf.Clamp(eyeScale, .75f, 1.1f);
            // Platform-specific fixed foveation must be enabled via a supported Meta/OpenXR plugin setting.
            // This class does not falsely claim 72Hz is guaranteed.
        }
    }
}
