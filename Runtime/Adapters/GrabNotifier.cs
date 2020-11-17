using System;
using UnityEngine;

namespace PoseAuthoring.Adapters
{
    public class GrabNotifier : MonoBehaviour
    {
        public Action<GameObject> OnGrabStarted;
        public Action<GameObject, float> OnGrabAttemp;
        public Action<GameObject> OnGrabEnded;

        public float AccotedFlex()
        {

        }

        public SnappableObject FindClosestSnappable()
        {
            return null;
        }
    }
}
