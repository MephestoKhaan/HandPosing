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
            Puppet.LerpToPose(userPose, relativeTo);
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
            _snapPoseVolume.volume.transform = Puppet.Grip;

        }

        public void RefreshPose(Transform relativeTo)
        {
            _snapPoseVolume.pose = Puppet.CurrentPoseVisual(relativeTo);
        }


        public void Highlight(float amount)
        {
            if(handRenderer != null)
            {
                Color color = Color.Lerp(defaultColor, highlightedColor, amount);
                handRenderer.material.SetColor(colorIndex, color);
            }
        }

        public void Highlight(bool highlight)
        {
            if (handRenderer != null)
            {
                Color color = highlight ? highlightedColor : defaultColor;
                handRenderer.material.SetColor(colorIndex, color);
            }
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

        public float CalculateBestPlace(HandSnapPose userPose, out Pose bestPlace)
        {
            float bestScore = 0f;
            HandSnapPose snapPose = _snapPoseVolume.pose;

            if (snapPose.handeness != userPose.handeness
                && !_snapPoseVolume.ambydextrous)
            {
                bestPlace = new Pose(Vector3.zero, Quaternion.identity);
                return bestScore;
            }

            Vector3 globalPosDesired = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion globalRotDesired = RelativeTo.rotation * userPose.relativeGripRot;
            Pose desiredPlace = new Pose(globalPosDesired, globalRotDesired);

            var similarPlace = SimilarPlaceAtVolume(userPose, snapPose);
            var nearestPlace = NearestPlaceAtVolume(userPose, snapPose);
            bestPlace = GetBestPlace(similarPlace, nearestPlace, desiredPlace, out bestScore);

            if (_snapPoseVolume.handCanInvert)
            {
                HandSnapPose invertedPose = _snapPoseVolume.InvertedPose(RelativeTo);

                var similarInvertedPlace = SimilarPlaceAtVolume(userPose, invertedPose);
                var nearestInvertedPlace = NearestPlaceAtVolume(userPose, invertedPose);
                var bestInvertedPlace = GetBestPlace(similarInvertedPlace, nearestInvertedPlace, desiredPlace, out float bestInvertedScore);

                if (bestInvertedScore > bestScore)
                {
                    bestPlace = bestInvertedPlace;
                    return bestInvertedScore;
                }

            }

            return bestScore;
        }

        private Pose GetBestPlace(Pose a, Pose b, Pose comparer, out float bestScore)
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

        private float Score(Pose from, Pose to)
        {
            float forwardDifference = Vector3.Dot(from.rotation * Vector3.forward, to.rotation * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from.rotation * Vector3.up, to.rotation * Vector3.up) * 0.5f + 0.5f;

            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(from.position, to.position) / this.SnapPoseVolume.maxDistance);

            return forwardDifference * upDifference * positionDifference;
        }

        private Pose NearestPlaceAtVolume(HandSnapPose userPose, HandSnapPose snapPose)
        {
            Vector3 desiredPos = RelativeTo.TransformPoint(userPose.relativeGripPos);
            Quaternion baseRot = RelativeTo.rotation * snapPose.relativeGripRot;

            Vector3 surfacePoint = _snapPoseVolume.volume.NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = _snapPoseVolume.volume.CalculateRotationOffset(surfacePoint, RelativeTo) * baseRot;

            return new Pose(surfacePoint, surfaceRotation);
        }

        private Pose SimilarPlaceAtVolume(HandSnapPose userPose, HandSnapPose snapPose)
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

            return new Pose(surfacePoint, surfaceRotation);
        }

        public HandSnapPose AdjustPlace(Pose volumePlace)
        {
            HandSnapPose snapPose = _snapPoseVolume.pose;
            snapPose.relativeGripPos = RelativeTo.InverseTransformPoint(volumePlace.position);
            snapPose.relativeGripRot = Quaternion.Inverse(RelativeTo.rotation) * volumePlace.rotation;
            return snapPose;
        }
    }
}
