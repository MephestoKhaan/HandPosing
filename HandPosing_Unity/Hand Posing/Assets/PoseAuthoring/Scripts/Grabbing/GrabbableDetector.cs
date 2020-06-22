using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public class GrabbableDetector : MonoBehaviour
    {
        [SerializeField]
        protected Collider[] m_grabVolumes = null;
        [SerializeField]
        private Transform m_gripTransform;

        protected Dictionary<SnappableObject, int> snappableCandidates = new Dictionary<SnappableObject, int>();

        void OnTriggerEnter(Collider otherCollider)
        {
            SnappableObject snappable = otherCollider.GetComponent<SnappableObject>() ?? otherCollider.GetComponentInParent<SnappableObject>();
            if (snappable == null)
            {
                return;
            }

            int refCount = 0;
            snappableCandidates.TryGetValue(snappable, out refCount);
            snappableCandidates[snappable] = refCount + 1;
        }

        void OnTriggerExit(Collider otherCollider)
        {
            SnappableObject snappable = otherCollider.GetComponent<SnappableObject>() ?? otherCollider.GetComponentInParent<SnappableObject>();
            if (snappable == null)
            {
                return;
            }

            bool found = snappableCandidates.TryGetValue(snappable, out int refCount);
            if (!found)
            {
                return;
            }

            if (refCount > 1)
            {
                snappableCandidates[snappable] = refCount - 1;
            }
            else
            {
                snappableCandidates.Remove(snappable);
            }
        }


        public SnappableObject NearsestSnappable()
        {
            float closestMagSq = float.MaxValue;
            SnappableObject closestGrabbable = null;

            foreach (SnappableObject snappable in snappableCandidates.Keys)
            {
                for (int j = 0; j < snappable.SnapPoints.Length; ++j)
                {
                    Collider grabbableCollider = snappable.SnapPoints[j];

                    Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(m_gripTransform.position);
                    float grabbableMagSq = (m_gripTransform.position - closestPointOnBounds).sqrMagnitude;
                    if (grabbableMagSq < closestMagSq)
                    {
                        closestMagSq = grabbableMagSq;
                        closestGrabbable = snappable;
                    }
                }
            }

            return closestGrabbable;
        }
    }
}