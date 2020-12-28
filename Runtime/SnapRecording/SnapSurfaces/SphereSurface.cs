using UnityEngine;

namespace HandPosing.SnapSurfaces
{
    [System.Serializable]
    public class SphereSurfaceData : SnapSurfaceData
    {
        public override System.Type SurfaceType => typeof(SphereSurface);

        public override object Clone()
        {
            SphereSurfaceData clone = new SphereSurfaceData();
            clone.centre = this.centre;
            return clone;
        }

        public override SnapSurfaceData Mirror()
        {
            SphereSurfaceData mirror = Clone() as SphereSurfaceData;
            return mirror;
        }

        public Vector3 centre;
    }

    /// <summary>
    /// Especifies an entire sphere around an object in which the grip point is valid.
    /// 
    /// One of the main advantages of spheres is that the rotation of the hand pose does
    /// not really matters, as it will always fit the surface correctly.
    /// </summary>
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

        /// <summary>
        /// The centre of the sphere in world coordinates.
        /// </summary>
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

        /// <summary>
        /// The radious of the sphere, this is automatically calculated as the distance between
        /// the centre and the original grip pose.
        /// </summary>
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

        /// <summary>
        /// The direction of the sphere, measured from the centre to the original grip position.
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                return (this.GripPoint.position - Centre).normalized;
            }
        }

        /// <summary>
        /// The rotation of the sphere from the recorded grip position.
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return Quaternion.LookRotation(Direction, this.GripPoint.forward);
            }
        }

        public override HandPose InvertedPose(HandPose pose)
        {
            return pose;
        }

        public override Pose MirrorPose(Pose pose)
        {
            Vector3 normal = Quaternion.Inverse(this.relativeTo.rotation) * Direction;
            Vector3 tangent = Vector3.Cross(normal,Vector3.up);
            return pose.MirrorPose(normal, tangent);
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