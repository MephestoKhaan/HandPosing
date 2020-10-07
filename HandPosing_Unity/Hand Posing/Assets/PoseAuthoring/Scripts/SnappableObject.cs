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
        private bool canSnapBack = true;

        private List<HandGhost> ghosts = new List<HandGhost>();


        public bool HandSnapBacks
        {
            get
            {
                return canSnapBack;
            }
        }

        private void Start()
        {
            LoadFromAsset();
        }

        public HandGhost AddPose(HandPuppet puppet)
        {
            HandSnapPose pose = puppet.CurrentPoseVisual(this.transform);
            HandGhost ghost = Instantiate(handProvider.GetHand(pose.handeness), this.transform);
            ghost.SetPose(pose, this.transform);
            this.ghosts.Add(ghost);
            return ghost;
        }

        private HandGhost AddPose(VolumetricPose poseVolume)
        {
            HandGhost ghost = Instantiate(handProvider.GetHand(poseVolume.pose.handeness), this.transform);
            ghost.SetPoseVolume(poseVolume, this.transform);
            this.ghosts.Add(ghost);

            return ghost;
        }

        public HandGhost FindNearsetGhost(HandSnapPose userPose, out float score, out Pose bestPlace)
        {
            float maxScore = 0f;
            HandGhost nearestGhost = null;
            bestPlace = new Pose();
            foreach (var ghost in this.ghosts)
            {
                float poseScore = ghost.CalculateBestPlace(userPose, out var place);
                if (poseScore > maxScore)
                {
                    nearestGhost = ghost;
                    maxScore = poseScore;
                    bestPlace = place;
                }
            }
            score = maxScore;
            return nearestGhost;
        }

        public void LoadFromAsset()
        {
            if(volumetricPosesCollection != null)
            {
                foreach (var volumetricPose in volumetricPosesCollection.Poses)
                {
                    AddPose(volumetricPose.Clone());
                }
            }
        }

        public void SaveToAsset()
        {
            List<VolumetricPose> volumetricPoses = new List<VolumetricPose>();
            foreach (var ghost in this.GetComponentsInChildren<HandGhost>())
            {
                ghost.RefreshPose(this.transform);
                volumetricPoses.Add(ghost.SnapPoseVolume);
            }
            volumetricPosesCollection.StorePoses(volumetricPoses);
        }
    }
}