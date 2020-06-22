using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring.Grabbing
{
    public class Grabber : MonoBehaviour
    {
        public float grabBegin = 0.55f;
        public float grabEnd = 0.35f;

        bool alreadyUpdated = false;

        [SerializeField]
        private OVRHand trackedHand;

        public Transform _gripTransform = null;
        [SerializeField]
        protected Collider[] _grabVolumes = null;

        [SerializeField]
        protected OVRInput.Controller m_controller;

        protected bool _grabVolumeEnabled = true;
        protected Vector3 _lastPos;
        protected Quaternion _lastRot;
        protected Quaternion _anchorOffsetRotation;
        protected Vector3 _anchorOffsetPosition;
        protected float _prevFlex;
        protected Grabbable _grabbedObj = null;
        protected Vector3 _grabbedObjectPosOff;
        protected Quaternion _grabbedObjectRotOff;
        protected Dictionary<Grabbable, int> _grabCandidates = new Dictionary<Grabbable, int>();
        private bool _nearGrab = false;

        public System.Action<Grabbable> OnGrabStarted;
        public System.Action<Grabbable, float> OnGrabAttemp;
        public System.Action<Grabbable> OnGrabEnded;

        public bool IsGrabbing
        {
            get
            {
                return _grabbedObj != null;
            }
        }

        public Grabbable GrabbedObject
        {
            get
            {
                return _grabbedObj;
            }
        }

        private static HashSet<Grabber> allGrabbers = new HashSet<Grabber>();

        public static void ClearAllGrabs(Grabbable grabbable)
        {
            foreach (var grabber in allGrabbers)
            {
                grabber.ForceUntouch(grabbable);
                grabber.ForceRelease(grabbable);
            }
        }

        public void ForceRelease(Grabbable grabbable)
        {
            bool canRelease = (
                (_grabbedObj != null) &&
                (_grabbedObj == grabbable || grabbable == null)
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

        protected virtual void Awake()
        {
            _anchorOffsetPosition = transform.localPosition;
            _anchorOffsetRotation = transform.localRotation;
            allGrabbers.Add(this);

            OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
            if (rig != null)
            {
                rig.UpdatedAnchors += (r) => OnUpdatedAnchors();
            }
        }



        protected virtual void Start()
        {
            _lastPos = transform.position;
            _lastRot = transform.rotation;
        }

        private void OnDisable()
        {
            foreach (var grabbable in new List<Grabbable>(_grabCandidates.Keys))
            {
                ForceUntouch(grabbable);
                ForceRelease(grabbable);
            }
        }

        virtual public void Update()
        {
            alreadyUpdated = false;
        }

        void OnUpdatedAnchors()
        {
            if (alreadyUpdated)
            {
                return;
            }
            alreadyUpdated = true;

            _lastPos = transform.position;
            _lastRot = transform.rotation;

            float prevFlex = _prevFlex;
            _prevFlex = CurrentFlex();

            CheckForGrabOrRelease(prevFlex);
        }

        public float CurrentFlex()
        {
            if (trackedHand)
            {
                return Math.Max(trackedHand.GetFingerPinchStrength(OVRHand.HandFinger.Index),
                     trackedHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle));
            }
            else
            {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, m_controller);
            }
        }

        void OnDestroy()
        {
            if (_grabbedObj != null)
            {
                GrabEnd();
            }
            allGrabbers.Remove(this);
        }

        public void OnTriggerEnter(Collider otherCollider)
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

        public void OnTriggerExit(Collider otherCollider)
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

        protected void CheckForGrabOrRelease(float prevFlex)
        {
            if ((_prevFlex >= grabBegin) && (prevFlex < grabBegin))
            {
                _nearGrab = false;
                GrabBegin();
            }
            else if ((_prevFlex <= grabEnd) && (prevFlex > grabEnd))
            {
                GrabEnd();
            }
            else if (GrabbedObject == null && _prevFlex > 0)
            {
                _nearGrab = true;
                NearGrab(_prevFlex / grabBegin);
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
            (Grabbable, Collider) closestGrabbable;
            closestGrabbable = FindClosestGrabbable();
            if (closestGrabbable.Item1 != null)
            {
                OnGrabAttemp?.Invoke(closestGrabbable.Item1, factor);
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

            ForceGrab(closestGrabbable, closestGrabbableCollider);
        }

        public void ForceGrab(Grabbable closestGrabbable, Collider closestGrabbableCollider)
        {
            GrabVolumeEnable(false);

            if (closestGrabbable != null)
            {
                Grab(closestGrabbable, closestGrabbableCollider);
            }
        }

        private void Grab(Grabbable closestGrabbable, Collider closestGrabbableCollider)
        {
            if (closestGrabbable.IsGrabbed)
            {
                closestGrabbable.GrabbedBy.OffhandGrabbed(closestGrabbable);
            }

            _grabbedObj = closestGrabbable;
            _grabbedObj.GrabBegin(this, closestGrabbableCollider);
            OnGrabStarted?.Invoke(_grabbedObj);

            _lastPos = transform.position;
            _lastRot = transform.rotation;

            Vector3 relPos = _grabbedObj.transform.position - transform.position;
            relPos = Quaternion.Inverse(transform.rotation) * relPos;
            _grabbedObjectPosOff = relPos;

            Quaternion relOri = Quaternion.Inverse(transform.rotation) * _grabbedObj.transform.rotation;
            _grabbedObjectRotOff = relOri;


            MoveGrabbedObject(_lastPos, _lastRot, true);
            _grabbedObj.transform.parent = transform;
        }


        public (Grabbable, Collider) FindClosestGrabbable()
        {
            float closestMagSq = float.MaxValue;
            Grabbable closestGrabbable = null;
            Collider closestGrabbableCollider = null;

            // Iterate grab candidates and find the closest grabbable candidate
            foreach (Grabbable grabbable in _grabCandidates.Keys)
            {
                bool canGrab = !grabbable.IsGrabbed;
                if (!canGrab)
                {
                    continue;
                }

                for (int j = 0; j < grabbable.GrabPoints.Length; ++j)
                {
                    Collider grabbableCollider = grabbable.GrabPoints[j];
                    if (grabbableCollider == null)
                    {
                        continue;
                    }
                    // Store the closest grabbable
                    Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(_gripTransform.position);
                    float grabbableMagSq = (_gripTransform.position - closestPointOnBounds).sqrMagnitude;
                    if (grabbableMagSq < closestMagSq)
                    {
                        closestMagSq = grabbableMagSq;
                        closestGrabbable = grabbable;
                        closestGrabbableCollider = grabbableCollider;
                    }
                }
            }
            return (closestGrabbable, closestGrabbableCollider);
        }

        protected virtual void MoveGrabbedObject(Vector3 pos, Quaternion rot, bool forceTeleport = false)
        {
            if (_grabbedObj == null
                || _grabbedObj.GrabbedBody == null)
            {
                return;
            }

            Rigidbody grabbedRigidbody = _grabbedObj.GrabbedBody;
            Vector3 grabbablePosition = pos + rot * _grabbedObjectPosOff;
            Quaternion grabbableRotation = rot * _grabbedObjectRotOff;

            if (forceTeleport)
            {
                grabbedRigidbody.transform.position = grabbablePosition;
                grabbedRigidbody.transform.rotation = grabbableRotation;
            }
            else
            {
                grabbedRigidbody.MovePosition(grabbablePosition);
                grabbedRigidbody.MoveRotation(grabbableRotation);
            }
        }

        protected void GrabEnd()
        {
            if (_grabbedObj != null)
            {
                OVRPose localPose = new OVRPose { position = OVRInput.GetLocalControllerPosition(m_controller), orientation = OVRInput.GetLocalControllerRotation(m_controller) };
                OVRPose offsetPose = new OVRPose { position = _anchorOffsetPosition, orientation = _anchorOffsetRotation };
                localPose = localPose * offsetPose;

                OVRPose trackingSpace = transform.ToOVRPose() * localPose.Inverse();
                Vector3 linearVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerVelocity(m_controller);
                Vector3 angularVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerAngularVelocity(m_controller);

                GrabbableRelease(linearVelocity, angularVelocity);
            }

            GrabVolumeEnable(true);
        }

        protected void GrabbableRelease(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            _grabbedObj.GrabEnd(linearVelocity, angularVelocity);
            _grabbedObj.transform.parent = null;
            OnGrabEnded?.Invoke(_grabbedObj);
            _grabbedObj = null;
        }

        protected virtual void GrabVolumeEnable(bool enabled)
        {
            if (_grabVolumeEnabled == enabled)
            {
                return;
            }

            _grabVolumeEnabled = enabled;
            for (int i = 0; i < _grabVolumes.Length; ++i)
            {
                Collider grabVolume = _grabVolumes[i];
                grabVolume.enabled = _grabVolumeEnabled;
            }

            if (!_grabVolumeEnabled)
            {
                _grabCandidates.Clear();
            }
        }

        protected virtual void OffhandGrabbed(Grabbable grabbable)
        {
            if (_grabbedObj == grabbable)
            {
                GrabbableRelease(Vector3.zero, Vector3.zero);
            }
        }
    }
}