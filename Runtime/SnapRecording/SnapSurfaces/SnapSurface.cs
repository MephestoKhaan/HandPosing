using UnityEngine;

namespace HandPosing.SnapSurfaces
{
    /// <summary>
    /// A serializable data-only version of the Surface data so it can be storef if it was
    /// generated during Play-Mode.
    /// 
    /// Not to be edited directly! Load the SnapPoint and edit it with the tools provided.
    /// </summary>
    [System.Serializable]
    public abstract class SnapSurfaceData : System.ICloneable
    {
        public abstract System.Type SurfaceType { get; }
        public abstract object Clone();
        public abstract SnapSurfaceData Mirror();
    }

    /// <summary>
    /// Especifies a surface around a Snappable in which a hand can snap to.
    /// SnapSurfaces are an optional property of SnapPoints and they actually define all
    /// the valid points for the Grip of the hand to snap to the object with the same HandPose.
    /// 
    /// Snap Surfaces typically come with Inspector and Handles to modify their properties, this is the
    /// recommended way to edit the values as editting the numbers directly can be very error-prone.
    /// </summary>
    [System.Serializable]
    public abstract class SnapSurface : MonoBehaviour
    {
        /// <summary>
        /// Getter for the data-only version of this surface. Used so it can be stored when created
        /// at Play-Mode.
        /// </summary>
        public virtual SnapSurfaceData Data { get => null; set { } }

        /// <summary>
        /// Valid point at which the hand can snap, typically the SnapPoint position itself.
        /// </summary>
        protected Transform GripPoint
        {
            get
            {
                return this.transform;
            }
        }

        /// <summary>
        /// Object to which the surface refers to.
        /// </summary>
        public Transform relativeTo;

        /// <summary>
        /// Method for mirroring a Pose around the surface.
        /// Different surfaces will prefer mirroring along different axis.
        /// </summary>
        /// <param name="pose">The Pose to be mirrored.</param>
        /// <returns>A new pose mirrored at this surface.</returns>
        public virtual Pose MirrorPose(Pose pose)
        {
            return pose;
        }

        /// <summary>
        /// STUB: Inverts a hand pose to an upside-down position.
        /// </summary>
        /// <param name="pose">The hand pose to be inverted</param>
        /// <returns>An upside-down version of the given pose.</returns>
        public abstract HandPose InvertedPose(HandPose pose);
        /// <summary>
        /// The nearest position at the surface from a given position.
        /// </summary>
        /// <param name="targetPosition">The position to measure from at world coordinates.</param>
        /// <returns>A valid position at the surface in world coordinates.</returns>
        public abstract Vector3 NearestPointInSurface(Vector3 targetPosition);
        /// <summary>
        /// Calculates a valid pose at the surface with the most similar rotation possible to the user's hand
        /// </summary>
        /// <param name="userPose">The user's hand pose</param>
        /// <param name="snapPose">The pose for the snap point</param>
        /// <returns>A valid pose at the surface</returns>
        public abstract Pose MinimalRotationPoseAtSurface(Pose userPose, Pose snapPose);
        /// <summary>
        /// Calculates a valid pose at the surface with the most similar position possible to the user's hand
        /// </summary>
        /// <param name="userPose">The user's hand pose</param>
        /// <param name="snapPose">The pose for the snap point</param>
        /// <returns>A valid pose at the surface</returns>
        public abstract Pose MinimalTranslationPoseAtSurface(Pose userPose, Pose snapPose);
    }
}
