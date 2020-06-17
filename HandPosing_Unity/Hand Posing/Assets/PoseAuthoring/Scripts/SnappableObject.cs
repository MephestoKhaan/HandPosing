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

        private List<HandPuppet> ghosts = new List<HandPuppet>();

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

        public void AddPose(HandPuppet puppet)
        {
            HandPose pose = puppet.CurrentPose(this.transform);
            AddPose(pose);
        }

        public void AddPose(HandPose pose)
        {
            HandPuppet ghost = Instantiate(handProvider.rightHand, this.transform);
            ghost.SetRecordedPose(pose, this.transform);
            ghosts.Add(ghost);
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
                poses.Add(ghost.CurrentPose(this.transform));
            }
            posesCollection.StorePoses(poses);
        }
    }
}