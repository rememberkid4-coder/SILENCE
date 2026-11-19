using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace Silence
{
    public sealed class ListenerAI : NetworkBehaviour
    {
        public enum Mood { Wandering, Investigating, Searching, Hunting }
        public NavMeshAgent agent;
        public Transform face;
        public Transform[] limbs;
        public float hearingSensitivity = 1.1f;
        public float patrolRadius = 35f;
        public float hearingThreshold = 0.035f;
        public float catchRadius = 1.2f;

        private readonly NetworkVariable<Vector3> syncedPosition = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Quaternion> syncedRotation = new NetworkVariable<Quaternion>();
        private readonly NetworkVariable<int> syncedMood = new NetworkVariable<int>();
        private readonly Queue<SoundSignal> history = new Queue<SoundSignal>();
        private Vector3 lastKnownLocation;
        private Vector3 spawn;
        private float interest;
        private float nextThink;
        private float nextSync;
        private float searchUntil;
        private float nextCatch;
        private float nextGrowl;
        private AudioSource growl;
        public Mood CurrentMood => (Mood)syncedMood.Value;

        private void Awake()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();
            spawn = transform.position;
            growl = GetComponent<AudioSource>();
            if (!growl) growl = gameObject.AddComponent<AudioSource>();
            growl.spatialBlend = 1;
            growl.minDistance = 1.5f;
            growl.maxDistance = 22f;
            growl.rolloffMode = AudioRolloffMode.Logarithmic;
            growl.playOnAwake = false;
            growl.clip = HorrorSynth.MakeCreatureCall();
        }

        public override void OnNetworkSpawn()
        {
            if (agent) agent.enabled = IsServer;
            if (IsServer)
            {
                SoundBus.Heard += ReceiveSound;
                syncedPosition.Value = transform.position;
                syncedRotation.Value = transform.rotation;
                SetMood(Mood.Wandering);
            }
        }
        public override void OnNetworkDespawn() => SoundBus.Heard -= ReceiveSound;

        private void ReceiveSound(SoundSignal signal)
        {
            if (!IsServer || !agent.enabled || !agent.isOnNavMesh) return;
            float distance = Vector3.Distance(transform.position, signal.Position);
            float obstruction = Physics.Linecast(transform.position + Vector3.up * 0.9f,
                signal.Position + Vector3.up * 1f, ~0, QueryTriggerInteraction.Ignore) ? 0.55f : 1f;
            float perceived = signal.Intensity * hearingSensitivity * obstruction / Mathf.Max(8, distance * distance);
            if (perceived < hearingThreshold) return;
            history.Enqueue(signal);
            while (history.Count > 5) history.Dequeue();
            if (perceived < interest * 0.55f && CurrentMood == Mood.Hunting) return;
            interest = perceived;
            lastKnownLocation = signal.Position;
            searchUntil = Time.time + Mathf.Clamp(4f + perceived * 2, 4f, 20f);
            SetMood(perceived > 0.27f ? Mood.Hunting : Mood.Investigating);
            agent.speed = CurrentMood == Mood.Hunting ? 3.6f : 2.0f;
            GoTo(lastKnownLocation);
        }

        private void Update()
        {
            AnimateBody();
            if (!IsSpawned) return;
            if (!IsServer)
            {
                float t = 1f - Mathf.Exp(-12f * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, syncedPosition.Value, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, syncedRotation.Value, t);
                return;
            }
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.4f;
                Think();
            }
            if (Time.time >= nextSync)
            {
                nextSync = Time.time + 0.075f;
                syncedPosition.Value = transform.position;
                syncedRotation.Value = transform.rotation;
            }
            if (Time.time >= nextGrowl && CurrentMood != Mood.Wandering)
            {
                nextGrowl = Time.time + Random.Range(3f, 8f);
                GrowlRpc();
            }
        }

        private void Think()
        {
            if (CurrentMood != Mood.Wandering)
            {
                foreach (var pair in NetworkManager.Singleton.ConnectedClients)
                {
                    var avatar = WorldSecurity.Avatar(pair.Key);
                    if (!avatar) continue;
                    float range = Vector3.Distance(avatar.AuthoritativeHead, transform.position);
                    // Only a tiny near-field detection: creatures are effectively blind.
                    if (range < catchRadius && Time.time >= nextCatch)
                    {
                        nextCatch = Time.time + 7;
                        GameDirector.Instance?.CatchPlayer(pair.Key);
                    }
                }
                if (!agent.pathPending && agent.remainingDistance < 1.7f)
                {
                    if (CurrentMood != Mood.Searching)
                    {
                        SetMood(Mood.Searching);
                        searchUntil = Mathf.Max(searchUntil, Time.time + 7f);
                    }
                    if (Time.time < searchUntil)
                    {
                        var next = RandomSearchSpot();
                        GoTo(next);
                    }
                    else
                    {
                        interest = 0;
                        SetMood(Mood.Wandering);
                    }
                }
            }
            if (CurrentMood == Mood.Wandering && (!agent.hasPath || agent.remainingDistance < 1.5f))
            {
                Vector2 circle = Random.insideUnitCircle * patrolRadius;
                GoTo(spawn + new Vector3(circle.x, 0, circle.y));
                agent.speed = 1.4f;
            }
        }

        private Vector3 RandomSearchSpot()
        {
            // Sample points around the heard location, not the exact live player position.
            Vector2 disk = Random.insideUnitCircle * Mathf.Lerp(3f, 8f, Random.value);
            Vector3 target = lastKnownLocation + new Vector3(disk.x, 0, disk.y);
            if (history.Count > 1 && Random.value < 0.25f)
            {
                foreach (var sound in history) target = sound.Position;
            }
            return target;
        }
        private void GoTo(Vector3 point)
        {
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
        private void SetMood(Mood mode) => syncedMood.Value = (int)mode;
        private void AnimateBody()
        {
            if (face) face.localRotation = Quaternion.Euler(3f * Mathf.Sin(Time.time * 1.2f),
                Mathf.Sin(Time.time * 0.55f) * 7f, 0);
            if (limbs == null) return;
            float speed = IsServer && agent && agent.enabled ? agent.velocity.magnitude :
                CurrentMood == Mood.Hunting ? 3.5f : CurrentMood == Mood.Wandering ? 1.4f : 2f;
            for (int i = 0; i < limbs.Length; i++)
                if (limbs[i]) limbs[i].localRotation = Quaternion.Euler(
                    Mathf.Sin(Time.time * (3f + speed * 2f) + i * 3.14159f) * (8f + speed * 5f), 0, 0);
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void GrowlRpc()
        {
            if (growl && growl.clip) { growl.pitch = Random.Range(0.7f, 1f); growl.PlayOneShot(growl.clip, 0.35f); }
        }
    }

    // Original synthetic sound source. Replace with authored recordings for the finished game.
    public static class HorrorSynth
    {
        private static AudioClip generated;
        public static AudioClip MakeCreatureCall()
        {
            if (generated) return generated;
            const int sampleRate = 22050;
            int count = sampleRate * 2;
            var samples = new float[count];
            var random = new System.Random(7431);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (float)i / count);
                float rumble = Mathf.Sin(2 * Mathf.PI * (58 + 13 * Mathf.Sin(3.1f * t)) * t);
                float harmonic = Mathf.Sin(2 * Mathf.PI * (111 + 9 * Mathf.Sin(1.3f * t)) * t);
                float hiss = ((float)random.NextDouble() * 2f - 1f) * 0.04f;
                samples[i] = (rumble * 0.35f + harmonic * 0.16f + hiss) * env;
            }
            generated = AudioClip.Create("Listener_Original_Call", count, 1, sampleRate, false);
            generated.SetData(samples, 0);
            return generated;
        }
    }
}
