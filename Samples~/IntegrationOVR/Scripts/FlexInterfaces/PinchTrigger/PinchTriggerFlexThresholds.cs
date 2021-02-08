// CONFIDENTIAL
// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using UnityEngine;

namespace GrabEngine {
  public class PinchTriggerFlexThresholds : MonoBehaviour {
    [SerializeField]
    [Tooltip("Grab threshold, left hand controller")]
    private Vector2 _grabThresoldControllerLeft = new Vector2(0.35f, 0.55f);
    [SerializeField]
    [Tooltip("Grab threshold, left hand pinch")]
    private Vector2 _grabThresoldHandLeft = new Vector2(0.35f, 0.95f);
    [SerializeField]
    [Tooltip("Grab threshold, right hand controller")]
    private Vector2 _grabThresoldControllerRight = new Vector2(0.35f, 0.55f);
    [SerializeField]
    [Tooltip("Grab threshold, right hand pinch")]
    private Vector2 _grabThresoldHandRight = new Vector2(0.35f, 0.95f);

    private static PinchTriggerFlexThresholds _pinchTriggerFlexThresholds = null;
    public static PinchTriggerFlexThresholds Instance {
      get {
        if (_pinchTriggerFlexThresholds == null) {
          PinchTriggerFlexThresholds[] allThresholds =
            FindObjectsOfType<PinchTriggerFlexThresholds>();
          if (allThresholds.Length == 0) {
            Debug.LogError("Can't find instance of PinchTriggerFlexThresholds in scene!");
            return null;
          }

          if (allThresholds.Length > 1) {
            Debug.LogWarning("More than one pinch trigger threshold object detected. Using first.");
          }
          _pinchTriggerFlexThresholds = allThresholds[0];
        }
        return _pinchTriggerFlexThresholds;
      }
    }

    public Vector2 GrabThresoldControllerLeft {
      get {
        return _grabThresoldControllerLeft;
      }
    }

    public Vector2 GrabThresoldHandLeft {
      get {
        return _grabThresoldHandLeft;
      }
    }

    public Vector2 GrabThresoldControllerRight {
      get {
        return _grabThresoldControllerRight;
      }
    }

    public Vector2 GrabThresoldHandRight {
      get {
        return _grabThresoldHandRight;
      }
    }
  }
}
