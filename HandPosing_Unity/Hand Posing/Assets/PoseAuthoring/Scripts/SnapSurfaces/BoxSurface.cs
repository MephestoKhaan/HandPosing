using UnityEngine;

namespace PoseAuthoring.PoseSurfaces
{
    [System.Serializable]
    public class BoxSurfaceData : SnapSurfaceData
    {
        public override System.Type SurfaceType => typeof(BoxSurface);

        [Range(0f,1f)]
        public float widthOffset;
        public Vector3 size;
        public Vector3 eulerAngles;

        //TODO select faces?
    }

    [System.Serializable]
    public class BoxSurface : SnapSurface
    {
        [SerializeField]
        private BoxSurfaceData _data = new BoxSurfaceData();

        public override SnapSurfaceData Data
        {
            get => _data;
            set => _data = value as BoxSurfaceData;
        }

        public float WidthOffset
        {
            get
            {
                return _data.widthOffset;
            }
            set
            {
                _data.widthOffset = value;
            }
        }


        public Vector3 Size
        {
            get
            {
                return _data.size;
            }
            set
            {
                _data.size = value;
            }
        }

        public Vector3 Offset => Vector3.zero;

        public Quaternion Rotation
        {
            get
            {
                return this.relativeTo.rotation * Quaternion.Euler(_data.eulerAngles);
            }
            set
            {
                _data.eulerAngles = (Quaternion.Inverse(this.relativeTo.rotation) *  value).eulerAngles;
            }
        }

        public Vector3 Direction
        {
            get
            {
                return Rotation * Vector3.forward;
            }
        }

        public override HandPose InvertedPose(HandPose pose)
        {
            return pose;
        }


        public override Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - Offset).normalized;
            return Offset + direction;
        }

        public override Pose MinimalRotationPoseAtSurface(Pose userPose, Pose snapPose)
        {
            Quaternion rotCorrection = Quaternion.FromToRotation(snapPose.up, Direction);
            Vector3 correctedDir = (rotCorrection * userPose.up).normalized;
            Vector3 surfacePoint = NearestPointInSurface(Offset + correctedDir);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, snapPose.rotation, userPose.rotation);
            return new Pose(surfacePoint, surfaceRotation);
        }

        public override Pose MinimalTranslationPoseAtSurface(Pose userPose, Pose snapPose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;
            Vector3 surfacePoint = NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, baseRot, userPose.rotation);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion RotationAtPoint(Vector3 surfacePoint, Quaternion baseRot, Quaternion desiredRotation)
        {
            Vector3 desiredDirection = (surfacePoint - Offset).normalized;
            Quaternion targetRotation = Quaternion.FromToRotation(Direction, desiredDirection) * baseRot;
            Vector3 targetProjected = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, desiredDirection).normalized;
            Vector3 desiredProjected = Vector3.ProjectOnPlane(desiredRotation * Vector3.forward, desiredDirection).normalized;
            Quaternion rotCorrection = Quaternion.FromToRotation(targetProjected, desiredProjected);
            return rotCorrection * targetRotation;
        }
    }
}