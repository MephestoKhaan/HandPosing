using System;
using UnityEngine;

namespace HandPosing.Interaction
{
    /// <summary>
    /// Layer for communicating between the Grabber and the Snapper code.
    /// You must implement this interface if you want your grabber to be able to snap to objects.
    /// </summary>
    public interface IGrabNotifier 
    {
        /// <summary>
        /// Event triggered when a Grab is started at a GameObject.
        /// </summary>
        /// <param name="GameObject">The grabbed object.</param>
        Action<GameObject> OnGrabStarted { get; set; }
        /// <summary>
        /// Event triggered when a Grab is attempted at a GameObject.
        /// </summary>
        /// <param name="GameObject">The object about to be grabbed.</param>
        /// <param name="float">The normalised value indicating how close to grab the user is. e.g. if the user grabs with a pinch 0.5 would indicate the pinch gesture is half-way in.</param>
        Action<GameObject, float> OnGrabAttemp { get; set; }
        /// <summary>
        /// Event triggered when a grabbed GameObject is released.
        /// </summary>
        /// <param name="GameObject">The ungrabbed object.</param>
        Action<GameObject> OnGrabEnded { get; set; }

        /// <summary>
        /// Min - Max value for the grab thresold. X indicates the release point, Y the grab point. Typically [0.33,0.9] or even [0,1]
        /// </summary>
        Vector2 GrabFlexThresold { get; }
        /// <summary>
        /// Current normalised value of the grabbing gesture. 1 for a fully realised gesture, 0 for no gesture detected.
        /// </summary>
        /// <returns>Normalised value for the grab gesture detection.</returns>
        float CurrentFlex();

        /// <summary>
        /// The snappable with the best score that can be reached at the current pose.
        /// Typically all grabbable objects will also have snappables, and the Grabber would already implement a metho to find the nearest grabbable,this could be very similar.
        /// </summary>
        /// <returns>If found, the best object that the hand can snap to.</returns>
        Snappable FindClosestSnappable();
    }
}
