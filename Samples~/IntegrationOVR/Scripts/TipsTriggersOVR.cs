using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HandPosing.OVRIntegration
{
    /// <summary>
    /// Component that prepares the OVRHand to have detection capsules in the tips of the fingers.
    /// This is not mandatory but can be interesting to add when using the Pinch grab to maximise accuracy.
    /// </summary>
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

        /// <summary>
        /// The Start method kickstart the initialisation of the capsules, but it needs to wait
        /// for OVRSkeleton
        /// </summary>
        /// <returns>Wait operation</returns>
        private IEnumerator Start()
        {
            while (!_skeleton.IsInitialized)
            {
                yield return null;
            }
            _allCapsules = new List<OVRBoneCapsule>(_skeleton.Capsules);
            SetUpCapsuleTriggerLogic();
        }

        /// <summary>
        /// Prepares the capsules received from the OVRSkeleton capsules for triggering.
        /// It will select the selected tips as triggers instead of collisions, 
        /// if disableRest is enabled it will disable all collisions for the rest of capsules.
        /// </summary>
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


        /// <summary>
        /// When grabbing an object, since it can be physically grabbed with a joint. Disable the collisions
        /// of the hand colliders with the object.
        /// Not this is for colliders and not the tips-triggers.
        /// </summary>
        /// <param name="obj">grabbed object</param>
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

        /// <summary>
        /// As oposed to ObjectGrabbed, when releasing re-enable the collisions wit the object.
        /// Not this is for colliders and not the tips-triggers.
        /// </summary>
        /// <param name="obj">The released object</param>
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

        /// <summary>
        /// When the grabber disables its own detection triggers, this disables also the ones on the skeleton.
        /// </summary>
        /// <param name="ignore">True to disable the detection triggers, false otherwise</param>
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

        /// <summary>
        /// Internal component to be attached to sub-triggers so we can relay notifications to the grabber.
        /// </summary>
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