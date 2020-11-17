using PoseAuthoring.Adapters;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public class SkeletonAxisDrawer : MonoBehaviour
    {
        [SerializeField]
        private SkeletonDataProvider skeleton;
        [SerializeField]
        private Transform axisPrototype;

        private Transform[] axises;

        private void InitializeAxis(List<HandBone> bones)
        {
            axises = new Transform[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                axises[i] = Instantiate<Transform>(axisPrototype, this.transform);
            }
        }

        void Update()
        {
            if (skeleton.IsTracking)
            {
                if(axises == null)
                {
                    InitializeAxis(skeleton.Bones);
                }

                for (int i = 0; i < skeleton.Bones.Count; i++)
                {
                    axises[i].SetPositionAndRotation(skeleton.Bones[i].Transform.position,
                        skeleton.Bones[i].Transform.rotation);
                }
            }
            
        }
    }
}