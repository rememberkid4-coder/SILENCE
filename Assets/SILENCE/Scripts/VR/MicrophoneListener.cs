using UnityEngine;

namespace Silence
{
    // Opt-in amplitude ONLY. Raw microphone audio is never transmitted or saved.
    // The server receives a quantized sound intensity via NetworkAvatar.
    public sealed class MicrophoneListener : MonoBehaviour
    {
        public bool EnabledByPlayer { get; private set; }
        public float rmsThreshold = 0.08f;
        private AudioClip capture;
        private float nextCheck;
        private readonly float[] samples = new float[512];

        public void SetEnabled(bool enabled)
        {
            if (!enabled) { Stop(); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            const string permission = "android.permission.RECORD_AUDIO";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                UnityEngine.Android.Permission.RequestUserPermission(permission);
                WristDisplay.Instance?.Notify("Allow mic access, then enable the option again.", 6f);
                return;
            }
#endif
            if (Microphone.devices.Length == 0) { WristDisplay.Instance?.Notify("No microphone available."); return; }
            capture = Microphone.Start(null, true, 1, 16000);
            EnabledByPlayer = capture != null;
            WristDisplay.Instance?.Notify(EnabledByPlayer ? "Mic noise detection ON" : "Microphone unavailable");
        }
        private void Stop()
        {
            if (Microphone.IsRecording(null)) Microphone.End(null);
            capture = null;
            EnabledByPlayer = false;
            WristDisplay.Instance?.Notify("Mic noise detection OFF");
        }
        private void OnDisable() { if (EnabledByPlayer) Stop(); }
        private void Update()
        {
            if (!EnabledByPlayer || capture == null || Time.time < nextCheck) return;
            nextCheck = Time.time + 0.23f;
            int pos = Microphone.GetPosition(null);
            if (pos < samples.Length) return;
            capture.GetData(samples, pos - samples.Length);
            float power = 0f;
            foreach (float s in samples) power += s * s;
            float rms = Mathf.Sqrt(power / samples.Length);
            if (rms > rmsThreshold)
                NetworkAvatar.Local?.EmitNoise(Mathf.Clamp((rms - rmsThreshold) * 140f, 4, 32), SoundType.Voice);
        }
    }
}
