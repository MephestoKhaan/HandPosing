using System.Collections.Generic;
using UnityEngine;

namespace HandPosing
{
    public abstract class SkeletonDataProvider : MonoBehaviour
    {
        public abstract bool IsTracking { get; }

        public abstract List<HandBone> Bones { get; }

        public virtual float? HandScale { get => 1f; }
    }
}