using System.Collections.Generic;
using UnityEngine;

namespace HandPosing
{
    public enum Handeness
    {
        Left, Right
    }

    [System.Serializable]
    public struct BoneRotation
    {
        public BoneId boneID;
        public Quaternion rotation;

        public static BoneRotation? Lerp(BoneRotation from, BoneRotation to, float t)
        {
            if(from.boneID != to.boneID)
            {
                Debug.LogError("Bones must have same id for interpolation");
                return null;
            }

            return new BoneRotation()
            {
                boneID = from.boneID,
                rotation = Quaternion.Lerp(from.rotation, to.rotation, t)
            };

        }
    }

    [System.Serializable]
    public struct HandPose
    {
        public Pose relativeGrip;
        public Handeness handeness;

        [SerializeField]
        private List<BoneRotation> _bones;
        public List<BoneRotation> Bones
        {
            get
            {
                if (_bones == null)
                {
                    _bones = new List<BoneRotation>();
                }
                return _bones;
            }
        }

        public Pose ToPose(Transform relativeTo)
        {
            Vector3 globalPosDesired = relativeTo.TransformPoint(relativeGrip.position);
            Quaternion globalRotDesired = relativeTo.rotation * relativeGrip.rotation;
            return new Pose(globalPosDesired, globalRotDesired);
        }

        public HandPose AdjustPose(Pose pose, Transform relativeTo)
        {
            HandPose snapPose = this;
            snapPose.relativeGrip.position = relativeTo.InverseTransformPoint(pose.position);
            snapPose.relativeGrip.rotation = Quaternion.Inverse(relativeTo.rotation) * pose.rotation;
            return snapPose;
        }

        public static HandPose? Lerp(HandPose from, HandPose to, float t)
        {
            if(from.handeness != to.handeness)
            {
                Debug.LogError("Hand poses must have same handenness for lerping");
                return null;
            }
            if(from.Bones.Count != to.Bones.Count)
            {
                Debug.LogError("Hand poses must have same Bones for lerping");
                return null;
            }

            HandPose result = new HandPose();
            result.relativeGrip = PoseUtils.Lerp(from.relativeGrip, to.relativeGrip, t);
            result.handeness = from.handeness;

            result.Bones.Clear();
            for (int i = 0; i < from.Bones.Count; i++)
            {
                BoneRotation? bone = BoneRotation.Lerp(from.Bones[i], to.Bones[i], t);
                if(bone.HasValue)
                {
                    result.Bones.Add(bone.Value);
                }
                else
                {
                    Debug.LogError("Hand poses must have same Bones for lerping");
                    return null;
                }
            }

            return result;

        }
    }

}