using PoseAuthoring.PoseVolumes;
using UnityEngine;

namespace PoseAuthoring.PoseRecording
{
    public class SnapPose : MonoBehaviour
    {
        [SerializeField]
        private Transform relativeTo;
        [SerializeField]
        private HandPose pose;

        [Space]
        [SerializeField]
        private SnapSurface surface;
        [SerializeField]
        private HandGhost ghost;

        //TODO needs saving? maybe even the surface
        [Space]
        [SerializeField]
        private bool canInvert = false;
        [SerializeField]
        private float maxDistance = 0.1f;
        [SerializeField]
        [Range(0f, 1f)]
        private float positionRotationWeight = 0.5f;
        [SerializeField]
        private bool snapsBack = false;
        [SerializeField]
        [Range(0f, 1f)]
        private float slideThresold = 0f;

        public Transform RelativeTo { get => relativeTo; }
        public bool SnapsBack { get => snapsBack; }
        public float SlideThresold { get => slideThresold; }

        private HandGhost _previousGhost;
        private SnapSurface _previousSurface;
        private Transform _previousRelativeTo;

        private Transform GripPoint { get => this.transform; }

        private void OnValidate()
        {
            if(ghost != _previousGhost)
            {
                WireGhost();
            }
            if (surface != _previousSurface)
            {
                WireSurface();
            }
            if(_previousRelativeTo != relativeTo)
            {
                WireGhost();
                WireSurface();
                _previousRelativeTo = relativeTo;
            }
        }

        private void RefreshGhostPose()
        {
            pose = ghost.ReadPose(relativeTo);
        }

        public HandPose SavePose()
        {
            return pose;
        }

        public void LoadPose(HandPose snapPose, Transform relativeTo)
        {
            pose = snapPose;
            this.relativeTo = relativeTo;

            this.transform.localPosition = snapPose.relativeGrip.position;
            this.transform.localRotation = snapPose.relativeGrip.rotation;

            LoadGhost();
            LoadDefaultSurface();
        }

        public void LoadGhost()
        {
            ghost = Instantiate(HandGhostProvider.Instance.GetHand(pose.handeness), this.transform);
            WireGhost();
        }

        private void LoadDefaultSurface()
        {
            surface = this.gameObject.AddComponent<CylinderSurface>();
            WireSurface();
        }

        private void WireGhost()
        {
            if (_previousGhost != null)
            {
                _previousGhost.OnDirty -= RefreshGhostPose;
            }
            if (ghost != null)
            {
                ghost.SetPose(pose, relativeTo);
                ghost.OnDirty += RefreshGhostPose;
            }
            _previousGhost = ghost;
        }

        private void WireSurface()
        {
            if (surface != null)
            {
                surface.relativeTo = RelativeTo;
            }
            _previousSurface = surface;
        }

        public HandPose InvertedPose()
        {
            if (surface != null)
            {
                return surface.InvertedPose(pose);
            }
            else
            {
                return pose;
            }
        }

        public Vector3 NearestInSurface(Vector3 worldPoint)
        {
            if (surface != null)
            {
                return surface.NearestPointInSurface(worldPoint);
            }
            else
            {
                return GripPoint.position;
            }
        }

        public ScoredHandPose CalculateBestPose(HandPose userPose, float? scoreWeight = null, SnapDirection direction = SnapDirection.Any)
        {
            if (pose.handeness != userPose.handeness)
            {
                return ScoredHandPose.Null();
            }

            scoreWeight = scoreWeight ?? positionRotationWeight;

            ScoredHandPose? bestForwardPose = null;
            ScoredHandPose? bestBackwardPose = null;

            if (direction == SnapDirection.Any
                || direction == SnapDirection.Forward)
            {
                bestForwardPose = CompareNearPoses(userPose, pose, scoreWeight.Value, SnapDirection.Forward);
            }

            if (canInvert
                && (direction == SnapDirection.Any
                || direction == SnapDirection.Backward))
            {
                HandPose invertedPose = InvertedPose();
                bestBackwardPose = CompareNearPoses(userPose, invertedPose, scoreWeight.Value, SnapDirection.Backward);

                if (!bestForwardPose.HasValue
                    || bestBackwardPose.Value.Score > bestForwardPose.Value.Score)
                {
                    return bestBackwardPose.Value;
                }
            }
            return bestForwardPose ?? bestBackwardPose.Value;
        }

        private ScoredHandPose CompareNearPoses(HandPose userPose, HandPose snapPose, float scoreWeight, SnapDirection direction)
        {
            Pose desired = userPose.ToPose(relativeTo);
            Pose snap = snapPose.ToPose(relativeTo);
            Pose similarPlace = surface ? surface.SimilarPlaceAtVolume(desired, snap) : snap;
            Pose nearestPlace = surface ? surface.NearestPlaceAtVolume(desired, snap) : snap;
            Pose bestForwardPlace = SelectBestPose(similarPlace, nearestPlace, desired, scoreWeight, out float bestScore);
            HandPose adjustedPose = snapPose.AdjustPose(bestForwardPlace, relativeTo);
            return new ScoredHandPose(adjustedPose, bestScore, direction);
        }

        private Pose SelectBestPose(Pose a, Pose b, Pose reference, float normalisedWeight, out float bestScore)
        {
            float aScore = Similitude(reference, a, maxDistance);
            float bScore = Similitude(reference, b, maxDistance);
            if (aScore * normalisedWeight >= bScore * (1f - normalisedWeight))
            {
                bestScore = aScore;
                return a;
            }
            bestScore = bScore;
            return b;
        }

        private float Similitude(Pose from, Pose to, float maxDistance)
        {
            float forwardDifference = Vector3.Dot(from.rotation * Vector3.forward, to.rotation * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from.rotation * Vector3.up, to.rotation * Vector3.up) * 0.5f + 0.5f;
            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(from.position, to.position) / maxDistance);
            return forwardDifference * upDifference * positionDifference;
        }
    }
}
