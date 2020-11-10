using UnityEngine;

namespace PoseAuthoring.PoseSurfaces
{
    [System.Serializable]
    public class SphereSurfaceData : SnapSurfaceData
    {
        public override System.Type SurfaceType => typeof(SphereSurface);
        public Vector3 centre;
    }

    [System.Serializable]
    public class SphereSurface : SnapSurface
    {
        [SerializeField]
        private SphereSurfaceData _data = new SphereSurfaceData();

        public override SnapSurfaceData Data
        {
            get => _data;
            set => _data = value as SphereSurfaceData;
        }

        public Vector3 Centre
        {
            get
            {
                if (this.relativeTo != null)
                {
                    return this.relativeTo.TransformPoint(_data.centre);
                }
                else
                {
                    return _data.centre;
                }
            }
            set
            {
                if (this.relativeTo != null)
                {
                    _data.centre = this.relativeTo.InverseTransformPoint(value);
                }
                else
                {
                    _data.centre = value;
                }
            }
        }

        public float Radious
        {
            get
            {
                if (this.GripPoint == null)
                {
                    return 0f;
                }
                return Vector3.Distance(Centre, this.GripPoint.position);
            }
        }


        public override HandPose InvertedPose(HandPose pose)
        {
            return pose;
        }

        public Vector3 Direction
        {
            get
            {
                return (this.GripPoint.position - Centre).normalized;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return Quaternion.LookRotation(Direction, this.GripPoint.forward);
            }
        }


        public override Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - Centre).normalized;
            return Centre + direction * Radious;
        }

        public override Pose MinimalRotationPoseAtSurface(Pose userPose, Pose snapPose)
        {
            Quaternion rotCorrection = Quaternion.FromToRotation(snapPose.up, Direction);
            Vector3 correctedDir = (rotCorrection * userPose.up).normalized;
            Vector3 surfacePoint = NearestPointInSurface(Centre + correctedDir * Radious);
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
            Vector3 desiredDirection = (surfacePoint - Centre).normalized;
            Quaternion targetRotation = Quaternion.FromToRotation(Direction, desiredDirection) * baseRot;
            Vector3 targetProjected = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, desiredDirection).normalized;
            Vector3 desiredProjected = Vector3.ProjectOnPlane(desiredRotation * Vector3.forward, desiredDirection).normalized;
            Quaternion rotCorrection = Quaternion.FromToRotation(targetProjected, desiredProjected);
            return rotCorrection * targetRotation;
        }
    }
}