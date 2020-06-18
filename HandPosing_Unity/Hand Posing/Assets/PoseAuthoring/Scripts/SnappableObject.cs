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
            foreach (var pose in posesCollection.SnapPoses)
            {
                AddPose(pose);
            }
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
            HandPose pose = puppet.CurrentPose(this.transform);
            return AddPose(pose);
        }

        private HandGhost AddPose(HandPose pose)
        {
            HandGhost ghost = Instantiate(handProvider.GetHand(pose.isRightHand), this.transform);
            ghost.SetPose(pose, this.transform);
            this.ghosts.Add(ghost);
            return ghost;
        }


        public HandGhost FindNearsetGhost(HandPuppet hand, out float score)
        {
            HandPose pose = hand.CurrentPose(this.transform);
            float maxScore = 0f;
            HandGhost nearestHand = null;
            foreach(var ghost in this.ghosts)
            {
               float poseScore = HandPose.Score(ghost.StoredPose, pose);
                if(poseScore > maxScore)
                {
                    nearestHand = ghost;
                    maxScore = poseScore;
                }
            }
            score = maxScore;
            return nearestHand;
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
            List<HandPose> poses = new List<HandPose>();
            foreach (var ghost in this.ghosts)
            {
                poses.Add(ghost.StoredPose);
            }
            posesCollection.StorePoses(poses);
        }
    }
}