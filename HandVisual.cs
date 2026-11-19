using UnityEngine;

namespace Silence
{
    // Lightweight segmented VR glove mesh controlled by grip/trigger.
    // The art is a procedural stand-in for a skinned skeletal hand, not final anatomical animation.
    public sealed class HandVisual : MonoBehaviour
    {
        public QuestRig rig;
        public bool leftHand;
        public Transform[] proximal;
        public Transform[] middle;
        public Transform[] distal;
        private float squeeze;
        private void Update()
        {
            if (!rig) return;
            float target = leftHand ? (rig.LeftGripPressed ? 1f : 0f) : (rig.RightGripPressed ? 1f : 0f);
            squeeze = Mathf.MoveTowards(squeeze, target, Time.deltaTime * 7f);
            for(int i = 0; i < 5; i++)
            {
                if (proximal != null && i < proximal.Length && proximal[i])
                    proximal[i].localRotation = Quaternion.Euler(-squeeze * (i==0 ? 24f : 52f),0,i==0?25f:0);
                if (middle != null && i < middle.Length && middle[i])
                    middle[i].localRotation = Quaternion.Euler(-squeeze * 63f,0,0);
                if (distal != null && i < distal.Length && distal[i])
                    distal[i].localRotation = Quaternion.Euler(-squeeze * 55f,0,0);
            }
        }
    }
}
