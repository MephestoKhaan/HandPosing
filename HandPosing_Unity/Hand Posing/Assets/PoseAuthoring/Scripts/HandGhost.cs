using UnityEngine;

namespace PoseAuthoring
{
    [RequireComponent(typeof(HandPuppet))]
    public class HandGhost : MonoBehaviour
    {
        [SerializeField]
        private Renderer handRenderer;
        [SerializeField]
        private Color highlightedColor = Color.yellow;
        [SerializeField]
        private Color defaultColor = Color.blue;
        [SerializeField]
        private string colorProperty = "_RimColor";
        [InspectorButton("MakeStaticPose")]
        public string StaticPose;

        [SerializeField]
        public CylinderHandle _cylinder;
        public CylinderHandle Cylinder
        {
            get
            {
                if(_cylinder == null)
                {
                    _cylinder = new CylinderHandle(this.transform, this.puppet.Grip);
                }
                return _cylinder;
            }
        }

        public HandSnapPose PoseToObject
        {
            get;
            private set;
        }

        public Transform RelativeTo
        {
            get;
            private set;
        }

        private HandPuppet puppet;
        private int colorIndex;

        private void Awake()
        {
            colorIndex = Shader.PropertyToID(colorProperty);
            Highlight(false);
        }
        
        public void SetPose(HandSnapPose pose, Transform relativeTo)
        {
            puppet.SetRecordedPose(pose, relativeTo);
            RelativeTo = relativeTo;
            PoseToObject = pose;
        }

        public void Highlight(float amount)
        {
            Color color = Color.Lerp(defaultColor, highlightedColor, amount);
            handRenderer.material.SetColor(colorIndex, color);
        }

        public void Highlight(bool highlight)
        {
            Color color = highlight ? highlightedColor : defaultColor;
            handRenderer.material.SetColor(colorIndex, color);
        }

        public void MakeStaticPose()
        {
            _cylinder?.MakeSinglePoint();
        }

        private void Reset()
        {
            puppet = this.GetComponent<HandPuppet>();
            _cylinder = new CylinderHandle(this.transform, this.puppet.Grip);
            handRenderer = this.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        public float Score(HandSnapPose desiredPose, Transform relativeTo, out (Vector3, Quaternion) surfacePose, float maxDistance = 0.1f)
        {
            HandSnapPose snapPose = this.PoseToObject;
            if (snapPose.isRightHand != desiredPose.isRightHand)
            {
                surfacePose = (Vector3.zero, Quaternion.identity);
                return 0f;
            }

            Vector3 globalPosDesired = relativeTo.TransformPoint(desiredPose.relativeGripPos);
            Vector3 surfacePoint = Cylinder.NearestPointInSurface(globalPosDesired);

            Quaternion globalRotDesired = relativeTo.rotation * desiredPose.relativeGripRot;
            Quaternion surfaceRotation = Cylinder.TransformRotation(desiredPose, relativeTo, surfacePoint);

            float forwardDifference = Vector3.Dot(surfaceRotation * Vector3.forward, globalRotDesired * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(surfaceRotation * Vector3.up, globalRotDesired * Vector3.up) * 0.5f + 0.5f;

            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(surfacePoint, globalPosDesired) / maxDistance);

            surfacePose = (surfacePoint, surfaceRotation);

            return forwardDifference * upDifference * positionDifference;
        }
    }
}
