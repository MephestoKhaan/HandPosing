using UnityEngine;

namespace PoseAuthoring
{
    [ExecuteInEditMode]
    public class HandGhost : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppet;
        
        public System.Action OnDirty;

        public void SetPose(HandPose userPose, Transform relativeTo)
        {
            puppet.LerpToPose(userPose, relativeTo);
        }

        public HandPose ReadPose(Transform relativeTo)
        {
            return puppet.TrackedPose(relativeTo, true);
        }

        private void Update()
        {
            if(AnyBoneChanged())
            {
                OnDirty?.Invoke();
            }
        }

        private bool AnyBoneChanged()
        {
            bool hasChanged = false;
            foreach(var bone in puppet.Bones)
            {
                if(bone.transform.hasChanged)
                {
                    bone.transform.hasChanged = false;
                    hasChanged = true;
                }
            }
            return hasChanged;
        }
    }
}
