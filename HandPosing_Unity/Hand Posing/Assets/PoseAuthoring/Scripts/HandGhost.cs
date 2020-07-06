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
        [InspectorButton("CreateDuplicate")]
        public string Duplicate;


        [SerializeField]
        private VolumetricPose _snapPoseVolume;
        public VolumetricPose SnapPoseVolume
        {
            get
            {
                return _snapPoseVolume;
            }
        }

        private Transform _relativeTo;
        public Transform RelativeTo
        {
            get
            {
                return _relativeTo ?? this.transform.parent;
            }
            private set
            {
                _relativeTo = value;
            }
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
            Puppet.TransitionToPose(userPose, relativeTo);
            RelativeTo = relativeTo;
            _snapPoseVolume = new VolumetricPose()
            {
                pose = userPose,
                volume = new CylinderSurface(Puppet.Grip).MakeSinglePoint(),
                maxDistance = 0.1f
            };
        }

        public void SetPoseVolume(VolumetricPose poseVolume, Transform relativeTo)
        {
            SetPose(poseVolume.pose, relativeTo);
            _snapPoseVolume = poseVolume;
            _snapPoseVolume.volume.Grip = Puppet.Grip;

        }

        public void RefreshPose(Transform relativeTo)
        {
            _snapPoseVolume.pose = Puppet.CurrentPoseVisual(relativeTo);
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
            _snapPoseVolume.volume.MakeSinglePoint();
        }

        public void CreateDuplicate()
        {
            HandGhost ghost = Instantiate(this, this.transform.parent);
            ghost.SetPoseVolume(this._snapPoseVolume, this.transform);
            ghost.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
        }

        private void Reset()
        {
            _snapPoseVolume.volume = new CylinderSurface(Puppet.Grip);
            handRenderer = this.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        public float CalculateBestPose(HandSnapPose userPose, out (Vector3, Quaternion) bestPose)
        {
            float bestScore = 0f;
            HandSnapPose snapPose = _snapPoseVolume.pose;

            if (snapPose.handeness != userPose.handeness
                && !_snapPoseVolume.ambydextrous)
            {
                bestPose = (Vector3.zero, Quaternion.identity);
                return bestScore;
            }

            Vector3 globalPosDesired = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion globalRotDesired = RelativeTo.rotation * userPose.relativeGripRot;
            (Vector3, Quaternion) desiredPose = (globalPosDesired, globalRotDesired);

            var similarPose = SimilarPoseAtVolume(userPose, snapPose);
            var nearestPose = NearestPoseAtVolume(userPose, snapPose);
            bestPose = GetBestPose(similarPose, nearestPose, desiredPose, out bestScore);

            if (_snapPoseVolume.handCanInvert)
            {
                HandSnapPose invertedPose = _snapPoseVolume.InvertedPose(RelativeTo);

                var similarInvertedPose = SimilarPoseAtVolume(userPose, invertedPose);
                var nearestInvertedPose = NearestPoseAtVolume(userPose, invertedPose);
                var bestInvertedPose = GetBestPose(similarInvertedPose, nearestInvertedPose, desiredPose, out float bestInvertedScore);

                if (bestInvertedScore > bestScore)
                {
                    bestPose = bestInvertedPose;
                    return bestInvertedScore;
                }

            }

            return bestScore;
        }

        private (Vector3, Quaternion) GetBestPose((Vector3, Quaternion) a, (Vector3, Quaternion) b, (Vector3, Quaternion) comparer, out float bestScore)
        {
            float aScore = Score(comparer, a);
            float bScore = Score(comparer, b);

            if (aScore >= bScore)
            {
                bestScore = aScore;
                return a;
            }
            bestScore = bScore;
            return b;
        }

        private float Score((Vector3, Quaternion) from, (Vector3, Quaternion) to)
        {
            float forwardDifference = Vector3.Dot(from.Item2 * Vector3.forward, to.Item2 * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from.Item2 * Vector3.up, to.Item2 * Vector3.up) * 0.5f + 0.5f;

            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(from.Item1, to.Item1) / this.SnapPoseVolume.maxDistance);

            return forwardDifference * upDifference * positionDifference;
        }

        private (Vector3, Quaternion) NearestPoseAtVolume(HandSnapPose userPose, HandSnapPose snapPose)
        {
            Vector3 desiredPos = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion baseRot = RelativeTo.rotation * snapPose.relativeGripRot;

            Vector3 surfacePoint = _snapPoseVolume.volume.NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = _snapPoseVolume.volume.CalculateRotationOffset(surfacePoint, RelativeTo) * baseRot;

            return (surfacePoint, surfaceRotation);
        }

        private (Vector3, Quaternion) SimilarPoseAtVolume(HandSnapPose userPose, HandSnapPose snapPose)
        {
            CylinderSurface cylinder = _snapPoseVolume.volume;
            Vector3 desiredPos = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion baseRot = RelativeTo.rotation * snapPose.relativeGripRot;
            Quaternion desiredRot = RelativeTo.rotation * userPose.relativeGripRot;

            Quaternion rotDif = (desiredRot) * Quaternion.Inverse(baseRot);
            Vector3 desiredDirection = (rotDif * cylinder.Rotation) * Vector3.forward;
            Vector3 projectedDirection = Vector3.ProjectOnPlane(desiredDirection, cylinder.Direction).normalized;

            Vector3 altitudePoint = cylinder.PointAltitude(desiredPos);
            Vector3 surfacePoint = cylinder.NearestPointInSurface(altitudePoint + projectedDirection * cylinder.Radious);
            Quaternion surfaceRotation = cylinder.CalculateRotationOffset(surfacePoint, RelativeTo) * baseRot;

            return (surfacePoint, surfaceRotation);
        }

        public HandSnapPose AdjustPose((Vector3, Quaternion) volumePose)
        {
            HandSnapPose snapPose = _snapPoseVolume.pose;

            snapPose.relativeGripPos = RelativeTo.InverseTransformPoint(volumePose.Item1);
            snapPose.relativeGripRot = Quaternion.Inverse(RelativeTo.rotation) * volumePose.Item2;

            return snapPose;
        }
    }
}
