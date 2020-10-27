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
        [SerializeField]
        private bool canSlide = false;
        [SerializeField]
        [Range(0f,1f)]
        private float positionToRotationWeight = 0.5f;

        private List<HandGhost> ghosts = new List<HandGhost>();

        public float PositionRotationWeight
        {
            get
            {
                return positionToRotationWeight;
            }
        }
        public bool HandSlides
        {
            get
            {
                return canSlide;
            }
        }
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
            HandSnapPose pose = puppet.VisualPose(this.transform);
            HandGhost ghost = Instantiate(handProvider.GetHand(pose.handeness), this.transform);
            ghost.SetPose(pose, this);
            this.ghosts.Add(ghost);
            return ghost;
        }

        private HandGhost AddPose(VolumetricPose poseVolume)
        {
            HandGhost ghost = Instantiate(handProvider.GetHand(poseVolume.pose.handeness), this.transform);
            ghost.SetPoseVolume(poseVolume, this);
            this.ghosts.Add(ghost);

            return ghost;
        }

        public HandGhost FindBestGhost(HandSnapPose userPose, out ScoredSnapPose bestSnapPose)
        {
            HandGhost nearestGhost = null;
            ScoredSnapPose bestPlace = ScoredSnapPose.Null();
            foreach (var ghost in this.ghosts)
            {
                ScoredSnapPose snapPose = ghost.CalculateBestPlace(userPose);
                if (snapPose.Score > bestPlace.Score)
                {
                    nearestGhost = ghost;
                    bestPlace = snapPose;
                }
            }
            bestSnapPose = bestPlace;
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