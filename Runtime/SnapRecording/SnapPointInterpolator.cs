using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    /// <summary>
    /// This SnapPoint is actually a Composition of several basic SnapPoints that can be 
    /// interpolated in between according to the hand scale.
    /// 
    /// If enabling hand scaling, create several normal SnapPoints at different scales for
    /// the same pose, and then group them together with this class before assigning it to
    /// the Snappable, so the system can interpolate between all of them depending on the 
    /// size of the user's hand.
    /// </summary>
    public class SnapPointInterpolator : BaseSnapPoint
    {
        [Space]
        /// <summary>
        /// The list of basic Snap Points to interpolate to based on their scales.
        /// </summary>
        [SerializeField]
        private List<SnapPoint> points;

        public override Vector3 NearestInSurface(Vector3 worldPoint, float scale = 1f)
        {
            SnapPoint under = null;
            SnapPoint over = null;
            float t = 0f;
            (under, over, t) = FindRange(scale);

            if(t >= 0f)
            {
                return Vector3.Lerp(
                    under.NearestInSurface(worldPoint),
                    over.NearestInSurface(worldPoint),
                    t);
            }
            else
            {
                Debug.LogError("Invalid range");
                return this.transform.position;
            }
        }

        public override ScoredHandPose CalculateBestPose(HandPose userPose, float? scoreWeight = null, SnapDirection direction = SnapDirection.Any, float scale = 1f)
        {
            SnapPoint under = null;
            SnapPoint over = null;
            float t = 0f;
            (under, over, t) = FindRange(scale);

            ScoredHandPose? result = null;
            if (t >= 0f)
            {
                result =  ScoredHandPose.Lerp(
                    under.CalculateBestPose(userPose, scoreWeight, direction),
                    over.CalculateBestPose(userPose, scoreWeight, direction),
                    t);
            }

            if(!result.HasValue)
            {
                Debug.LogError("Invalid range");
                return points[0].CalculateBestPose(userPose, scoreWeight, direction);
            }
            return result.Value;
        }

        /// <summary>
        /// Finds the two nearest (upper and lower) snap points to interpolate from given a scale.
        /// </summary>
        /// <param name="scale">The user scale</param>
        /// <returns>The SnapPoints inmediately under and over the scale, and the interpolation factor between them.</returns>
        private (SnapPoint, SnapPoint, float) FindRange(float scale)
        {
            SnapPoint under = null;
            SnapPoint over = null;

            foreach (var point in points)
            {
                if (point.Scale <= scale
                    && point.Scale > (under ? under.Scale : float.NegativeInfinity))
                {
                    under = point;
                }

                if (point.Scale >= scale
                    && point.Scale < (over ? over.Scale : float.PositiveInfinity))
                {
                    over = point;
                }
            }

            if (under == null
                && over == null)
            {
                return (null, null, -1f);
            }
            else if (under == null
                && over != null)
            {
                return (over, over, 1f);
            }
            else if (under != null
                && over == null)
            {
                return (under, under, 0f);
            }

            float t = (scale - under.Scale) / (over.Scale - under.Scale);
            return (under, over, t);
        }


        public override void DestroyImmediate()
        {
            foreach(var point in points)
            {
                point.DestroyImmediate();
            }
            DestroyImmediate(this.gameObject);
        }
    }
}