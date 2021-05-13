using HandPosing.SnapSurfaces;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    /// <summary>
    /// Serializable data-only version of the SnapPoint so it can be stored when they
    /// are generated at Play-Mode (where Hand-tracking works).
    /// </summary>
    [System.Serializable]
    public struct SnapPointData
    {
        public HandPose pose;
        public SnapSurfaceData surfaceData;
        public float maxDistance;
        public SnapType snapMode;
        public float slideThresold;
        public float positionRotationWeight;
        public float scale;
    }

    /// <summary>
    /// A single snap point. 
    /// When not using different hand-scales this is the main type of BaseSnapPoint to use.
    /// </summary>
    [ExecuteInEditMode]
    public class SnapPoint : BaseSnapPoint
    {
        /// <summary>
        /// Relative pose of the hand at this point.
        /// </summary>
        [SerializeField]
        private HandPose pose;

        [Space]
        /// <summary>
        /// If provided, surface in which this pose is valid
        /// If ommited, just the transform position/rotation can be snapped to.
        /// Not mandatory.
        /// </summary>
        [SerializeField]
        [Tooltip("Not mandatory surface in which this pose is valid.")]
        private SnapSurface surface;
        /// <summary>
        /// If provided, visual representation of the hand pose at this point.
        /// Not mandatory.
        /// </summary>
        [SerializeField]
        [Tooltip("Not mandatory visual representation of the hand pose at this point.")]
        private HandGhost ghost;
        [Space]
        /// <summary>
        /// Creates an Inspector button to create a mirrored duplicate of this point.
        /// </summary>
        [InspectorButton("Mirror")]
        public string CreateMirror;

        [Space]
        /// <summary>
        /// Maximum distance from the surface at which the measures are valid.
        /// </summary>
        [SerializeField]
        private float maxDistance = 0.1f;
        /// <summary>
        /// How much to favour linear distance versus angular distance when scoring poses.
        /// </summary>
        [SerializeField]
        [Range(0f, 1f)]
        private float positionRotationWeight = 0.5f;

        private HandGhost _previousGhost;
        private SnapSurface _previousSurface;
        private Transform _previousRelativeTo;

        /// <summary>
        /// General getter for the grip of the snapPoint, which shoud be exactly at the snapPoint position.
        /// </summary>
        private Transform GripPoint { get => this.transform; }

        /// <summary>
        /// Scale of the recorded hand.
        /// </summary>
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

        /// <summary>
        /// Creates a new SnapPoint under the given object
        /// </summary>
        /// <param name="parent">The relative object for the snap point</param>
        /// <returns>An non-populated SnapPoint</returns>
        public static SnapPoint Create(Transform parent)
        {
            GameObject go = new GameObject("Snap Point");
            go.transform.SetParent(parent, false);
            SnapPoint record = go.AddComponent<SnapPoint>();
            return record;
        }

        /// <summary>
        /// Generates a new SnapPoint that mirrors this one. Left hand becomes right hand and vice-versa.
        /// The mirror axis is defined by the surface of the snap point, if any, if none a best-guess is provided
        /// but note that it can then moved manually in the editor.
        /// </summary>
        /// <returns>A new snapPoint for the opposite hand of this one</returns>
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
            }
        }
#endif
        /// <summary>
        /// Updates the data of the pose from the attached ghost
        /// </summary>
        private void RefreshGhostPose()
        {
            this.pose = ghost.ReadPose(relativeTo);
        }

        /// <summary>
        /// Serializes the data of the SnapPoint so it can be stored 
        /// </summary>
        /// <returns>The struct data to recreate the snap point</returns>
        public SnapPointData SaveData()
        {
            return new SnapPointData()
            {
                pose = this.pose,
                surfaceData = this.surface?.Data,
                maxDistance = this.maxDistance,
                positionRotationWeight = this.positionRotationWeight,
                snapMode = this.snapMode,
                slideThresold = this.slideThresold,
                scale = this.Scale
            };
        }

        /// <summary>
        /// Populates the SnapPoint with the serialized data version
        /// </summary>
        /// <param name="data">The serialized data for the SnapPoint.</param>
        /// <param name="relativeTo">The object the data refers to.</param>
        public void LoadData(SnapPointData data, Transform relativeTo)
        {
            SetPose(data.pose, relativeTo);
            this.maxDistance = data.maxDistance;
            this.positionRotationWeight = data.positionRotationWeight;
            this.slideThresold = data.slideThresold;
            this.snapMode = data.snapMode;
            this.transform.localScale = Vector3.one * data.scale;
            LoadSurface(data.surfaceData);
        }

        /// <summary>
        /// Applies the given position/rotation to the SnapPoint
        /// </summary>
        /// <param name="snapPose">Relative hand position/rotation.</param>
        /// <param name="relativeTo">Reference coordinates for the pose.</param>
        public void SetPose(HandPose snapPose, Transform relativeTo)
        {
            pose = snapPose;
            this.relativeTo = relativeTo;
            this.transform.SetPose(snapPose.relativeGrip, Space.Self);
        }

        /// <summary>
        /// Creates a visual representation of the Hand position at this point using a Hand ghost
        /// </summary>
        /// <param name="ghostProvider">The prefabs collection for the ghosts generation</param>
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

        /// <summary>
        /// Populates the surface of the SnapPoint from the stored data
        /// </summary>
        /// <param name="surfaceData">Data-only version of the surface.</param>
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

        /// <summary>
        /// Connects all the dependencies of the visual-representation of the hand so it
        /// updates with the data.
        /// </summary>
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

        /// <summary>
        /// Connects the dependencies of the visual representation of the surface.
        /// </summary>
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

        /// <summary>
        /// Finds the most similar pose at this SnapPoint to the user hand pose
        /// </summary>
        /// <param name="userPose">The user current hand pose.</param>
        /// <param name="snapPose">The snap point hand pose.</param>
        /// <param name="scoreWeight">Position to rotation scoring ratio.</param>
        /// <param name="direction">Direction in which the snapping ocurred.</param>
        /// <returns>The adjusted best pose at the surface.</returns>
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

        /// <summary>
        /// Compares two poses to a reference and returns the most similar one
        /// </summary>
        /// <param name="a">First pose to compare with the reference.</param>
        /// <param name="b">Second pose to compare with the reference.</param>
        /// <param name="reference">Reference pose to measure from.</param>
        /// <param name="normalisedWeight">Position to rotation scoring ratio.</param>
        /// <param name="bestScore">Out value with the score of the best pose.</param>
        /// <returns>The most similar pose to reference out of a and b</returns>
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
