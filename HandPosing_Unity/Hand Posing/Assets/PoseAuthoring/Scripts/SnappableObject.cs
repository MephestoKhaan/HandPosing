using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public class SnappableObject : MonoBehaviour
    {
        [SerializeField]
        private PosesCollection posesCollection;
        [SerializeField]
        public HandProvider handProvider;
        [InspectorButton("SaveToAsset")]
        public string StorePoses;

        [Space]
        [SerializeField]
        protected Collider[] snapPoints = null;
        public Collider[] SnapPoints
        {
            get
            {
                return snapPoints;
            }
        }

        private List<HandGhost> ghosts = new List<HandGhost>();

        private void Awake()
        {
            InitializeSnapPoints();
        }

        private void Start()
        {
            LoadFromAsset();
        }

        private void InitializeSnapPoints()
        {
            if (snapPoints == null
                || snapPoints.Length == 0)
            {
                if (!this.TryGetComponent(out Collider collider))
                {
                    throw new System.ArgumentException("Snappables cannot have zero grab points and no collider -- please add a grab point or collider.");
                }
                snapPoints = new Collider[1] { collider };
            }
        }

        public HandGhost AddPose(HandPuppet puppet)
        {
            HandSnapPose pose = puppet.CurrentPoseVisual(this.transform);
            return AddPose(pose);
        }

        private HandGhost AddPose(HandSnapPose pose)
        {
            HandGhost ghost = Instantiate(handProvider.GetHand(pose.isRightHand), this.transform);
            ghost.SetPose(pose, this.transform);
            this.ghosts.Add(ghost);
            return ghost;
        }

        public HandGhost FindNearsetGhost(HandPuppet hand, out float score)
        {
            HandSnapPose handToObject = hand.CurrentPoseVisual(this.transform);
            float maxScore = 0f;
            (Vector3, Quaternion) bestSurfacePose;
            HandGhost nearestGhost = null;
            foreach (var ghost in this.ghosts)
            {
                float poseScore = ghost.Score(handToObject, this.transform, out (Vector3, Quaternion) surfacePose);
                if (poseScore > maxScore)
                {
                    nearestGhost = ghost;
                    maxScore = poseScore;
                    bestSurfacePose = surfacePose;
                }
            }
            score = maxScore;
            return nearestGhost;
        }

        public void LoadFromAsset()
        {
            foreach (var pose in posesCollection.SnapPoses)
            {
                AddPose(pose);
            }
        }

        public void SaveToAsset()
        {
            List<HandSnapPose> poses = new List<HandSnapPose>();
            foreach (var ghost in this.ghosts)
            {
                poses.Add(ghost.PoseToObject);
            }
            posesCollection.StorePoses(poses);
        }
    }
}