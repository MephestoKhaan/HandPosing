using UnityEngine;

namespace PoseAuthoring.PoseVolumes
{
    [System.Serializable]
    public class CylinderSurface : SnapSurface
    {
        [SerializeField]
        private Vector3 _startPoint;
        [SerializeField]
        private Vector3 _endPoint;
        [SerializeField]
        private float _angle;

        public Vector3 StartAngleDir
        {
            get
            {
                if(this.GripPoint == null)
                {
                    return Vector3.forward;
                }
                return Vector3.ProjectOnPlane(GripPoint.transform.position - StartPoint, Direction).normalized;
            }
        }

        public Vector3 EndAngleDir
        {
            get
            {
                return Quaternion.AngleAxis(Angle, Direction) * StartAngleDir;
            }
        }

        public Vector3 StartPoint
        {
            get
            {
                if(this.GripPoint != null)
                {
                    return this.GripPoint.TransformPoint(_startPoint);
                }
                else
                {
                    return _startPoint;
                }
            }
            set
            {
                if (this.GripPoint != null)
                {
                    _startPoint = this.GripPoint.InverseTransformPoint(value);
                }
                else
                {
                    _startPoint = value;
                }
            }
        }

        public Vector3 EndPoint
        {
            get
            {
                if (this.GripPoint != null)
                {
                    return this.GripPoint.TransformPoint(_endPoint);
                }
                else
                {
                    return _endPoint;
                }
            }
            set
            {
                if (this.GripPoint != null)
                {
                    _endPoint = this.GripPoint.InverseTransformPoint(value);
                }
                else
                {
                    _endPoint = value;
                }
            }
        }

        public float Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = Mathf.Repeat(value, 360f);
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
                Vector3 start = StartPoint;
                Vector3 projectedPoint = start + Vector3.Project(this.GripPoint.position - start, Direction);
                return Vector3.Distance(projectedPoint, this.GripPoint.position);
            }
        }

        public float Height
        {
            get
            {
                return (EndPoint - StartPoint).magnitude;
            }
        }

        public Vector3 Direction
        {
            get
            {
                Vector3 dir = (EndPoint - StartPoint);
                if (dir.sqrMagnitude == 0f)
                {
                    return this.GripPoint?this.GripPoint.up:Vector3.up;
                }
                return dir.normalized;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if(_startPoint == _endPoint)
                {
                    return Quaternion.LookRotation(Vector3.forward);
                }
                return Quaternion.LookRotation(StartAngleDir, Direction);
            }
        }

        public override HandPose InvertedPose(Transform relativeTo, HandPose pose)
        {
            HandPose invertedPose = pose;
            Quaternion globalRot = relativeTo.rotation * invertedPose.relativeGrip.rotation;

            Quaternion invertedRot = Quaternion.AngleAxis(180f, StartAngleDir) * globalRot;
            invertedPose.relativeGrip.rotation = Quaternion.Inverse(relativeTo.rotation) * invertedRot;

            return invertedPose;
        }

        public Vector3 PointAltitude(Vector3 point)
        {
            Vector3 start = StartPoint;
            Vector3 projectedPoint = start + Vector3.Project(point - start, Direction);
            return projectedPoint;
        }

        public override Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 start = StartPoint;
            Vector3 dir = Direction;
            Vector3 projectedVector = Vector3.Project(targetPosition - start, dir);

            if (projectedVector.magnitude > Height)
            {
                projectedVector = projectedVector.normalized * Height;
            }
            if (Vector3.Dot(projectedVector, dir) < 0f)
            {
                projectedVector = Vector3.zero;
            }

            Vector3 projectedPoint = StartPoint + projectedVector;
            Vector3 targetDirection = Vector3.ProjectOnPlane((targetPosition - projectedPoint), dir).normalized;
            //clamp of the surface
            float desiredAngle = Mathf.Repeat(Vector3.SignedAngle(StartAngleDir, targetDirection, dir), 360f);
            if (desiredAngle > Angle)
            {
                if (Mathf.Abs(desiredAngle - Angle) >= Mathf.Abs(360f - desiredAngle))
                {
                    targetDirection = StartAngleDir;
                }
                else
                {
                    targetDirection = EndAngleDir;
                }
            }

            Vector3 surfacePoint = projectedPoint + targetDirection * Radious;
            return surfacePoint;
        }

        public override Quaternion CalculateRotationOffset(Vector3 surfacePoint, Transform relativeTo)
        {
            Vector3 recordedDirection = Vector3.ProjectOnPlane(this.GripPoint.position - StartPoint, Direction);
            Vector3 desiredDirection = Vector3.ProjectOnPlane(surfacePoint - StartPoint, Direction);

            return Quaternion.FromToRotation(recordedDirection, desiredDirection);
        }


        public override Pose SimilarPlaceAtVolume(Pose userPose, Pose snapPose, Transform relativeTo)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion desiredRot = userPose.rotation;
            Quaternion baseRot = snapPose.rotation;

            Quaternion rotDif = (desiredRot) * Quaternion.Inverse(baseRot);
            Vector3 desiredDirection = (rotDif * Rotation) * Vector3.forward;
            Vector3 projectedDirection = Vector3.ProjectOnPlane(desiredDirection, Direction).normalized;

            Vector3 altitudePoint = PointAltitude(desiredPos);
            Vector3 surfacePoint = NearestPointInSurface(altitudePoint + projectedDirection * Radious);
            Quaternion surfaceRotation = CalculateRotationOffset(surfacePoint, relativeTo) * baseRot;

            return new Pose(surfacePoint, surfaceRotation);
        }
    }
}