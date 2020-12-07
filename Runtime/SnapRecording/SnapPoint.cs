using HandPosing.SnapSurfaces;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    [ExecuteInEditMode]
    public class SnapPoint : BaseSnapPoint
    {
        [SerializeField]
        private HandPose pose;

        [Space]
        [SerializeField]
        private SnapSurface surface;
        [SerializeField]
        private HandGhost ghost;
        [Space]

        [InspectorButton("Mirror")]
        public string CreateMirror;

        [Space]
        [SerializeField]
        private float maxDistance = 0.1f;
        [SerializeField]
        [Range(0f, 1f)]
        private float positionRotationWeight = 0.5f;

        private HandGhost _previousGhost;
        private SnapSurface _previousSurface;
        private Transform _previousRelativeTo;

        private Transform GripPoint { get => this.transform; }

        public float Scale
        {
            get
            {
                return this.transform.localScale.x;
            }
        }

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

        #region generation

        public static SnapPoint Create(Transform parent)
        {
            GameObject go = new GameObject("Snap Point");
            go.transform.SetParent(parent, false);
            SnapPoint record = go.AddComponent<SnapPoint>();
            return record;
        }

        public SnapPoint Mirror()
        {
            SnapPoint record = Create(this.transform.parent);
            record.gameObject.name = $"{this.gameObject.name}_Mirror";
            SnapPointData mirrorData = this.SaveData();
            mirrorData.surfaceData = mirrorData.surfaceData?.Mirror();
            mirrorData.pose.handeness = this.pose.handeness == Handeness.Left ? Handeness.Right : Handeness.Left;

            if (this.surface != null)
            {
                mirrorData.pose.relativeGrip = this.surface.MirrorPose(mirrorData.pose.relativeGrip);
            }
            else
            {
                mirrorData.pose.relativeGrip = mirrorData.pose.relativeGrip.MirrorPose(Vector3.forward, Vector3.up);
                Vector3 translation = Vector3.Project(mirrorData.pose.relativeGrip.position, Vector3.right);
                mirrorData.pose.relativeGrip.position = mirrorData.pose.relativeGrip.position - 2f * translation;
            }

            record.LoadData(mirrorData, this.RelativeTo);
            return record;
        }


        #endregion

#if UNITY_EDITOR
        private void Update()
        {
            if (this.transform.hasChanged)
            {
                this.transform.hasChanged = false;
                this.pose.relativeGrip = this.transform.GetPose(Space.Self);
                ghost?.SetPose(this.pose, this.relativeTo);
            }
        }
#endif

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
                maxDistance = this.maxDistance,
                positionRotationWeight = this.positionRotationWeight,
                snapsBack = this.snapsBack,
                slideThresold = this.slideThresold
            };
        }

        public void LoadData(SnapPointData data, Transform relativeTo)
        {
            SetPose(data.pose, relativeTo);
            this.maxDistance = data.maxDistance;
            this.positionRotationWeight = data.positionRotationWeight;
            this.snapsBack = data.snapsBack;
            this.slideThresold = data.slideThresold;
            LoadSurface(data.surfaceData);
        }

        public void SetPose(HandPose snapPose, Transform relativeTo)
        {
            pose = snapPose;
            this.relativeTo = relativeTo;
            this.transform.SetPose(snapPose.relativeGrip, Space.Self);
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
                Debug.LogWarning("No HandGhostProvider", this);
            }
        }

        private void LoadSurface(SnapSurfaceData surfaceData)
        {
            if (surfaceData == null)
            {
                return;
            }

            if (this.surface != null)
            {
                Destroy(this.surface);
                this.surface = null;
            }

            this.surface = this.gameObject.AddComponent(surfaceData.SurfaceType) as SnapSurface;
            this.surface.Data = surfaceData;
            WireSurface();
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

        public override Vector3 NearestInSurface(Vector3 worldPoint, float scale = 1f)
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

        public override ScoredHandPose CalculateBestPose(HandPose userPose, float? scoreWeight = null, SnapDirection direction = SnapDirection.Any , float scale = 1f)
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
            return bestForwardPose ?? bestBackwardPose.Value;
        }

        private ScoredHandPose CompareNearPoses(HandPose userPose, HandPose snapPose, float scoreWeight, SnapDirection direction)
        {
            Pose desired = userPose.ToPose(relativeTo);
            Pose snap = snapPose.ToPose(relativeTo);
            Pose similarPlace = surface ? surface.MinimalRotationPoseAtSurface(desired, snap) : snap;
            Pose nearestPlace = surface ? surface.MinimalTranslationPoseAtSurface(desired, snap) : snap;
            Pose bestForwardPlace = SelectBestPose(similarPlace, nearestPlace, desired, scoreWeight, out float bestScore);
            HandPose adjustedPose = snapPose.AdjustPose(bestForwardPlace, relativeTo);
            return new ScoredHandPose(adjustedPose, bestScore, direction);
        }

        private Pose SelectBestPose(Pose a, Pose b, Pose reference, float normalisedWeight, out float bestScore)
        {
            float aScore = PoseUtils.Similitude(reference, a, maxDistance);
            float bScore = PoseUtils.Similitude(reference, b, maxDistance);
            if (aScore * normalisedWeight >= bScore * (1f - normalisedWeight))
            {
                bestScore = aScore;
                return a;
            }
            bestScore = bScore;
            return b;
        }

        public override void DestroyImmediate()
        {
            DestroyImmediate(this.gameObject);
        }
    }
}
