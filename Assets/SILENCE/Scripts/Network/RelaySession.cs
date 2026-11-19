using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Silence
{
    public sealed class RelaySession : MonoBehaviour
    {
        public static RelaySession Instance { get; private set; }
        public string JoinCode { get; private set; } = "";
        public string Status { get; private set; } = "Choose a mode";
        public bool Busy { get; private set; }
        public bool Connected => NetworkManager.Singleton && NetworkManager.Singleton.IsListening;
        public event Action OnStatusChanged;

        private void Awake() => Instance = this;

        // Offline survival uses a local host; no paid service or sign-in required.
        public void StartSolo()
        {
            if (Busy || Connected) return;
            bool ok = NetworkManager.Singleton.StartHost();
            SetStatus(ok ? "SOLO: shelter unlocked" : "Could not start solo host");
        }

        private async Task SignIn()
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public async void HostOnline()
        {
            if (Busy || Connected) return;
            Busy = true;
            SetStatus("Connecting to Unity Relay...");
            try
            {
                await SignIn();
                var allocation = await RelayService.Instance.CreateAllocationAsync(3);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                    AllocationUtils.ToRelayServerData(allocation, "dtls"));
                string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                if (!NetworkManager.Singleton.StartHost()) throw new Exception("StartHost failed");
                JoinCode = code;
                SetStatus("CO-OP HOSTED. CODE: " + code);
            }
            catch (Exception exception)
            {
                SetStatus("Relay error: " + exception.Message);
                Debug.LogException(exception);
            }
            finally { Busy = false; }
        }

        public async void JoinOnline(string code)
        {
            if (Busy || Connected || string.IsNullOrWhiteSpace(code)) return;
            Busy = true;
            SetStatus("Joining " + code + "...");
            try
            {
                await SignIn();
                var allocation = await RelayService.Instance.JoinAllocationAsync(code.Trim().ToUpperInvariant());
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                    AllocationUtils.ToRelayServerData(allocation, "dtls"));
                if (!NetworkManager.Singleton.StartClient()) throw new Exception("StartClient failed");
                SetStatus("Connected to " + code);
            }
            catch (Exception exception)
            {
                SetStatus("Join failed: " + exception.Message);
                Debug.LogException(exception);
            }
            finally { Busy = false; }
        }

        public void Disconnect()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
            JoinCode = "";
            SoundBus.Clear();
            SetStatus("Disconnected");
        }
        private void SetStatus(string status) { Status = status; OnStatusChanged?.Invoke(); }
    }
}
