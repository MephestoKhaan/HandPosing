using UnityEngine;

namespace PoseAuthoring
{
    [System.Serializable]
    public class CylinderSurface
    {
        [SerializeField]
        private Vector3 _startPoint;
        [SerializeField]
        private Vector3 _endPoint;
        [SerializeField]
        private float _angle;

        public Transform transform { private get; set; }

        public CylinderSurface(Transform grip)
        {
            this.transform = grip;

            _angle = 230f;
            _startPoint = Vector3.up * 0.2f;
            _endPoint = Vector3.down * 0.2f;
        }

        public CylinderSurface(CylinderSurface other)
        {
            this.transform = other.transform;

            _angle = other.Angle;
            _startPoint = other._startPoint;
            _endPoint = other._endPoint;
        }

        public Vector3 StartAngleDir
        {
            get
            {
                if(this.transform == null)
                {
                    return Vector3.forward;
                }
                return Vector3.ProjectOnPlane(transform.transform.position - StartPoint, Direction).normalized;
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
                if(this.transform != null)
                {
                    return this.transform.TransformPoint(_startPoint);
                }
                else
                {
                    return _startPoint;
                }
            }
            set
            {
                if (this.transform != null)
                {
                    _startPoint = this.transform.InverseTransformPoint(value);
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
                if (this.transform != null)
                {
                    return this.transform.TransformPoint(_endPoint);
                }
                else
                {
                    return _endPoint;
                }
            }
            set
            {
                if (this.transform != null)
                {
                    _endPoint = this.transform.InverseTransformPoint(value);
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
                if (this.transform == null)
                {
                    return 0f;
                }
                Vector3 start = StartPoint;
                Vector3 projectedPoint = start + Vector3.Project(this.transform.position - start, Direction);
                return Vector3.Distance(projectedPoint, this.transform.position);
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
                    return this.transform?this.transform.up:Vector3.up;
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

        public CylinderSurface MakeSinglePoint()
        {
            _startPoint = _endPoint = Vector3.zero;
            Angle = 0f;
            return this;
        }

        public Vector3 PointAltitude(Vector3 point)
        {
            Vector3 start = StartPoint;
            Vector3 dir = Direction;
            Vector3 projectedPoint = start + Vector3.Project(point - start, Direction);
            return projectedPoint;
        }

        public Vector3 NearestPointInSurface(Vector3 targetPosition)
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

        public Quaternion CalculateRotationOffset(Vector3 surfacePoint, Transform relativeTo)
        {
            Vector3 recordedDirection = Vector3.ProjectOnPlane(this.transform.position - StartPoint, Direction);
            Vector3 desiredDirection = Vector3.ProjectOnPlane(surfacePoint - StartPoint, Direction);

            return Quaternion.FromToRotation(recordedDirection, desiredDirection);
        }
    }
}