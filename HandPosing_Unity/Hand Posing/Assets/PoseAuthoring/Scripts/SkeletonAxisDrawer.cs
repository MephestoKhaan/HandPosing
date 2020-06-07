using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public class SkeletonAxisDrawer : MonoBehaviour
    {
        [SerializeField]
        private OVRSkeleton skeleton;
        [SerializeField]
        private Transform axisPrototype;

        private Transform[] axises;


        private void InitializeAxis(OVRSkeleton skeleton)
        {
            axises = new Transform[skeleton.Bones.Count];
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                axises[i] = Instantiate<Transform>(axisPrototype, this.transform);
            }
        }


        void Update()
        {
            if (skeleton.IsInitialized && skeleton.IsDataValid)
            {
                if(axises == null)
                {
                    InitializeAxis(skeleton);
                }

                for (int i = 0; i < skeleton.Bones.Count; i++)
                {
                    axises[i].SetPositionAndRotation(skeleton.Bones[i].Transform.position, skeleton.Bones[i].Transform.rotation);
                }
            }
            
        }
    }
}