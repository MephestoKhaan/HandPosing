using PoseAuthoring.PoseVolumes;
using UnityEngine;
using static PoseAuthoring.ScoredHandPose;

namespace PoseAuthoring.PoseRecording
{
    public class SnapPose : MonoBehaviour
    {
        public Transform relativeTo;
        public HandPose pose;
        public SnapSurface snapSurface;

        //TODO needs saving? maybe even the surface
        [Space]
        public bool handCanInvert = false;
        public float maxDistance = 0.1f;
        public bool snapsBack = false;
        [Range(0f, 1f)]
        public float positionRotationWeight = 0.5f;
        [Range(0f, 1f)]
        public float slideThresold = 0f;

        [SerializeField]
        private HandGhost _ghost;
        private HandGhost _previousGhost;

        private void OnValidate()
        {
            if(_ghost != _previousGhost)
            {
                WireGhost();
            }
        }

        private void RefreshGhostPose()
        {
            this.pose = _ghost.ReadPose(relativeTo);
        }

        public HandPose SavePose()
        {
            return pose;
        }

        public void LoadPose(HandPose snapPose, Transform relativeTo)
        {
            this.pose = snapPose;
            this.relativeTo = relativeTo;

            //TODO: not great
            this.transform.localPosition = snapPose.relativeGrip.position;
            this.transform.localRotation = snapPose.relativeGrip.rotation;

            LoadGhost();
            LoadDefaultSurface();
        }

        private void LoadDefaultSurface()
        {
            this.snapSurface = this.gameObject.AddComponent<CylinderSurface>();
        }

        public void LoadGhost()
        {
            _ghost = Instantiate(HandGhostProvider.Instance.GetHand(pose.handeness), this.transform);
            WireGhost();
        }

        private void WireGhost()
        {
            if (_previousGhost != null)
            {
                _previousGhost.OnDirty -= RefreshGhostPose;
            }
            if (_ghost != null)
            {
                _ghost.SetPose(pose, relativeTo);
                _ghost.OnDirty += RefreshGhostPose;
            }
            _previousGhost = _ghost;
        }

        public HandPose InvertedPose()
        {
            if (snapSurface != null)
            {
                return snapSurface.InvertedPose(relativeTo, pose);
            }
            else
            {
                return pose;
            }
        }

        public Vector3 NearestInVolume(Vector3 worldPoint)
        {
            if (snapSurface != null)
            {
                return snapSurface.NearestPointInSurface(worldPoint);
            }
            else
            {
                return this.transform.position; //TODO: is it the grip point?
            }
        }

        public ScoredHandPose CalculateBestPlace(HandPose userPose, float? scoreWeight = null, SnapDirection direction = SnapDirection.Any)
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
                bestForwardPose = ComparePoses(userPose, pose, scoreWeight.Value, SnapDirection.Forward);
            }

            if (handCanInvert
                && (direction == SnapDirection.Any
                || direction == SnapDirection.Backward))
            {
                HandPose invertedPose = InvertedPose();
                bestBackwardPose = ComparePoses(userPose, invertedPose, scoreWeight.Value, SnapDirection.Backward);

                if (!bestForwardPose.HasValue
                    || bestBackwardPose.Value.Score > bestForwardPose.Value.Score)
                {
                    return bestBackwardPose.Value;
                }
            }
            return bestForwardPose ?? bestBackwardPose.Value;
        }

        private ScoredHandPose ComparePoses(HandPose userPose, HandPose snapPose, float scoreWeight, SnapDirection direction)
        {
            Pose desired = userPose.ToPose(relativeTo);
            Pose snap = snapPose.ToPose(relativeTo);
            Pose similarPlace = snapSurface ? snapSurface.SimilarPlaceAtVolume(desired, snap, relativeTo) : snap;
            Pose nearestPlace = snapSurface ? snapSurface.NearestPlaceAtVolume(desired, snap, relativeTo) : snap;
            Pose bestForwardPlace = SelectBestPose(similarPlace, nearestPlace, desired, scoreWeight, out float bestScore);
            HandPose adjustedPose = snapPose.AdjustPose(bestForwardPlace, relativeTo);
            return new ScoredHandPose(adjustedPose, bestScore, direction);
        }

        private Pose SelectBestPose(Pose a, Pose b, Pose comparer, float normalisedWeight, out float bestScore)
        {
            float aScore = Score(comparer, a);
            float bScore = Score(comparer, b);
            if (aScore * normalisedWeight >= bScore * (1f - normalisedWeight))
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
            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(from.position, to.position) / maxDistance);
            return forwardDifference * upDifference * positionDifference;
        }
    }
}
