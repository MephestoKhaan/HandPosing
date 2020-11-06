using UnityEngine;

namespace PoseAuthoring
{
    [ExecuteInEditMode]
    public class HandGhost : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppet;

        public System.Action OnDirtyBones;
        public System.Action OnDirtyGrip;


        private HandPose _lockPose;
        private Transform _lockRelativeTo;

        public void SetPose(HandPose userPose, Transform relativeTo)
        {
            _lockPose = userPose;
            _lockRelativeTo = relativeTo;
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

        private void HoldInPlace()
        {
            if (_lockRelativeTo != null
                && this.transform.hasChanged)
            {
                this.transform.hasChanged = false;
                Debug.LogError("Do not move the ghost directly, but the snap point instead", this);
                puppet.LerpToPose(_lockPose, _lockRelativeTo, 0f,1f);
            }
        }
    }
}
