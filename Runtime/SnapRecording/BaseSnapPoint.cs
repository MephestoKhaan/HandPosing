using HandPosing.SnapSurfaces;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    /// <summary>
    /// A SnapPoint indicates the properties about how a hand can snap to an object.
    /// The most important is the position/rotation and finger rotations for the hand, 
    /// but it can also contain extra information like a valid holding surface (instead of just
    /// a single point) or a visual representation (using a hand-ghost)
    /// </summary>
    [System.Serializable]
    public abstract class BaseSnapPoint : MonoBehaviour
    {
        /// <summary>
        /// The transform of the object this snap point refers to.
        /// Typically the parent.
        /// </summary>
        [SerializeField]
        protected Transform relativeTo;
        /// <summary>
        /// If true, the hand can be overriden to reach the object, and once it is grabbed
        /// it will move back (now holding it) to the user-tracking position.
        /// </summary>
        [SerializeField]
        [Tooltip("If true, the hand can be overriden to reach the object, and once it is grabbed it will move back")]
        protected bool snapsBack;
        /// <summary>
        /// Indicates how firmly the grab strength must be so the hand can slide. 
        /// Normalised, a value of 1 will always slide, a value of 0.5 will start sliding only 
        /// when the grab is half-released.
        /// 
        /// It is recommended to use this only with grabs based on physics Joints.
        /// </summary>
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Indicates how firmly the grab strength must be so the hand can slide (0 never slides, 1 always). Use with Physics grabs.")]
        protected float slideThresold = 0f;

        /// <summary>
        /// General getter for the transform of the object this snap point refers to
        /// </summary>
        public Transform RelativeTo { get => relativeTo; }
        /// <summary>
        /// General getter indicating if the hand can reach for the object and then move back to the user-tracked position.
        /// </summary>
        public bool SnapsBack { get => snapsBack; }
        /// <summary>
        /// General getter indicatig how firmly to held the object so the hand does not slide throught the surface.
        /// </summary>
        public float SlideThresold { get => slideThresold; }

        /// <summary>
        /// Find the best valid hand-pose at this snap point.
        /// Remember that a snap point can actually have a whole surface the user can snap to. 
        /// In some cases it can also have different hand scales with their surfaces, it will interpolate
        /// between the best available matches.
        /// </summary>
        /// <param name="userPose">Hand pose to compare to the snap point.</param>
        /// <param name="scoreWeight">How much to score the position or the rotation difference.</param>
        /// <param name="direction">Consider only poses at the surface using the provided direction.</param>
        /// <param name="scale">The desired scale of the hand to compare.</param>
        /// <returns>The most similar valid HandPose at this SnapPoint</returns>
        public abstract ScoredHandPose CalculateBestPose(HandPose userPose, float? scoreWeight = null, SnapDirection direction = SnapDirection.Any, float scale = 1f);
        
        /// <summary>
        /// Find the closes point at the snap surface.
        /// If there is no surface, the SnapPoint position itself is returned.
        /// If multiple scales are available, it will interpolate between the closest matches.
        /// </summary>
        /// <param name="worldPoint">Point to measure distane to the surface</param>
        /// <param name="scale">The desired scale of the hand.</param>
        /// <returns>The nearest point in World coordinates</returns>
        public abstract Vector3 NearestInSurface(Vector3 worldPoint, float scale = 1f);

        /// <summary>
        /// Destroys this snap point, to be called from the Inspector only.
        /// </summary>
        public abstract void DestroyImmediate();
    }
}