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
    }
}
