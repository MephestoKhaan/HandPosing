using OVRTouchSample;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public class SnappableObject : MonoBehaviour
    {
        [SerializeField]
        private VolumetricPosesCollection volumetricPosesCollection;
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
            HandGhost ghost = Instantiate(handProvider.GetHand(pose.isRightHand), this.transform);
            ghost.SetPose(pose, this.transform);
            this.ghosts.Add(ghost);
            return ghost;
        }

        private HandGhost AddPose(VolumetricPose poseVolume)
        {
            HandGhost ghost = Instantiate(handProvider.GetHand(poseVolume.pose.isRightHand), this.transform);
            ghost.SetPoseVolume(poseVolume, this.transform);
            this.ghosts.Add(ghost);
            return ghost;
        }

        public HandGhost FindNearsetGhost(HandSnapPose userPose, out float score)
        {
            float maxScore = 0f;
            HandGhost nearestGhost = null;
            foreach (var ghost in this.ghosts)
            {
                float poseScore = ghost.Score(userPose);
                if (poseScore > maxScore)
                {
                    nearestGhost = ghost;
                    maxScore = poseScore;
                }
            }
            score = maxScore;
            return nearestGhost;
        }

        public void LoadFromAsset()
        {
            foreach (var volumetricPose in volumetricPosesCollection.Poses)
            {
                AddPose(volumetricPose);
            }
        }

        public void SaveToAsset()
        {
            List<VolumetricPose> volumetricPoses = new List<VolumetricPose>();
            foreach (var ghost in this.ghosts)
            {
                volumetricPoses.Add(ghost.PoseVolume);
            }
            volumetricPosesCollection.StorePoses(volumetricPoses);
        }
    }
}