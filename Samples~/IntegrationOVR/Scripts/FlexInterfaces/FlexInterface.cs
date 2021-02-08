// CONFIDENTIAL
// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using UnityEngine;

namespace GrabEngine {
  public enum GrabState { None = 0, Failed, Begin, Sustain, End }
  /// <summary>
  /// Represents interface for "flex" or grab strength. Serves as template
  /// for all grabbing methods.
  /// </summary>
  public interface FlexInterface {
    FlexFactory.FlexType InterfaceFlexType { get; }

    GrabState CurrentGrabState { get; }

    /// <summary>
    /// Return normalized grab strength.
    /// </summary>
    /// <returns>Grab strength, restricted to 0.0-1.0.</returns>
    float CurrentGrabStrength { get; }

    bool VisualIndicatorEnabled { get; set; }

    bool DoFingerTipsGrab { get; }

    // TODO: this initialize seems a bit overcomplicated now! simplify
    void Initialize(OVRHand ovrHand, OVRInput.Controller controller,
      OVRSkeleton handSkeleton, Transform handAnchor);

    void Enable();

    void Disable();

    void Update(Transform transform);
  }

  public class FlexHelperMethods {
    private const float FAIL_SUSTAIN_TIME = 0.08f;

    /// <summary>
    /// Tells call if grab failed this frame. When an almost grab happens, start a timer.
    /// Once enough time has passed and our threshold is below an almost grab (i.e. a release),
    /// then fire an event by returning true. Use another boolean to prevent true from
    /// being returned across multiple frames.
    /// </summary>
    /// <param name="flexThresholdAlmostGrab">Whether the grab strength meets the
    /// "almost grab" threshold.</param>
    /// <param name="flexThresholdBelowAlmostGrab">If flesh threshold is below
    /// almost grab.</param>
    /// <param name="initialFailTime">When grab first failed.</param>
    /// <param name="failBooleanAlreadyReturned">If we have already
    /// returned a "grab failed this frame" event or boolean. This is toggled
    /// to true to prevent this event from happening multiple times.</param>
    /// <returns></returns>
    public static bool GrabFailedJustNow(
      bool flexThresholdAlmostGrab, bool flexThresholdBelowAlmostGrab,
      ref float initialFailTime, ref bool failBooleanAlreadyReturned) {
      bool grabFailedThisFrame = false;

      // start timer if almost grab failed
      if (flexThresholdAlmostGrab && initialFailTime < 0.0f) {
        failBooleanAlreadyReturned = false;
        initialFailTime = Time.time;
      }
      else if (flexThresholdBelowAlmostGrab) {
        // if we are coming back from an almost grab,
        // if enough time has passed (assuming timer started),
        // and event hasn't been fired yet:
        // tell caller that a fail just happened this frame
        if (initialFailTime > 0.0f &&
            Time.time > (initialFailTime + FAIL_SUSTAIN_TIME) &&
            !failBooleanAlreadyReturned) {
          grabFailedThisFrame = true;
          failBooleanAlreadyReturned = true;
        }

        initialFailTime = -1.0f;
      }

      return grabFailedThisFrame;
    }

    public static GrabState GetNewGrabState(GrabState CurrentGrabState,
      bool failedGrabConditionsMet, bool startGrabConditionsMet,
      bool endGrabConditionsMet) {
      switch (CurrentGrabState) {
        // none can start a failed or successful grab
        case GrabState.None:
          if (startGrabConditionsMet) {
            CurrentGrabState = GrabState.Begin;
          }
          else if (failedGrabConditionsMet) {
            CurrentGrabState = GrabState.Failed;
          }
          break;
        // if fail, we can only reset back to none state
        case GrabState.Failed:
          CurrentGrabState = GrabState.None;
          break;
        // begin grab can either go into end or sustain
        case GrabState.Begin:
          CurrentGrabState = endGrabConditionsMet ? GrabState.End :
            GrabState.Sustain;
          break;
        // can only end grab after sustaining
        case GrabState.Sustain:
          if (endGrabConditionsMet) {
            CurrentGrabState = GrabState.End;
          }
          break;
        // if grab ended last frame, can go into failed grab,
        // start grab, or default none state
        case GrabState.End:
          if (failedGrabConditionsMet) {
            CurrentGrabState = GrabState.Failed;
          }
          else {
            CurrentGrabState = startGrabConditionsMet ? GrabState.Begin :
              GrabState.None;
          }
          break;
      }

      return CurrentGrabState;
    }
  }
}
