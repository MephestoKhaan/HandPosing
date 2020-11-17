using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring.Adapters
{
    public abstract class SkeletonDataProvider : MonoBehaviour
    {
        public abstract bool IsTracking { get; }

        public abstract List<HandBone> Bones { get; }
    }
}