using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    /// <summary>
    /// Represents interface for "flex" or grab strength. Serves as template
    /// for all grabbing methods.
    /// </summary>
    public interface FlexInterface
    {
        /// <summary>
        /// True if the current Flex Interface can be used, for example the Hand is being tracked in the case of a hand-tracking flex.
        /// This is important so we can fallback to other FlexInterfaces in case one of them is not valid.
        /// </summary>
        bool IsValid { get; }
        /// <summary>
        /// Return normalized grab strength.
        /// </summary>
        /// <returns>Grab strength, restricted to 0.0-1.0. Null if no strength is available</returns>
        float? GrabStrength { get; }
        /// <summary>
        /// Return strenght values to start (Y) or stop (X) grabbing.
        /// </summary>
        Vector2 GrabThreshold { get; }
        /// <summary>
        /// Return strenght values to indicate a grabbing attempt, can be narrower than GrabThresold.
        /// </summary>
        Vector2 GrabAttemptThreshold { get; }
        /// <summary>
        /// Indicates the minimum value for a grab Typically a bit higher than the minimum GrabThreshold
        /// </summary>
        float AlmostGrabRelease { get; }
    }
}
