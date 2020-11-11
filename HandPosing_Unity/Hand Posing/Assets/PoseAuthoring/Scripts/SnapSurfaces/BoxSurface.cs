using UnityEngine;

namespace PoseAuthoring.PoseSurfaces
{
    [System.Serializable]
    public class BoxSurfaceData : SnapSurfaceData
    {
        public override System.Type SurfaceType => typeof(BoxSurface);

        public Vector3 centre;
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
                return (this.GripPoint.position - Centre);
            }
        }

        public override HandPose InvertedPose(HandPose pose)
        {
            return pose;
        }


        public override Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - Centre).normalized;
            return Centre + direction;
        }

        public override Pose MinimalRotationPoseAtSurface(Pose userPose, Pose snapPose)
        {
            Quaternion rotCorrection = Quaternion.FromToRotation(snapPose.up, Direction);
            Vector3 correctedDir = (rotCorrection * userPose.up).normalized;
            Vector3 surfacePoint = NearestPointInSurface(Centre + correctedDir);
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