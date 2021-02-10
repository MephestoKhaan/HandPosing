using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.OVRIntegration
{
    public class TipsTriggersOVR : MonoBehaviour
    {
        [SerializeField]
        private OVRSkeleton skeleton;
        [SerializeField]
        private GrabberHybridOVR grabber;

        private List<OVRBoneCapsule> _allCapsules;

        private readonly List<OVRSkeleton.BoneId> TIP_BONES = new List<OVRSkeleton.BoneId>
        {
            OVRSkeleton.BoneId.Hand_Thumb3,
            OVRSkeleton.BoneId.Hand_Index3,
            OVRSkeleton.BoneId.Hand_Middle3,
            OVRSkeleton.BoneId.Hand_Ring3,
            OVRSkeleton.BoneId.Hand_Pinky3
        };

        private void OnEnable()
        {
            grabber.OnGrabStarted += ObjectGrabbed;
            grabber.OnGrabEnded += ObjectReleased;

            grabber.OnIgnoreTriggers += IgnoreTriggers;
        }

        private void OnDisable()
        {
            grabber.OnGrabStarted -= ObjectGrabbed;
            grabber.OnGrabEnded -= ObjectReleased;

            grabber.OnIgnoreTriggers -= IgnoreTriggers;
        }

        private IEnumerator Start()
        {
            while (!skeleton.IsInitialized)
            {
                yield return null;
            }

            _allCapsules = new List<OVRBoneCapsule>(skeleton.Capsules);
            SetUpCapsuleTriggerLogic();
        }

        private void SetUpCapsuleTriggerLogic()
        {
            if (_allCapsules.Count == 0)
            {
                Debug.LogError("Ensure EnableCapsulePhysics is enabled in the OVRSkeleton");
                return;
            }
            foreach (var tipBone in TIP_BONES)
            {
                OVRBoneCapsule capsule = _allCapsules.Find(c => c.BoneIndex == (short)tipBone);

                capsule.CapsuleCollider.isTrigger = true;
                TriggerRelay relay = capsule.CapsuleRigidbody.gameObject.AddComponent<TriggerRelay>();
                relay.Grabber = grabber;
            }
        }


        private void ObjectGrabbed(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }
            foreach (var collider in obj.GetComponentsInChildren<Collider>())
            {
                IgnoreCollisions(collider, true);
            }
        }

        private void ObjectReleased(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }
            foreach (var collider in obj.GetComponentsInChildren<Collider>())
            {
                IgnoreCollisions(collider, false);
            }
        }


        private void IgnoreCollisions(Collider collider, bool ignoreCollision)
        {
            if(_allCapsules == null)
            {
                return;
            }
            foreach(var capsule in _allCapsules)
            {
                if(!capsule.CapsuleCollider.isTrigger)
                {
                    Physics.IgnoreCollision(capsule.CapsuleCollider, collider, ignoreCollision);
                }
            }
        }



        private void IgnoreTriggers(bool ignore)
        {
            if (_allCapsules == null)
            {
                return;
            }
            foreach (var capsule in _allCapsules)
            {
                if (capsule.CapsuleCollider.isTrigger)
                {
                    capsule.CapsuleCollider.enabled = !ignore;
                }
            }
        }



        private class TriggerRelay : MonoBehaviour
        {
            public GrabberHybridOVR Grabber { get; set; }

            private void OnTriggerEnter(Collider other)
            {
                Grabber.OnTriggerEnter(other);
            }

            private void OnTriggerExit(Collider other)
            {
                Grabber.OnTriggerExit(other);
            }
        }
    }
}