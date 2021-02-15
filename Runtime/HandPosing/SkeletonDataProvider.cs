using System.Collections.Generic;
using UnityEngine;

namespace HandPosing
{
    /// <summary>
    /// Abstract class to translate the hand-tracking data from specific systems to the one used by the HandPosing system.
    /// Implement this class for example to translate the hand-tracking data from Oculus or HTC Vive to a common one.
    /// One script is needed per hand.
    /// </summary>
    public abstract class SkeletonDataProvider : MonoBehaviour
    {
        /// <summary>
        /// True is the tracking data has been initialised and it is estable.
        /// </summary>
        public abstract bool IsTracking { get; }

        /// <summary>
        /// Collection of received bones.
        /// </summary>
        public abstract List<HandBone> Bones { get; }

        /// <summary>
        /// Detected scale of the hand.
        /// </summary>
        public virtual float? HandScale { get => 1f; }

    }
}