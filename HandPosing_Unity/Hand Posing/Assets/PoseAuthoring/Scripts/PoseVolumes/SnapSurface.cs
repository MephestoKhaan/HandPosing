using UnityEngine;

namespace PoseAuthoring.PoseVolumes
{
    [System.Serializable]
    public abstract class SnapSurface : MonoBehaviour
    {
        protected Transform GripPoint
        {
            get
            {
                return this.transform;
            }
        }

        //TODO: relativeTo probably not needed
        public abstract HandPose InvertedPose(Transform relativeTo, HandPose pose);

        public abstract Vector3 NearestPointInSurface(Vector3 targetPosition);
        public abstract Quaternion CalculateRotationOffset(Vector3 surfacePoint, Transform relativeTo);

        public abstract Pose SimilarPlaceAtVolume(Pose userPose, Pose snapPose, Transform relativeTo);

        public Pose NearestPlaceAtVolume(Pose userPose, Pose snapPose, Transform relativeTo)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;

            Vector3 surfacePoint = NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = CalculateRotationOffset(surfacePoint, relativeTo) * baseRot;

            return new Pose(surfacePoint, surfaceRotation);
        }
    }
}
