using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Silence
{
    public enum SoundType { Footstep, Sprint, Impact, Glass, Door, Voice, Distraction, Machinery }

    public readonly struct SoundSignal
    {
        public readonly Vector3 Position;
        public readonly float Intensity;
        public readonly SoundType Kind;
        public readonly float Timestamp;
        public SoundSignal(Vector3 position, float intensity, SoundType kind)
        {
            Position = position; Intensity = Mathf.Clamp(intensity, 0, 100);
            Kind = kind; Timestamp = Time.time;
        }
    }

    // All AI-relevant stimuli run on the authoritative host, not on each client.
    public static class SoundBus
    {
        public static event Action<SoundSignal> Heard;
        private static readonly List<SoundSignal> Memory = new List<SoundSignal>();
        public static IReadOnlyList<SoundSignal> Recent => Memory;

        public static void Emit(Vector3 worldPosition, float loudness, SoundType kind)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;
            var signal = new SoundSignal(worldPosition, loudness, kind);
            Memory.Add(signal);
            if (Memory.Count > 64) Memory.RemoveAt(0);
            Heard?.Invoke(signal);
        }

        public static void Clear() => Memory.Clear();
    }

    public sealed class ImpactNoise : MonoBehaviour
    {
        public float threshold = 1.5f;
        public float loudnessMultiplier = 3.5f;
        private float lastImpact;
        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time - lastImpact < 0.3f) return;
            float impact = collision.relativeVelocity.magnitude;
            if (impact < threshold) return;
            lastImpact = Time.time;
            SoundBus.Emit(transform.position, Mathf.Min(65, impact * loudnessMultiplier),
                gameObject.name.ToLowerInvariant().Contains("glass") ? SoundType.Glass : SoundType.Impact);
        }
    }
}
