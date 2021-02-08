// CONFIDENTIAL
// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using UnityEngine;

namespace GrabEngine {
  /// <summary>
  /// Used to create a flex implementation on request.
  /// </summary>
  public class FlexFactory : MonoBehaviour {
    public enum FlexType {
      Noop = 0, PinchTriggerFlex
    }

    private static FlexFactory _flexFactory;
    public static FlexFactory Instance {
      get {
        if (_flexFactory == null) {
          _flexFactory = FindObjectOfType<FlexFactory>();
          if (_flexFactory == null) {
            Debug.LogError("Can't find instance of FlexFactory in scene!");
          }
        }
        return _flexFactory;
      }
    }

    /// <summary>
    /// Create instance of grab detector.
    /// </summary>
    /// <param name="flexType">The grab detector or "flex" type.</param>
    /// <param name="ovrHand">The associated hand.</param>
    /// <param name="controller">The associated controller. The OVRHand API does
    /// not expose handedness, so we need the controller too.</param>
    /// <param name="handAnchor">The anchor of the hand, if required.</param>
    /// <returns></returns>
    public FlexInterface CreateFlexInterface(FlexType flexType, OVRHand ovrHand,
      OVRInput.Controller controller, OVRSkeleton handSkeleton, Transform handAnchor) {
      FlexInterface newFlex;

      switch (flexType) {
        case FlexType.PinchTriggerFlex:
          newFlex = new PinchTriggerFlex();
          break;
        default:
          newFlex = new NoopFlex();
          break;
      }

      newFlex.Initialize(ovrHand, controller, handSkeleton, handAnchor);

      return newFlex;
    }
  }
}
