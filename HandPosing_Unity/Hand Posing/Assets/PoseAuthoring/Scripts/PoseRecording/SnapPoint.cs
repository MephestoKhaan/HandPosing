using PoseAuthoring.PoseVolumes;
using UnityEngine;

namespace PoseAuthoring.PoseRecording
{
    [System.Serializable]
    public struct SnapPointData
    {
        public HandPose pose;
        public SnapSurfaceData surfaceData;
        public bool canInvert;
        public float maxDistance;
        public float positionRotationWeight;
        public bool snapsBack;
        public float slideThresold;
    }

    public class SnapPoint : MonoBehaviour
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

        [Space]
        [SerializeField]
        private bool canInvert = false;
        [SerializeField]
        private bool snapsBack = false;
        [SerializeField]
        private float maxDistance = 0.1f;
        [SerializeField]
        [Range(0f, 1f)]
        private float positionRotationWeight = 0.5f;
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
            if (ghost != _previousGhost)
            {
                WireGhost();
            }
            if (surface != _previousSurface)
            {
                WireSurface();
            }
            if (_previousRelativeTo != relativeTo)
            {
                WireGhost();
                WireSurface();
                _previousRelativeTo = relativeTo;
            }
        }

        private void RefreshGhostPose()
        {
            this.pose = ghost.ReadPose(relativeTo);
        }

        public SnapPointData SaveData()
        {
            return new SnapPointData()
            {
                pose = this.pose,
                surfaceData = this.surface?.Data,
                canInvert = this.canInvert,
                maxDistance = this.maxDistance,
                positionRotationWeight = this.positionRotationWeight,
                snapsBack = this.snapsBack,
                slideThresold = this.slideThresold
            };
        }

        public void LoadData(SnapPointData data, Transform relativeTo)
        {
            SetPose(data.pose, relativeTo);
            this.canInvert = data.canInvert;
            this.maxDistance = data.maxDistance;
            this.positionRotationWeight = data.positionRotationWeight;
            this.snapsBack = data.snapsBack;
            this.slideThresold = data.slideThresold;
            //this.surface.Data = data.surfaceData;
        }

        public void SetPose(HandPose snapPose, Transform relativeTo)
        {
            pose = snapPose;
            this.relativeTo = relativeTo;

            //this.transform.localPosition = snapPose.relativeGrip.position;
            //this.transform.localRotation = snapPose.relativeGrip.rotation;
        }

        public void LoadGhost(HandGhostProvider ghostProvider)
        {
            HandGhost ghostPrototype = ghostProvider?.GetHand(pose.handeness);
            if (ghostPrototype != null)
            {
                ghost = Instantiate(ghostPrototype, this.transform);
                WireGhost();
            }
            else
            {
                Debug.LogError("No HandGhostProvider", this);
            }
        }

        private void LoadSurface(SnapSurfaceData surfaceData)
        {
            if (this.surface != null)
            {
                Destroy(this.surface);
                this.surface = null;
            }

            this.surface = this.gameObject.AddComponent(surfaceData.SurfaceType) as SnapSurface;
            this.surface.Data = surfaceData;
        }

        private void WireGhost()
        {
            if (_previousGhost != null)
            {
                _previousGhost.OnDirtyBones -= RefreshGhostPose;
            }
            if (ghost != null)
            {
                ghost.SetPose(pose, relativeTo);
                ghost.OnDirtyBones += RefreshGhostPose;
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
