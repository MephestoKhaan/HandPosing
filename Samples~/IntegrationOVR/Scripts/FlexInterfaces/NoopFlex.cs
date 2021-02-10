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

        public float GrabStrength => 0f;
        public Vector2 GrabThresold => Vector2.one;
        public Vector2 FailGrabThresold => Vector2.one;
    }
}
