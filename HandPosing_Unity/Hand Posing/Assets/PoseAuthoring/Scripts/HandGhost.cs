using UnityEngine;

namespace PoseAuthoring
{
    [RequireComponent(typeof(HandPuppet))]
    public class HandGhost : MonoBehaviour
    {
        [SerializeField]
        private Renderer handRenderer;
        [SerializeField]
        private Color highlightedColor = Color.yellow;
        [SerializeField]
        private Color defaultColor = Color.blue;
        [SerializeField]
        private string colorProperty = "_RimColor";
        [InspectorButton("MakeStaticPose")]
        public string StaticPose;

        [SerializeField]
        public CylinderSurface _cylinder;
        public CylinderSurface Cylinder
        {
            get
            {
                return _cylinder;
            }
        }

        public HandSnapPose Pose
        {
            get;
            private set;
        }

        public VolumetricPose PoseVolume
        {
            get
            {
                return new VolumetricPose()
                {
                    pose = Pose,
                    volume = Cylinder
                };
            }
        }

        public Transform RelativeTo
        {
            get;
            private set;
        }

        private HandPuppet _puppet;
        private HandPuppet Puppet
        {
            get
            {
                if (_puppet == null)
                {
                    _puppet = this.GetComponent<HandPuppet>();
                }
                return _puppet;
            }
        }
        private int colorIndex;

        private void Awake()
        {
            this.colorIndex = Shader.PropertyToID(colorProperty);
            Highlight(false);
        }

        public void SetPose(HandSnapPose userPose, Transform relativeTo)
        {
            Puppet.SetRecordedPose(userPose, relativeTo);
            RelativeTo = relativeTo;
            Pose = userPose;
            _cylinder = new CylinderSurface(Puppet.Grip);
            _cylinder.MakeSinglePoint();
        }

        public void SetPoseVolume(VolumetricPose poseVolume, Transform relativeTo)
        {
            SetPose(poseVolume.pose, relativeTo);
            _cylinder = poseVolume.volume;
            _cylinder.Grip = Puppet.Grip;
        }

        public void Highlight(float amount)
        {
            Color color = Color.Lerp(defaultColor, highlightedColor, amount);
            handRenderer.material.SetColor(colorIndex, color);
        }

        public void Highlight(bool highlight)
        {
            Color color = highlight ? highlightedColor : defaultColor;
            handRenderer.material.SetColor(colorIndex, color);
        }

        public void MakeStaticPose()
        {
            _cylinder.MakeSinglePoint();
        }

        private void Reset()
        {
            _cylinder = new CylinderSurface(Puppet.Grip);
            handRenderer = this.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        public float Score(HandSnapPose userPose, out (Vector3,Quaternion) bestPose, float maxDistance = 0.1f)
        {
            HandSnapPose snapPose = this.Pose;
            if (snapPose.isRightHand != userPose.isRightHand)
            {
                bestPose = (Vector3.zero, Quaternion.identity);
                return 0f;
            }

            Vector3 globalPosDesired = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion globalRotDesired = RelativeTo.rotation * userPose.relativeGripRot;

            (Vector3, Quaternion) desiredPose = (globalPosDesired, globalRotDesired);

            var similarPose = SimilarPoseAtVolume(userPose);
            var nearestPose = NearestPoseAtVolume(userPose);

            float similarScore = Score(similarPose, desiredPose);
            float nearestScore = Score(nearestPose, desiredPose);

            if(similarScore >= nearestScore)
            {
                bestPose = similarPose;
                return similarScore;
            }
            bestPose = nearestPose;
            return nearestScore;
        }
        
        private float Score((Vector3, Quaternion) from, (Vector3,Quaternion) to,  float maxDistance = 0.1f)
        {
            float forwardDifference = Vector3.Dot(from.Item2 * Vector3.forward, to.Item2 * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from.Item2 * Vector3.up, to.Item2 * Vector3.up) * 0.5f + 0.5f;

            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(from.Item1, to.Item1) / maxDistance);

            return forwardDifference * upDifference * positionDifference;
        }

        private (Vector3, Quaternion) NearestPoseAtVolume(HandSnapPose userPose)
        {
            Vector3 desiredPos = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion baseRot = RelativeTo.rotation * Pose.relativeGripRot;

            Vector3 surfacePoint = Cylinder.NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = Cylinder.CalculateRotationOffset(surfacePoint, RelativeTo) * baseRot;

            return (surfacePoint, surfaceRotation);
        }

        private (Vector3, Quaternion) SimilarPoseAtVolume(HandSnapPose userPose)
        {
            Vector3 desiredPos = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion baseRot = RelativeTo.rotation * Pose.relativeGripRot;
            Quaternion desiredRot = RelativeTo.rotation * userPose.relativeGripRot;

            Quaternion rotDif = (desiredRot) * Quaternion.Inverse(baseRot);
            Vector3 desiredDirection = (rotDif * _cylinder.Rotation) * Vector3.forward;
            Vector3 projectedDirection = Vector3.ProjectOnPlane(desiredDirection, _cylinder.Direction).normalized;

            Vector3 altitudePoint = _cylinder.PointAltitude(desiredPos);
            Vector3 surfacePoint = Cylinder.NearestPointInSurface(altitudePoint + projectedDirection * _cylinder.Radious);
            Quaternion surfaceRotation = Cylinder.CalculateRotationOffset(surfacePoint, RelativeTo) * baseRot;

            return (surfacePoint, surfaceRotation);
        }

        public HandSnapPose AdjustPose((Vector3, Quaternion) volumePose)
        {
            HandSnapPose snapPose = this.Pose;

            snapPose.relativeGripPos = RelativeTo.InverseTransformPoint(volumePose.Item1);
            snapPose.relativeGripRot = Quaternion.Inverse(RelativeTo.rotation) * volumePose.Item2;

            return snapPose;
        }
    }
}
