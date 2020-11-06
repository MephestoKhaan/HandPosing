using UnityEngine;

namespace PoseAuthoring.PoseVolumes
{
    [System.Serializable]
    public class SnapSurfaceData { }

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

        public Transform relativeTo;

        public abstract SnapSurfaceData Data { get; set; }

        public abstract HandPose InvertedPose(HandPose pose);
        public abstract Vector3 NearestPointInSurface(Vector3 targetPosition);
        public abstract Quaternion CalculateRotationOffset(Vector3 surfacePoint);

        public abstract Pose SimilarPlaceAtVolume(Pose userPose, Pose snapPose);

        public Pose NearestPlaceAtVolume(Pose userPose, Pose snapPose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;

            Vector3 surfacePoint = NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = CalculateRotationOffset(surfacePoint) * baseRot;

            return new Pose(surfacePoint, surfaceRotation);
        }
    }
}
