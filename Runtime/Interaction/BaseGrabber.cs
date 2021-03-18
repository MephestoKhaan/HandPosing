using System;
using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.Interaction
{
    /// <summary>
    /// Sample implementation for a Grabber (without dependencies) that works with the snapping system.
    /// Inherit from this class to be able to reuse the provided Grabber-Grabbable classes or
    /// completely implement (or adapt!) your own by implementing the much smaller IGrabNotifier.
    /// This grabbed does not only takes care of detecting a grab but also if the user intended to grab and it failed.
    /// </summary>
    public abstract class BaseGrabber : MonoBehaviour, IGrabNotifier
    {
        /// <summary>
        /// Grip point of the hand. 
        /// Used to measure distance to grabbables.
        /// </summary>
        [SerializeField]
        private Transform gripTransform = null;
        /// <summary>
        /// Trigger zones that detect grabbables.
        /// </summary>
        [SerializeField]
        private Collider[] grabVolumes = null;

        /// <summary>
        /// Callbacks indicating when the hand tracking has updated.
        /// Not mandatory.
        /// </summary>
        [SerializeField]
        [Tooltip("Not mandatory callbacks indicating when the hand tracking has updated.")]
        private AnchorsUpdateNotifier updateNotifier;

        /// <summary>
        /// Offset from the grip of the hand to the grabbed object.
        /// </summary>
        private Pose _grabbedObjectOffset;

        /// <summary>
        /// True if the updates are being driven externally by an updateNotifier, instead of the standard Unity lifecycle
        /// </summary>
        private bool _usingUpdateNotifier;
        
        private bool _grabVolumeEnabled = true;
        /// <summary>
        /// Current grab strength
        /// </summary>
        private float _flex;
        /// <summary>
        /// Grabbables in contact with the hand volume
        /// </summary>
        private Dictionary<Grabbable, int> _grabCandidates = new Dictionary<Grabbable, int>();
        /// <summary>
        /// True is the hand is not yet grabbing but in the hysteresis range
        /// </summary>
        private bool _nearGrab = false;


        #region grab fail
        /// <summary>
        /// Last grabbable that stopped beign in contact with the hand
        /// </summary>
        private Grabbable _lastGrabCandidate = null;

        /// <summary>
        /// How long has passed since the last grab candidate stopped touching the hand
        /// </summary>
        private float? _timeSinceWithoutCandidates = null;
        /// <summary>
        /// How long has passed since we started a grab
        /// </summary>
        private float? _timeSinceLastGrab = null;
        /// <summary>
        /// How long has passed since we released
        /// </summary>
        private float? _timeSinceLastRelease = null;
        /// <summary>
        /// How long has passed since we attempted to grab, check GrabAttempt()
        /// </summary>
        private float? _timeSinceGrabAttempt = null;
        /// <summary>
        /// How long has passed since a grab failed. CheckGrabFailed and GrabFailed() 
        /// </summary>
        private float? _timeSinceLastFail = null;

        private const float FAIL_SUSTAIN_TIME = 0.08f;
        private const float GRAB_ATTEMPT_DURATION = 2.0f;
        private const float ACTUAL_GRAB_BUFFER_TIME = 1.5f;
        #endregion

        /// <summary>
        /// Current grabbed object.
        /// </summary>
        public Grabbable GrabbedObject { get; private set; } = null;
        /// <summary>
        /// Callback that indicates that the detection trigger has been enabled/disabled
        /// </summary>
        public Action<bool> OnIgnoreTriggers { get; set; }

        #region IGrabNotifier
        public Action<GameObject> OnGrabStarted { get; set; }
        public Action<GameObject, float> OnGrabAttemp { get; set; }
        public Action<GameObject> OnGrabEnded { get; set; }
        public Action<GameObject> OnGrabAttemptFail { get; set; }

        public abstract Vector2 GrabFlexThreshold { get; }

        public abstract float CurrentFlex();

        public Snappable FindClosestSnappable()
        {
            var closestGrabbable = FindClosestGrabbable();
            return closestGrabbable?.GetComponent<Snappable>();
        }
        #endregion

        /// <summary>
        /// Callback indicating that a grab finished and how long it was held. 
        /// </summary>
        public Action<GameObject, float> OnGrabTimedEnded;
        /// <summary>
        /// Range for detecting that grab failed. Typically narrower than GrabFlexThreshold.
        /// </summary>
        public abstract Vector2 GrabAttemptThreshold { get; }
        /// <summary>
        /// Indicates the minimum value for a grab Typically a bit higher than the minimum GrabFlexThreshold
        /// </summary>
        public abstract float ReleasedFlexThreshold { get; }

        /// <summary>
        /// Relative velocities of the hand for throwing.
        /// </summary>
        /// <param name="to">The point at which to measure the velocity.</param>
        /// <returns>The linear and angular velocity of the hand at the given pose.</returns>
        protected abstract (Vector3, Vector3) HandRelativeVelocity(Pose to);

        #region clearer
        private static HashSet<BaseGrabber> allGrabbers = new HashSet<BaseGrabber>();
        /// <summary>
        /// Unsuscribe all the objects grabbed by all hands.
        /// </summary>
        /// <param name="grabbable"></param>
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
                GrabEnd(false);
            }
            allGrabbers.Remove(this);
        }

        /// <summary>
        /// Release a grabbable from this grabber.
        /// </summary>
        /// <param name="grabbable">The grabbable to be released.</param>
        public void ForceRelease(Grabbable grabbable)
        {
            bool canRelease = (
                (GrabbedObject != null) &&
                (GrabbedObject == grabbable || grabbable == null)
            );
            if (canRelease)
            {
                GrabEnd(false);
            }
        }

        /// <summary>
        /// Unsuscribe an object from the list of touched grabbables.
        /// The object will not be a grabbing candidate until it is touched again.
        /// </summary>
        /// <param name="grabbable">The grabbable to be unsuscribed</param>
        public void ForceUntouch(Grabbable grabbable)
        {
            if (_grabCandidates.ContainsKey(grabbable))
            {
                _grabCandidates.Remove(grabbable);
            }
        }

        protected virtual void Update()
        {
            if (!_usingUpdateNotifier)
            {
                UpdateAnchors();
            }
        }

        /// <summary>
        /// Called when the hands have moved, or in Udpdate if no notifier was provided.
        /// </summary>
        private void UpdateAnchors()
        {
            UpdateGrabStates();
        }

        /// <summary>
        /// Checks the current grab strength and updates the internal state to reflect it
        /// </summary>
        private void UpdateGrabStates()
        {
            float prevFlex = _flex;
            _flex = CurrentFlex();

            if (IsGrabAttemp(_flex))
            {
                _timeSinceGrabAttempt = Time.timeSinceLevelLoad;
            }

            CheckForGrabOrRelease(prevFlex, _flex);
            MoveGrabbedObject(this.transform.position, this.transform.rotation);
        }

        /// <summary>
        /// Checks the current grabbing gesture and tries to grab/release/approach a grabbable.
        /// This key method triggers the callbacks for the snapping system.
        /// </summary>
        /// <param name="prevFlex">Last grabbing gesture strength, normalised.</param>
        /// <param name="currentFlex">Current gragginb gesture strength, normalised.</param>
        protected void CheckForGrabOrRelease(float prevFlex, float currentFlex)
        {
            if (CheckGrabFailed(currentFlex))
            {
                GrabFailed();
            }
            else if (prevFlex < GrabFlexThreshold.y
                 && currentFlex >= GrabFlexThreshold.y)
            {
                _nearGrab = false;
                GrabBegin();
            }
            else if (prevFlex > GrabFlexThreshold.x
                && currentFlex <= GrabFlexThreshold.x)
            {
                GrabEnd(true);
            }

            if (GrabbedObject == null
                && currentFlex > 0)
            {
                _nearGrab = true;
                NearGrab(currentFlex / GrabFlexThreshold.y);
            }
            else if (_nearGrab)
            {
                _nearGrab = false;
                NearGrab(0f);
            }
        }

        /// <summary>
        /// Calculates if a flex value is within the grab-attempt range.
        /// This range can be narrower than the actual grab threshold
        /// </summary>
        /// <param name="flex">Raw grab strength to check</param>
        /// <returns>True if the flex is attempting to grab</returns>
        private bool IsGrabAttemp(float flex)
        {
            Vector2 attemptThresold = GrabAttemptThreshold;
            return (flex > attemptThresold.x
                && flex < attemptThresold.y);
        }

        /// <summary>
        /// To calculate if a grab failed:
        /// - check nothing is grabbed
        /// - the user stopped trying to grab in the last few frames
        /// - there is, or there was, a grabbable candidate nearby recently
        /// - we have not grabbed recently anything
        /// </summary>
        /// <param name="currentFlex">The current raw grab strength</param>
        /// <returns>True if all conditions above are true</returns>
        private bool CheckGrabFailed(float currentFlex)
        {
            if (GrabbedObject != null)
            {
                return false;
            }

            bool justReleasedAfterAttempt = _timeSinceGrabAttempt.HasValue
                && Time.timeSinceLevelLoad - _timeSinceGrabAttempt.Value > FAIL_SUSTAIN_TIME
                && currentFlex <= ReleasedFlexThreshold;

            if (justReleasedAfterAttempt)
            {
                _timeSinceGrabAttempt = null;
                bool areGrabbablesNearby = _grabCandidates.Count > 0
                    || (_timeSinceWithoutCandidates.HasValue
                        && Time.timeSinceLevelLoad - _timeSinceWithoutCandidates.Value < GRAB_ATTEMPT_DURATION);
                bool grabbedRecently = _timeSinceLastRelease.HasValue
                    && Time.timeSinceLevelLoad - _timeSinceLastRelease.Value < ACTUAL_GRAB_BUFFER_TIME;

                return areGrabbablesNearby
                    && !grabbedRecently;
            }
            return false;
        }

        /// <summary>
        /// When a grab attempt fails, set the state accordingly
        /// and trigger the relevant callbacks like OnGrabAttemptFail
        /// </summary>
        protected virtual void GrabFailed()
        {
            bool sentFailedEventRecently = _timeSinceLastFail.HasValue
                && Time.timeSinceLevelLoad - _timeSinceLastFail.Value < GRAB_ATTEMPT_DURATION;

            if (!sentFailedEventRecently)
            {
                Grabbable failedGrabbable = _lastGrabCandidate;
                if (failedGrabbable == null)
                {
                    failedGrabbable = FindClosestGrabbable();
                }
                OnGrabAttemptFail?.Invoke(failedGrabbable?.gameObject);
                _timeSinceLastFail = Time.timeSinceLevelLoad;
            }
        }

        /// <summary>
        /// Triggers how close the grabber is to start grabbing a nearby object, informing the snapping system.
        /// </summary>
        /// <param name="factor">Current normalised value for the grab attemp, 1 indicates a grab.</param>
        protected void NearGrab(float factor)
        {
            if (factor == 0f)
            {
                OnGrabAttemp?.Invoke(null, 0f);
                return;
            }

            Grabbable closestGrabbable = FindClosestGrabbable();
            if (closestGrabbable != null)
            {
                OnGrabAttemp?.Invoke(closestGrabbable.gameObject, factor);
            }
            else
            {
                OnGrabAttemp?.Invoke(null, 0f);
            }
        }


        /// <summary>
        /// Search for a nearby object and grab it.
        /// </summary>
        protected virtual void GrabBegin()
        {
            Grabbable closestGrabbable = FindClosestGrabbable();
            GrabVolumeEnable(false);
            if (closestGrabbable != null)
            {
                _timeSinceLastRelease = Time.timeSinceLevelLoad;
                Grab(closestGrabbable);
            }
        }

        /// <summary>
        /// Attach a given grabbable to the hand, storing the offset to the hand so it can be kept while holding.
        /// </summary>
        /// <param name="closestGrabbable">The object to be grabbed.</param>
        protected virtual void Grab(Grabbable closestGrabbable)
        {
            _timeSinceLastGrab = Time.timeSinceLevelLoad;
            GrabbedObject = closestGrabbable;
            GrabbedObject?.GrabBegin(this);
            OnGrabStarted?.Invoke(GrabbedObject?.gameObject);

            Transform grabedTransform = closestGrabbable.transform;
            _grabbedObjectOffset = new Pose();
            _grabbedObjectOffset.position = Quaternion.Inverse(this.transform.rotation) * (grabedTransform.position - this.transform.position);
            _grabbedObjectOffset.rotation = Quaternion.Inverse(this.transform.rotation) * grabedTransform.rotation;
        }

        /// <summary>
        /// Update the grabbed object position/rotation using the offset recorded when the grab started.
        /// </summary>
        /// <param name="pos">Current position of the grabber.</param>
        /// <param name="rot">Current rotation of the grabber.</param>
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

        /// <summary>
        /// Releases the current grabbed object
        /// </summary>
        /// <param name="canGrab">
        /// If the hand can grab again anything within reach after this release.
        /// Set False if the grab was ended artifially, not by the user actually ungrasping.</param>
        protected virtual void GrabEnd(bool canGrab = true)
        {
            if (GrabbedObject != null)
            {
                Vector3 linearVelocity, angularVelocity;
                (linearVelocity, angularVelocity) = HandRelativeVelocity(_grabbedObjectOffset);
                ReleaseGrabbedObject(linearVelocity, angularVelocity);
            }

            if (canGrab)
            {
                GrabVolumeEnable(true);
            }
        }

        /// <summary>
        /// Throw the current grabbed object.
        /// </summary>
        /// <param name="linearVelocity">Linear velocity of the throw.</param>
        /// <param name="angularVelocity">Angular velocity of the throw.</param>
        protected void ReleaseGrabbedObject(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            _timeSinceLastRelease = Time.timeSinceLevelLoad;
            GrabbedObject.GrabEnd(this, linearVelocity, angularVelocity);
            OnGrabEnded?.Invoke(GrabbedObject?.gameObject);
            OnGrabTimedEnded?.Invoke(GrabbedObject?.gameObject, (_timeSinceLastRelease - _timeSinceLastGrab)??0f);
            GrabbedObject = null;
        }

        /// <summary>
        /// Release an object without throwing it.
        /// </summary>
        /// <param name="grabbable">Object to release</param>
        public virtual void OffhandGrabbed(Grabbable grabbable)
        {
            if (GrabbedObject == grabbable)
            {
                ReleaseGrabbedObject(Vector3.zero, Vector3.zero);
            }
        }

        #region grabbable detection


        protected Grabbable FindClosestGrabbable()
        {
            float closestMagSq = float.MaxValue;
            Grabbable closestGrabbable = null;

            foreach (Grabbable grabbable in _grabCandidates.Keys)
            {
                Collider collider = FindClosestCollider(grabbable, out float distance);
                if (distance < closestMagSq)
                {
                    closestMagSq = distance;
                    closestGrabbable = grabbable;
                }
            }
            return closestGrabbable;
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
            OnIgnoreTriggers?.Invoke(!_grabVolumeEnabled);

            if (!_grabVolumeEnabled)
            {
                _grabCandidates.Clear();
            }
        }

        public void OnTriggerEnter(Collider otherCollider)
        {
            Grabbable grabbable = otherCollider.GetComponent<Grabbable>() ?? otherCollider.GetComponentInParent<Grabbable>();
            if (grabbable == null)
            {
                return;
            }

            _grabCandidates.TryGetValue(grabbable, out int refCount);
            _grabCandidates[grabbable] = refCount + 1;

            _timeSinceWithoutCandidates = null;
            _lastGrabCandidate = null;
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

                if (_grabCandidates.Count == 0)
                {
                    _lastGrabCandidate = grabbable;
                    _timeSinceWithoutCandidates = Time.timeSinceLevelLoad;
                }
            }
        }
        #endregion
    }
}