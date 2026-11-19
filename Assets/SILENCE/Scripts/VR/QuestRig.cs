using UnityEngine;
using UnityEngine.XR;

namespace Silence
{
    // Native XR tracking; never animates or shakes the HMD camera.
    // Supports OpenXR on device and keyboard test controls in the Editor.
    [RequireComponent(typeof(CharacterController))]
    public sealed class QuestRig : MonoBehaviour
    {
        public static QuestRig Local { get; private set; }
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;
        public float speed = 2.0f;
        public float sprintMultiplier = 1.5f;
        public float gravity = -16f;
        public bool snapTurn = true;
        public float snapAngle = 35f;
        public float smoothTurnDegrees = 65f;
        public float heightOffset = 0f;
        public bool IsPaused { get; set; } = true;
        public bool IsClimbing { get; set; }

        private CharacterController controller;
        private float fallingVelocity;
        private float turnCooldown;
        private float stepDistance;
        private Vector3 lastHeadPosition;
        private bool wasGrounded;
        private Vector2 leftAxis;
        private Vector2 rightAxis;
        private InputDevice hmd, left, right;
        public bool RightTriggerDown { get; private set; }
        public bool LeftTriggerDown { get; private set; }
        public bool RightGripDown { get; private set; }
        public bool LeftGripDown { get; private set; }
        public bool LeftMenuDown { get; private set; }
        public bool RightPrimaryDown { get; private set; }
        public bool LeftGripPressed { get; private set; }
        public bool RightGripPressed { get; private set; }
        private bool oldRT, oldLT, oldRG, oldLG, oldMenu, oldPrimary;
        public Vector3 HeadWorldPosition => head ? head.position : transform.position + Vector3.up * 1.6f;

        private void Awake()
        {
            Local = this;
            controller = GetComponent<CharacterController>();
            controller.radius = 0.22f;
            controller.stepOffset = 0.24f;
            controller.slopeLimit = 42;
            controller.minMoveDistance = 0;
        }

        private void Update()
        {
            if (!hmd.isValid) hmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!left.isValid) left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!right.isValid) right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            TrackPose(hmd, head);
            TrackPose(left, leftHand);
            TrackPose(right, rightHand);

            left.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftAxis);
            right.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightAxis);
            left.TryGetFeatureValue(CommonUsages.triggerButton, out bool lt);
            right.TryGetFeatureValue(CommonUsages.triggerButton, out bool rt);
            left.TryGetFeatureValue(CommonUsages.gripButton, out bool lg);
            right.TryGetFeatureValue(CommonUsages.gripButton, out bool rg);
            left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool menu);
            right.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);

#if UNITY_EDITOR
            if (!hmd.isValid)
            {
                leftAxis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                rightAxis.x = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
                rt |= Input.GetMouseButton(0);
                rg |= Input.GetMouseButton(1);
                menu |= Input.GetKey(KeyCode.Escape);
                primary |= Input.GetKey(KeyCode.F);
                float yaw = Input.GetAxis("Mouse X") * 2f;
                head.localRotation = Quaternion.Euler(0, head.localEulerAngles.y + yaw, 0);
            }
#endif
            RightTriggerDown = rt && !oldRT;
            LeftTriggerDown = lt && !oldLT;
            RightGripDown = rg && !oldRG;
            RightGripPressed = rg; LeftGripPressed = lg;
            RightPrimaryDown = primary && !oldPrimary;
            LeftGripDown = lg && !oldLG;
            LeftMenuDown = menu && !oldMenu;
            oldRT = rt; oldLT = lt; oldRG = rg; oldLG = lg; oldMenu = menu; oldPrimary = primary;

            if (IsPaused) return;
            if (!IsClimbing) Move();
            Turn();
        }

        private void TrackPose(InputDevice device, Transform target)
        {
            if (!target || !device.isValid) return;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                target.localPosition = position + (target == head ? Vector3.up * heightOffset : Vector3.zero);
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                target.localRotation = rotation;
        }

        private void Move()
        {
            if (!head) return;
            controller.height = Mathf.Clamp(head.localPosition.y + 0.14f, 0.85f, 2.15f);
            controller.center = new Vector3(head.localPosition.x, controller.height / 2f, head.localPosition.z);
            Vector3 f = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Vector3 r = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
            bool sprinting = left.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool press) && press;
#if UNITY_EDITOR
            sprinting |= Input.GetKey(KeyCode.LeftShift);
#endif
            Vector3 desired = Vector3.ClampMagnitude(f * leftAxis.y + r * leftAxis.x, 1f);
            float movingSpeed = speed * (sprinting ? sprintMultiplier : 1f);
            if (controller.isGrounded && fallingVelocity < 0) fallingVelocity = -1.5f;
            fallingVelocity += gravity * Time.deltaTime;
            controller.Move((desired * movingSpeed + Vector3.up * fallingVelocity) * Time.deltaTime);
            if (controller.isGrounded && !wasGrounded && fallingVelocity < -3f)
                NetworkAvatar.Local?.EmitNoise(18f, SoundType.Impact);
            wasGrounded = controller.isGrounded;
            stepDistance += desired.magnitude * movingSpeed * Time.deltaTime;
            if (controller.isGrounded && stepDistance >= (sprinting ? 1.0f : 1.5f))
            {
                stepDistance = 0;
                NetworkAvatar.Local?.EmitNoise(sprinting ? 23 : 8, sprinting ? SoundType.Sprint : SoundType.Footstep);
            }
        }

        private void Turn()
        {
            float x = rightAxis.x;
            if (snapTurn)
            {
                if (Mathf.Abs(x) < 0.35f) turnCooldown = 0f;
                if (Mathf.Abs(x) > 0.7f && turnCooldown <= 0f)
                {
                    transform.RotateAround(head.position, Vector3.up, Mathf.Sign(x) * snapAngle);
                    turnCooldown = 0.5f;
                }
            }
            else if (Mathf.Abs(x) > 0.15f)
                transform.RotateAround(head.position, Vector3.up, x * smoothTurnDegrees * Time.deltaTime);
            turnCooldown = Mathf.Max(0, turnCooldown - Time.deltaTime);
        }

        public void ClimbMove(Vector3 offset)
        {
            controller.Move(Vector3.ClampMagnitude(offset, 0.2f));
            fallingVelocity = -1.5f;
        }

        public void TeleportToCheckpoint(Vector3 point)
        {
            controller.enabled = false;
            transform.position = point;
            controller.enabled = true;
        }
    }
}
