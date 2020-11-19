using System;
using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.Interaction
{
    public abstract class BaseGrabber : MonoBehaviour, IGrabNotifier
    {
        [SerializeField]
        private Transform gripTransform = null;
        [SerializeField]
        private Collider[] grabVolumes = null;
        [SerializeField]
        private AnchorsUpdateNotifier updateNotifier;

        private Pose _grabbedObjectOffset;

        private bool _usingUpdateNotifier;
        private bool _grabVolumeEnabled = true;
        private float _prevFlex;
        private Dictionary<Grabbable, int> _grabCandidates = new Dictionary<Grabbable, int>();
        private bool _nearGrab = false;

        public Grabbable GrabbedObject { get; private set; } = null;

        public Action<GameObject> OnGrabStarted { get; set; }
        public Action<GameObject, float> OnGrabAttemp { get; set; }
        public Action<GameObject> OnGrabEnded { get; set; }

        public abstract Vector2 GrabFlexThresold { get; }
        public abstract float CurrentFlex();
        protected abstract (Vector3, Vector3) HandRelativeVelocity(Pose to);

        #region clearer
        private static HashSet<BaseGrabber> allGrabbers = new HashSet<BaseGrabber>();
        public static void ClearAllGrabs(Grabbable grabbable)
        {
            foreach (var grabber in allGrabbers)
            {
                grabber.ForceUntouch(grabbable);
                grabber.ForceRelease(grabbable);
            }
        }
        #endregion

        protected virtual void Reset()
        {
            gripTransform = this.GetComponent<HandPuppet>()?.Grip;
            updateNotifier = this.GetComponentInParent<AnchorsUpdateNotifier>();
        }

        protected virtual void Awake()
        {
            allGrabbers.Add(this);
        }

        protected virtual void OnEnable()
        {
            if (updateNotifier != null)
            {
                updateNotifier.OnAnchorsFirstUpdate += UpdateAnchors;
                _usingUpdateNotifier = true;
            }
            else
            {
                _usingUpdateNotifier = false;
            }
        }

        protected virtual void OnDisable()
        {
            if (_usingUpdateNotifier)
            {
                updateNotifier.OnAnchorsFirstUpdate -= UpdateAnchors;
            }
            foreach (var grabbable in new List<Grabbable>(_grabCandidates.Keys))
            {
                ForceUntouch(grabbable);
                ForceRelease(grabbable);
            }
        }

        protected virtual void OnDestroy()
        {
            if (GrabbedObject != null)
            {
                GrabEnd();
            }
            allGrabbers.Remove(this);
        }

        public void ForceRelease(Grabbable grabbable)
        {
            bool canRelease = (
                (GrabbedObject != null) &&
                (GrabbedObject == grabbable || grabbable == null)
            );
            if (canRelease)
            {
                GrabEnd();
            }
        }

        public void ForceUntouch(Grabbable grabbable)
        {
            if (_grabCandidates.ContainsKey(grabbable))
            {
                _grabCandidates.Remove(grabbable);
            }
        }

        protected virtual void Update()
        {
            if(!_usingUpdateNotifier)
            {
                UpdateAnchors();
            }
        }

        private void UpdateAnchors()
        {
            UpdateGrabStates();
        }

        protected void UpdateGrabStates()
        {
            float prevFlex = _prevFlex;
            _prevFlex = CurrentFlex();
            CheckForGrabOrRelease(prevFlex, _prevFlex);
            MoveGrabbedObject(transform.position, transform.rotation);
        }

        protected void CheckForGrabOrRelease(float prevFlex, float currentFlex)
        {
            if (prevFlex < GrabFlexThresold.y
                && currentFlex >= GrabFlexThresold.y)
            {
                _nearGrab = false;
                GrabBegin();
            }
            else if (prevFlex > GrabFlexThresold.x
                && currentFlex <= GrabFlexThresold.x)
            {
                GrabEnd();
            }

            if (GrabbedObject == null && currentFlex > 0)
            {
                _nearGrab = true;
                NearGrab(currentFlex / GrabFlexThresold.y);
            }
            else if (_nearGrab)
            {
                _nearGrab = false;
                NearGrab(0f);
            }
        }

        private void NearGrab(float factor)
        {
            if (factor == 0f)
            {
                OnGrabAttemp?.Invoke(null, 0f);
                return;
            }

            (Grabbable, Collider) closestGrabbable = FindClosestGrabbable();
            if (closestGrabbable.Item1 != null)
            {
                OnGrabAttemp?.Invoke(closestGrabbable.Item1.gameObject, factor);
            }
            else
            {
                OnGrabAttemp?.Invoke(null, 0f);
            }
        }

        protected virtual void GrabBegin()
        {
            Grabbable closestGrabbable;
            Collider closestGrabbableCollider;
            (closestGrabbable, closestGrabbableCollider) = FindClosestGrabbable();

            GrabVolumeEnable(false);
            if (closestGrabbable != null)
            {
                Grab(closestGrabbable, closestGrabbableCollider);
            }
        }

        protected virtual void Grab(Grabbable closestGrabbable, Collider closestGrabbableCollider)
        {
            if (closestGrabbable.IsGrabbed)
            {
                closestGrabbable.GrabbedBy.OffhandGrabbed(closestGrabbable);
            }

            GrabbedObject = closestGrabbable;
            GrabbedObject.GrabBegin(this, closestGrabbableCollider);

            OnGrabStarted?.Invoke(GrabbedObject?.gameObject);

            _grabbedObjectOffset = new Pose();
            _grabbedObjectOffset.position = Quaternion.Inverse(transform.rotation) * (GrabbedObject.transform.position - transform.position);
            _grabbedObjectOffset.rotation = Quaternion.Inverse(transform.rotation) * GrabbedObject.transform.rotation;
        }

        protected virtual void MoveGrabbedObject(Vector3 pos, Quaternion rot)
        {
            if (GrabbedObject == null)
            {
                return;
            }
            Vector3 grabbablePosition = pos + rot * _grabbedObjectOffset.position;
            Quaternion grabbableRotation = rot * _grabbedObjectOffset.rotation;
            GrabbedObject.MoveTo(grabbablePosition, grabbableRotation);
        }

        protected virtual void GrabEnd()
        {
            if (GrabbedObject != null)
            {
                Vector3 linearVelocity, angularVelocity;
                (linearVelocity, angularVelocity) = HandRelativeVelocity(_grabbedObjectOffset);
                ReleaseGrabbedObject(linearVelocity, angularVelocity);
            }

            GrabVolumeEnable(true);
        }

        protected void ReleaseGrabbedObject(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            GrabbedObject.GrabEnd(linearVelocity, angularVelocity);
            OnGrabEnded?.Invoke(GrabbedObject?.gameObject);
            GrabbedObject = null;
        }

        protected virtual void OffhandGrabbed(Grabbable grabbable)
        {
            if (GrabbedObject == grabbable)
            {
                ReleaseGrabbedObject(Vector3.zero, Vector3.zero);
            }
        }

        #region grabbable detection

        public Snappable FindClosestSnappable()
        {
            var closestGrabbable = FindClosestGrabbable();
            return closestGrabbable.Item1?.GetComponent<Snappable>();
        }

        private (Grabbable, Collider) FindClosestGrabbable()
        {
            float closestMagSq = float.MaxValue;
            Grabbable closestGrabbable = null;
            Collider closestGrabbableCollider = null;

            foreach (Grabbable grabbable in _grabCandidates.Keys)
            {
                Collider collider = FindClosestCollider(grabbable, out float distance);
                if (distance < closestMagSq)
                {
                    closestMagSq = distance;
                    closestGrabbable = grabbable;
                    closestGrabbableCollider = collider;
                }
            }
            return (closestGrabbable, closestGrabbableCollider);
        }

        private Collider FindClosestCollider(Grabbable grabbable, out float score)
        {
            float closestMagSq = float.MaxValue;
            Collider closestGrabbableCollider = null;

            for (int j = 0; j < grabbable.GrabPoints.Length; ++j)
            {
                Collider grabbableCollider = grabbable.GrabPoints[j];
                if (grabbableCollider == null)
                {
                    continue;
                }
                Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(gripTransform.position);
                float grabbableMagSq = (gripTransform.position - closestPointOnBounds).sqrMagnitude;
                if (grabbableMagSq < closestMagSq)
                {
                    closestMagSq = grabbableMagSq;
                    closestGrabbableCollider = grabbableCollider;
                }
            }
            score = closestMagSq;
            return closestGrabbableCollider;
        }

        private void GrabVolumeEnable(bool enabled)
        {
            if (_grabVolumeEnabled == enabled)
            {
                return;
            }

            _grabVolumeEnabled = enabled;
            for (int i = 0; i < grabVolumes.Length; ++i)
            {
                Collider grabVolume = grabVolumes[i];
                grabVolume.enabled = _grabVolumeEnabled;
            }

            if (!_grabVolumeEnabled)
            {
                _grabCandidates.Clear();
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            Grabbable grabbable = otherCollider.GetComponent<Grabbable>() ?? otherCollider.GetComponentInParent<Grabbable>();
            if (grabbable == null)
            {
                return;
            }

            int refCount = 0;
            _grabCandidates.TryGetValue(grabbable, out refCount);
            _grabCandidates[grabbable] = refCount + 1;
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            Grabbable grabbable = otherCollider.GetComponent<Grabbable>() ?? otherCollider.GetComponentInParent<Grabbable>();
            if (grabbable == null)
            {
                return;
            }
            bool found = _grabCandidates.TryGetValue(grabbable, out int refCount);
            if (!found)
            {
                return;
            }
            if (refCount > 1)
            {
                _grabCandidates[grabbable] = refCount - 1;
            }
            else
            {
                _grabCandidates.Remove(grabbable);
            }
        }
        #endregion

    }
}