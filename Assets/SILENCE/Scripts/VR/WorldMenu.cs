using System.Collections.Generic;
using UnityEngine;

namespace Silence
{
    public sealed class MenuKey : MonoBehaviour
    {
        public string value;
    }

    // World-space menu, selected by pointing a controller and pressing trigger.
    // Join code is entered with 3D in-headset keys, no phone keyboard required.
    public sealed class WorldMenu : MonoBehaviour
    {
        public QuestRig rig;
        public Transform panel;
        public TextMesh header;
        public TextMesh status;
        public GameObject mainButtons;
        public GameObject keyboard;
        public Transform pointer;
        public LineRenderer line;
        private string code = "";
        private bool opened = true;
        private bool enteringCode;
        private float clickLock;
        private void Start() { SetOpened(true); }
        public void SetOpened(bool value)
        {
            opened = value;
            if (rig) rig.IsPaused = value;
            if (panel) panel.gameObject.SetActive(value);
            if (value) ShowMain();
        }
        public void ShowMain()
        {
            enteringCode = false;
            if (mainButtons) mainButtons.SetActive(true);
            if (keyboard) keyboard.SetActive(false);
        }
        public void ShowKeyboard()
        {
            enteringCode = true;
            code = "";
            if (mainButtons) mainButtons.SetActive(false);
            if (keyboard) keyboard.SetActive(true);
        }
        private void Update()
        {
            if (!rig) return;
            if (rig.LeftMenuDown && RelaySession.Instance && RelaySession.Instance.Connected)
                SetOpened(!opened);
            if (!opened) return;
            if (status)
                status.text = enteringCode ? "JOIN CODE: " + code + "_" :
                    (RelaySession.Instance ? RelaySession.Instance.Status : "SELECT MODE") + "\n" +
                    "Point your right hand; press trigger";
            Transform hand = rig.rightHand;
            Vector3 p = hand.position, d = hand.forward;
            if (line)
            {
                line.positionCount = 2;
                line.SetPosition(0, p);
                line.SetPosition(1, p + d * 2.5f);
            }
            if (Time.time < clickLock || !rig.RightTriggerDown) return;
            if (!Physics.Raycast(p, d, out RaycastHit hit, 3.4f)) return;
            var key = hit.collider.GetComponent<MenuKey>();
            if (!key) return;
            clickLock = Time.time + 0.24f;
            if (enteringCode) HandleCodeKey(key.value);
            else HandleMainKey(key.value);
        }

        private void HandleMainKey(string value)
        {
            switch (value)
            {
                case "SOLO": RelaySession.Instance?.StartSolo(); break;
                case "HOST": RelaySession.Instance?.HostOnline(); break;
                case "JOIN": ShowKeyboard(); break;
                case "RESUME": if (RelaySession.Instance && RelaySession.Instance.Connected) SetOpened(false); break;
                case "SNAP": rig.snapTurn = !rig.snapTurn; break;
                case "HEIGHT+": rig.heightOffset = Mathf.Min(0.5f, rig.heightOffset + 0.05f); break;
                case "HEIGHT-": rig.heightOffset = Mathf.Max(-0.5f, rig.heightOffset - 0.05f); break;
                case "MIC": GetComponent<MicrophoneListener>()?.SetEnabled(
                    !(GetComponent<MicrophoneListener>()?.EnabledByPlayer ?? false)); break;
                case "QUIT": RelaySession.Instance?.Disconnect(); break;
            }
        }
        private void HandleCodeKey(string value)
        {
            if (value == "BACK") { ShowMain(); return; }
            if (value == "DEL") { if (code.Length > 0) code = code.Substring(0, code.Length - 1); return; }
            if (value == "GO")
            {
                if (code.Length >= 5) { RelaySession.Instance?.JoinOnline(code); ShowMain(); }
                return;
            }
            if (code.Length < 8) code += value;
        }
    }
}
