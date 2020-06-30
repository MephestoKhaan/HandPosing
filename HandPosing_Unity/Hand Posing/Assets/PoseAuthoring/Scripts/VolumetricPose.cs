using System;
using UnityEngine;

namespace PoseAuthoring
{
    [Serializable]
    public struct VolumetricPose
    {
        public HandSnapPose pose;
        public CylinderSurface volume;
        public bool ambydextrous;
        public bool handCanInvert;
        public float maxDistance;

        public CylinderSurface Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
            }
        }


        public HandSnapPose InvertedPose(Transform relativeTo) //relativeTo probably not needed
        {
            HandSnapPose invertedPose = pose;
            Quaternion globalRot = relativeTo.rotation * invertedPose.relativeGripRot;

            Quaternion invertedRot = Quaternion.AngleAxis(180f, volume.StartAngleDir) * globalRot;
            invertedPose.relativeGripRot = Quaternion.Inverse(relativeTo.rotation) * invertedRot;

            return invertedPose;
        }


        public VolumetricPose Clone()
        {
            return new VolumetricPose()
            {
                ambydextrous = this.ambydextrous,
                handCanInvert = this.handCanInvert,
                maxDistance = this.maxDistance,
                pose = this.pose,
                volume = new CylinderSurface(this.volume)
            };
        }
    }
}
