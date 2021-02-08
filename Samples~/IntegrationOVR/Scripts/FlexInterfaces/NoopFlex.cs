// CONFIDENTIAL
// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using UnityEngine;

namespace GrabEngine {
  /// <summary>
  /// No op flex detector used in snap-to-pose, used to turn off grab
  /// detection so that poses can be recorded.
  /// </summary>
  public class NoopFlex : FlexInterface {
    public FlexFactory.FlexType InterfaceFlexType {
      get {
        return FlexFactory.FlexType.Noop;
      }
    }

    public GrabState CurrentGrabState { get; private set; }

    public float CurrentGrabStrength { get; private set; }

    public bool VisualIndicatorEnabled { get; set; }

    public bool DoFingerTipsGrab {
      get {
        return false;
      }
    }

    public void Enable() {
      CurrentGrabState = GrabState.None;
    }

    public void Disable() {
    }

    public void Initialize(OVRHand ovrHand, OVRInput.Controller controller,
      OVRSkeleton handSkeleton, Transform handAnchor) {
      CurrentGrabStrength = 0.0f;
    }

    public void Update(Transform transform) {
      // nothing to see here
    }
  }
}
