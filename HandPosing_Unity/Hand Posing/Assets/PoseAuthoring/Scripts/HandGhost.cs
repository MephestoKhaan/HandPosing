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

        public HandPose PoseToObject
        {
            get;
            private set;
        }

        private HandPuppet puppet;
        private int colorIndex;

        private void Awake()
        {
            puppet = this.GetComponent<HandPuppet>();
            colorIndex = Shader.PropertyToID(colorProperty);
            Highlight(false);
        }
        
        public void SetPose(HandPose pose, Transform relativeTo)
        {
            puppet.SetRecordedPose(pose, relativeTo);
            PoseToObject = pose;
        }

        public void Highlight(bool highlight)
        {
            Color color = highlight ? highlightedColor : defaultColor;
            handRenderer.material.SetColor(colorIndex, color);
        }
    }
}
