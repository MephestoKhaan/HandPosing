// CONFIDENTIAL
// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using System;
using UnityEngine;

namespace GrabEngine {
  /// <summary>
  /// Stock flex detector used in snap-to-pose.
  /// </summary>
  public class PinchTriggerFlex : FlexInterface {
    private const float ALMOST_PINCH_LOWER_PERCENT = 1.2f;
    private const float ALMOST_PINCH_UPPER_PERCENT = 0.75f;

    public FlexFactory.FlexType InterfaceFlexType {
      get {
        return FlexFactory.FlexType.PinchTriggerFlex;
      }
    }

    public GrabState CurrentGrabState { get; private set; }

    public float CurrentGrabStrength { get; private set; }

    public bool VisualIndicatorEnabled { get; set; }

    public bool DoFingerTipsGrab {
      get {
        return true;
      }
    }

    private OVRHand _flexHand;
    private OVRInput.Controller _controller;

    private Vector2 _thresholdHand;
    private Vector2 _thresholdController;

    private float _initialFailTimePinch = -1.0f;
    private bool _pinchFailTriggeredSinceTimerStart = false;

    public void Enable() {
      CurrentGrabState = GrabState.None;
    }

    public void Disable() {
    }

    public void Initialize(OVRHand ovrHand, OVRInput.Controller controller,
      OVRSkeleton handSkeleton, Transform handAnchor) {
      _flexHand = ovrHand;
      _controller = controller;

      var thresholdsInstance = PinchTriggerFlexThresholds.Instance;
      bool isLeftHanded = controller == OVRInput.Controller.LTouch;
      _thresholdHand = isLeftHanded ?
        thresholdsInstance.GrabThresoldHandLeft :
        thresholdsInstance.GrabThresoldHandRight;
      _thresholdController = isLeftHanded ?
        thresholdsInstance.GrabThresoldControllerLeft :
        thresholdsInstance.GrabThresoldControllerRight;
    }

    public void Update(Transform transform) {
      var grabThreshold = _thresholdHand;

      if (_flexHand && _flexHand.IsTracked) {
        CurrentGrabStrength =
          Math.Max(_flexHand.GetFingerPinchStrength(OVRHand.HandFinger.Index),
             _flexHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle));
      } else {
        grabThreshold = _thresholdController;
        CurrentGrabStrength = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger,
          _controller);
      }

      bool grabPassesStartThreshold = CurrentGrabStrength >= grabThreshold.y;
      bool grabFailsEndThreshold = CurrentGrabStrength <= grabThreshold.x;
      bool grabFailed = FlexHelperMethods.GrabFailedJustNow(
        !grabPassesStartThreshold &&
        CurrentGrabStrength >= grabThreshold.x * ALMOST_PINCH_LOWER_PERCENT &&
        CurrentGrabStrength <= grabThreshold.y * ALMOST_PINCH_UPPER_PERCENT,
        grabFailsEndThreshold,
        ref _initialFailTimePinch, ref _pinchFailTriggeredSinceTimerStart);
      CurrentGrabState = FlexHelperMethods.GetNewGrabState(CurrentGrabState,
        grabFailed, grabPassesStartThreshold, grabFailsEndThreshold);
    }
  }
}
