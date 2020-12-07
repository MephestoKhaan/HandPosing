using HandPosing.SnapSurfaces;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    [System.Serializable]
    public struct SnapPointData
    {
        public HandPose pose;
        public SnapSurfaceData surfaceData;
        public float maxDistance;
        public bool snapsBack;
        public float slideThresold;
        public float positionRotationWeight;
    }

    [System.Serializable]
    public abstract class BaseSnapPoint : MonoBehaviour
    {
        [SerializeField]
        protected Transform relativeTo;
        [SerializeField]
        protected bool snapsBack;
        [SerializeField]
        [Range(0f,1f)]
        protected float slideThresold;

        public Transform RelativeTo { get => relativeTo; }
        public bool SnapsBack { get => snapsBack; }
        public float SlideThresold { get => slideThresold; }

        public abstract ScoredHandPose CalculateBestPose(HandPose userPose, float? scoreWeight = null, SnapDirection direction = SnapDirection.Any, float scale = 1f);
        public abstract Vector3 NearestInSurface(Vector3 worldPoint, float scale = 1f);

        public abstract void DestroyImmediate();
    }
}