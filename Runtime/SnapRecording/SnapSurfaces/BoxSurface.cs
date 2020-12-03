using UnityEngine;

namespace HandPosing.SnapSurfaces
{
    [System.Serializable]
    public class BoxSurfaceData : SnapSurfaceData
    {
        public override System.Type SurfaceType => typeof(BoxSurface);

        public override object Clone()
        {
            BoxSurfaceData clone = new BoxSurfaceData();
            clone.widthOffset = this.widthOffset;
            clone.snapOffset = this.snapOffset;
            clone.size = this.size;
            clone.eulerAngles = this.eulerAngles;
            return clone;
        }


        [Range(0f, 1f)]
        public float widthOffset = 0.5f;
        public Vector4 snapOffset;
        public Vector3 size = new Vector3(0.1f, 0f, 0.1f);
        public Vector3 eulerAngles;
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
        public Vector4 SnapOffset
        {
            get
            {
                return _data.snapOffset;
            }
            set
            {
                _data.snapOffset = value;
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
                _data.eulerAngles = (Quaternion.Inverse(this.relativeTo.rotation) * value).eulerAngles;
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


        public override Pose MirrorPose(Pose pose)
        {
            Vector3 normal = Quaternion.Inverse(this.relativeTo.rotation) * Direction;
            Vector3 tangent = Quaternion.Inverse(this.relativeTo.rotation) * (Rotation * Vector3.up);
            return pose.MirrorPose(normal, tangent);
        }


        private (Vector3, Vector3, Vector3, Vector3) CalculateCorners()
        {
            Vector3 rightRot = Rotation * Vector3.right;
            Vector3 bottomLeft = GripPoint.position - rightRot * _data.size.x * (1f - _data.widthOffset);
            Vector3 bottomRight = GripPoint.position + rightRot * _data.size.x * (_data.widthOffset);
            Vector3 forwardOffset = Rotation * Vector3.forward * _data.size.z;
            Vector3 topLeft = bottomLeft + forwardOffset;
            Vector3 topRight = bottomRight + forwardOffset;
            return (bottomLeft, bottomRight, topLeft, topRight);
        }

        private Vector3 ProjectOnSegment(Vector3 point, (Vector3, Vector3) segment)
        {
            Vector3 line = segment.Item2 - segment.Item1;
            Vector3 projection = Vector3.Project(point - segment.Item1, line);
            if (Vector3.Dot(projection, line) < 0f)
            {
                projection = segment.Item1;
            }
            else if (projection.magnitude > line.magnitude)
            {
                projection = segment.Item2;
            }
            else
            {
                projection += segment.Item1;
            }
            return projection;
        }

        public override Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            return NearestPointAndAngleInSurface(targetPosition).Item1;
        }

        private (Vector3, float) NearestPointAndAngleInSurface(Vector3 targetPosition)
        {
            Vector3 rightDir = Rotation * Vector3.right;
            Vector3 forwardDir = Rotation * Vector3.forward;
            Vector3 bottomLeft, bottomRight, topLeft, topRight;
            (bottomLeft, bottomRight, topLeft, topRight) = CalculateCorners();

            Vector3 bottomP = ProjectOnSegment(targetPosition, (bottomLeft + rightDir * SnapOffset.y, bottomRight + rightDir * SnapOffset.x));
            Vector3 topP = ProjectOnSegment(targetPosition, (topLeft - rightDir * SnapOffset.x, topRight - rightDir * SnapOffset.y));
            Vector3 leftP = ProjectOnSegment(targetPosition, (bottomLeft - forwardDir * SnapOffset.z, topLeft - forwardDir * SnapOffset.w));
            Vector3 rightP = ProjectOnSegment(targetPosition, (bottomRight + forwardDir * SnapOffset.w, topRight + forwardDir * SnapOffset.z));

            float bottomDistance = Vector3.Distance(bottomP, targetPosition);
            float topDistance = Vector3.Distance(topP, targetPosition);
            float leftDistance = Vector3.Distance(leftP, targetPosition);
            float rightDistance = Vector3.Distance(rightP, targetPosition);

            float minDistance = Mathf.Min(bottomDistance, Mathf.Min(topDistance, Mathf.Min(leftDistance, rightDistance)));
            if (bottomDistance == minDistance)
            {
                return (bottomP, 0f);
            }
            if (topDistance == minDistance)
            {
                return (topP, 180f);
            }
            if (leftDistance == minDistance)
            {
                return (leftP, 90f);
            }
            return (rightP, -90f);
        }

        public override Pose MinimalRotationPoseAtSurface(Pose userPose, Pose snapPose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;
            Quaternion desiredRot = userPose.rotation;
            Vector3 up = Rotation * Vector3.up;

            Quaternion bottomRot = baseRot;
            Quaternion topRot = Quaternion.AngleAxis(180f, up) * baseRot;
            Quaternion leftRot = Quaternion.AngleAxis(90f, up) * baseRot;
            Quaternion rightRot = Quaternion.AngleAxis(-90f, up) * baseRot;

            float bottomDot = PoseUtils.RotationDifference(bottomRot, desiredRot);
            float topDot = PoseUtils.RotationDifference(topRot, desiredRot);
            float leftDot = PoseUtils.RotationDifference(leftRot, desiredRot);
            float rightDot = PoseUtils.RotationDifference(rightRot, desiredRot);

            Vector3 rightDir = Rotation * Vector3.right;
            Vector3 forwardDir = Rotation * Vector3.forward;
            Vector3 bottomLeft, bottomRight, topLeft, topRight;
            (bottomLeft, bottomRight, topLeft, topRight) = CalculateCorners();

            float maxDot = Mathf.Max(bottomDot, Mathf.Max(topDot, Mathf.Max(leftDot, rightDot)));
            if (bottomDot == maxDot)
            {
                Vector3 projBottom = ProjectOnSegment(desiredPos, (bottomLeft + rightDir * SnapOffset.y, bottomRight + rightDir * SnapOffset.x));
                return new Pose(projBottom, bottomRot);
            }
            if (topDot == maxDot)
            {
                Vector3 projTop = ProjectOnSegment(desiredPos, (topLeft - rightDir * SnapOffset.x, topRight - rightDir * SnapOffset.y));
                return new Pose(projTop, topRot);
            }
            if (leftDot == maxDot)
            {
                Vector3 projLeft = ProjectOnSegment(desiredPos, (bottomLeft - forwardDir * SnapOffset.z, topLeft - forwardDir * SnapOffset.w));
                return new Pose(projLeft, leftRot);
            }
            Vector3 projRight = ProjectOnSegment(desiredPos, (bottomRight + forwardDir * SnapOffset.w, topRight + forwardDir * SnapOffset.z));
            return new Pose(projRight, rightRot);
        }

        public override Pose MinimalTranslationPoseAtSurface(Pose userPose, Pose snapPose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;
            Vector3 surfacePoint;
            float surfaceAngle;
            (surfacePoint, surfaceAngle) = NearestPointAndAngleInSurface(desiredPos);
            Quaternion surfaceRotation = RotateUp(baseRot, surfaceAngle);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion RotateUp(Quaternion baseRot, float angle)
        {
            Quaternion offset = Quaternion.AngleAxis(angle, Rotation * Vector3.up);
            return offset * baseRot;
        }
    }
}