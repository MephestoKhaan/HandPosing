using System;

namespace PoseAuthoring.PoseVolumes
{
    [Serializable]
    public struct VolumetricPose
    {
        public HandSnapPose pose;
        public CylinderSurface volume;
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

        public HandSnapPose InvertedPose(UnityEngine.Transform relativeTo) //relativeTo probably not needed
        {
            return Volume.InvertedPose(relativeTo, pose);
        }

        public VolumetricPose Clone()
        {
            return new VolumetricPose()
            {
                handCanInvert = this.handCanInvert,
                maxDistance = this.maxDistance,
                pose = this.pose,
                volume = new CylinderSurface(this.volume)
            };
        }
    }
}
