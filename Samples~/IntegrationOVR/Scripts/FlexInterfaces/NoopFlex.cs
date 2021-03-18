using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    /// <summary>
    /// No op flex detector used in snap-to-pose, used to turn off grab
    /// detection so that poses can be recorded.
    /// </summary>
    public class NoopFlex : FlexInterface
    {
        public FlexType InterfaceFlexType => FlexType.Noop;
        public bool IsValid => true;
        public float? GrabStrength => null;
        public Vector2 GrabThreshold => Vector2.one;
        public Vector2 FailGrabThreshold => Vector2.one;
        public float AlmostGrabRelease => -1f;
    }
}
