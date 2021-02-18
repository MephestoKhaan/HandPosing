using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HandPosing.OVRIntegration
{
    [RequireComponent(typeof(OVRSkeleton))]
    public class TipsTriggersOVR : MonoBehaviour
    {
        [SerializeField]
        private GrabberHybridOVR grabber;


        [SerializeField]
        private bool disableRest;

        private OVRSkeleton _skeleton;
        private List<OVRBoneCapsule> _allCapsules;

        private readonly List<OVRSkeleton.BoneId> TIP_BONES = new List<OVRSkeleton.BoneId>
        {
            OVRSkeleton.BoneId.Hand_Thumb3,
            OVRSkeleton.BoneId.Hand_Index3,
            OVRSkeleton.BoneId.Hand_Middle3,
            OVRSkeleton.BoneId.Hand_Ring3,
            OVRSkeleton.BoneId.Hand_Pinky3
        };

        private void Awake()
        {
            _skeleton = this.GetComponent<OVRSkeleton>();
        }

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
            while (!_skeleton.IsInitialized)
            {
                yield return null;
            }

            _allCapsules = new List<OVRBoneCapsule>(_skeleton.Capsules);
            SetUpCapsuleTriggerLogic();
        }

        private void SetUpCapsuleTriggerLogic()
        {
            if (_allCapsules.Count == 0)
            {
                Debug.LogError("Ensure EnableCapsulePhysics is enabled in the OVRSkeleton");
                return;
            }

            List<OVRBoneCapsule> _tipCapsules = new List<OVRBoneCapsule>();
            foreach (var tipBone in TIP_BONES)
            {
                OVRBoneCapsule capsule = _allCapsules.Find(c => c.BoneIndex == (short)tipBone);
                _tipCapsules.Add(capsule);

                capsule.CapsuleCollider.isTrigger = true;
                TriggerRelay relay = capsule.CapsuleRigidbody.gameObject.AddComponent<TriggerRelay>();
                relay.Grabber = grabber;
            }

            if(disableRest)
            {
                var midCapsules = _allCapsules.Except(_tipCapsules);
                foreach (var midCapsule in midCapsules)
                {
                    midCapsule.CapsuleCollider.enabled = false;
                }
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