using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    public enum FlexType
    {
        Noop = 0, 
        Controller,
        PinchTriggerFlex,
        SphereGrab
    }

    /// <summary>
    /// Represents interface for "flex" or grab strength. Serves as template
    /// for all grabbing methods.
    /// </summary>
    public interface FlexInterface
    {
        FlexType InterfaceFlexType { get; }

        bool IsValid { get; }

        /// <summary>
        /// Return normalized grab strength.
        /// </summary>
        /// <returns>Grab strength, restricted to 0.0-1.0. Null if no strength is available</returns>
        float? GrabStrength { get; }

        /// <summary>
        /// Return strenght values to start (Y) or stop (X) grabbing.
        /// </summary>
        Vector2 GrabThresold { get; }
        /// <summary>
        /// Return strenght values to indicate a grabbing attempt, can be narrower than GrabThresold.
        /// </summary>
        Vector2 FailGrabThresold { get; }

        float AlmostGrabRelease { get; }
    }
}
