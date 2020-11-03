using UnityEngine;

namespace PoseAuthoring.PoseVolumes
{
    public abstract class AbstractSurface
    {
        public Transform transform { protected get; set; }

        public AbstractSurface(Transform grip)
        {
            this.transform = grip;
        }

        public AbstractSurface(AbstractSurface other)
        {
            this.transform = other.transform;
        }

        //relativeTo probably not needed
        public abstract HandSnapPose InvertedPose(Transform relativeTo, HandSnapPose pose);

    }
}
