using UnityEngine;

namespace HandPosing.SnapRecording
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(HandPuppet))]
    public class HandGhost : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppet;

        public System.Action OnDirtyBones;

        private void Reset()
        {
            puppet = this.GetComponent<HandPuppet>();
        }

        public void SetPose(HandPose userPose, Transform relativeTo)
        {
            puppet.LerpToPose(userPose, relativeTo);
        }

        public HandPose ReadPose(Transform relativeTo)
        {
            return puppet.TrackedPose(relativeTo, true);
        }

        private void LateUpdate()
        {
            if (AnyBoneChanged())
            {
                OnDirtyBones?.Invoke();
            }
        }

        private bool AnyBoneChanged()
        {
            bool hasChanged = false;
            foreach (var bone in puppet.Bones)
            {
                if (bone.transform.hasChanged)
                {
                    bone.transform.hasChanged = false;
                    hasChanged = true;
                }
            }
            return hasChanged;
        }
    }
}
