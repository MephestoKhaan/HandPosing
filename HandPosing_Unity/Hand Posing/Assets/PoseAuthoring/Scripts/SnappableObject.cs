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

        private List<HandGhost> ghosts = new List<HandGhost>();

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

        public HandGhost FindNearsetGhost(HandSnapPose userPose, out float score, out (Vector3, Quaternion) bestPose)
        {
            float maxScore = 0f;
            HandGhost nearestGhost = null;
            bestPose = (Vector3.zero, Quaternion.identity);
            foreach (var ghost in this.ghosts)
            {
                float poseScore = ghost.CalculateBestPose(userPose, out var pose);
                if (poseScore > maxScore)
                {
                    nearestGhost = ghost;
                    maxScore = poseScore;
                    bestPose = pose;
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
            foreach (var ghost in this.GetComponentsInChildren<HandGhost>())
            {
                ghost.RefreshPose(this.transform);
                volumetricPoses.Add(ghost.SnapPoseVolume);
            }
            volumetricPosesCollection.StorePoses(volumetricPoses);
        }
    }
}