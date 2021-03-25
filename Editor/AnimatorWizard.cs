using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace HandPosing.Editor
{
    public class AnimatorWizard : ScriptableWizard
    {

        [SerializeField]
        private string folder = "GeneratedAnimations";
        [SerializeField]
        private string controllerName = "HandController";

        [SerializeField]
        private HandClips handClips;

        [System.Serializable]
        public struct HandClips
        {
            public AnimationClip handFist;
            public AnimationClip hand3qtrFist;
            public AnimationClip handMidFist;
            public AnimationClip handPinch;
            public AnimationClip handCap;

            public AnimationClip thumbCap;
            public AnimationClip thumbUp;

            public AnimationClip indexCap;
            public AnimationClip indexPoint;
        }


        [InspectorButton("GenerateAnimator")]
        public string generateAnimator;


        [MenuItem("HandPosing/Hand Animator Generator")]
        private static void CreateWizard()
        {
            AnimatorWizard wizard = ScriptableWizard.DisplayWizard<AnimatorWizard>("Hand Animator Generator", "Close");
        }

        private void OnWizardCreate() { }


        private void GenerateAnimator()
        {
            string targetFolder = $"Assets/{folder}/";
            CreateFolder(targetFolder);
            string path = $"{targetFolder}/{controllerName}.controller";
            CreateAnimator(path, handClips);
        }

        private const string FLEX_PARAM = "Flex";
        private const string POSE_PARAM = "Pose";
        private const string PINCH_PARAM = "Pinch";

        private AnimatorController CreateAnimator(string path, HandClips clips)
        {
            AnimatorController animator = AnimatorController.CreateAnimatorControllerAtPath(path);

            animator.AddParameter(FLEX_PARAM, AnimatorControllerParameterType.Float);
            animator.AddParameter(POSE_PARAM, AnimatorControllerParameterType.Float);
            animator.AddParameter(PINCH_PARAM, AnimatorControllerParameterType.Float);

            animator.RemoveLayer(0);
            CreateLayer(animator, "Flex Layer");
            CreateLayer(animator, "Thumb Layer");
            CreateLayer(animator, "Point Layer");

            CreateFlexStates(animator, 0, clips);
            CreateThumbUpStates(animator, 1, clips);
            CreatePointStates(animator, 2, clips);

            return animator;
        }

        private AnimatorControllerLayer CreateLayer(AnimatorController animator, string layerName)
        {
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.name = layerName;
            AnimatorStateMachine stateMachine = new AnimatorStateMachine();
            stateMachine.name = layer.name;
            AssetDatabase.AddObjectToAsset(stateMachine, animator);
            stateMachine.hideFlags = HideFlags.HideInHierarchy;
            layer.stateMachine = stateMachine;
            animator.AddLayer(layer);
            return layer;
        }


        private void CreateFlexStates(AnimatorController animator, int layerIndex, HandClips clips)
        {
            BlendTree blendTree;
            AnimatorState flexState = animator.CreateBlendTreeInController("Flex", out blendTree, layerIndex);
            blendTree.blendType = BlendTreeType.FreeformCartesian2D;
            blendTree.blendParameter = FLEX_PARAM;
            blendTree.blendParameterY = PINCH_PARAM;
            blendTree.AddChild(clips.handCap, new Vector2(0f, 0f));
            blendTree.AddChild(clips.handPinch, new Vector2(0f, 0.835f));
            blendTree.AddChild(clips.handPinch, new Vector2(0f, 1f));
            blendTree.AddChild(clips.handMidFist, new Vector2(0.5f, 0f));
            blendTree.AddChild(clips.handMidFist, new Vector2(0.5f, 1f));
            blendTree.AddChild(clips.hand3qtrFist, new Vector2(0.835f, 0f));
            blendTree.AddChild(clips.hand3qtrFist, new Vector2(0.835f, 1f));
            blendTree.AddChild(clips.handFist, new Vector2(1f, 0f));
            blendTree.AddChild(clips.handFist, new Vector2(1f, 1f));
            animator.layers[layerIndex].stateMachine.defaultState = flexState;
        }


        private void CreateThumbUpStates(AnimatorController animator, int layerIndex, HandClips clips)
        {
            BlendTree blendTree;
            AnimatorState flexState = animator.CreateBlendTreeInController("Thumbs Up", out blendTree, layerIndex);
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = FLEX_PARAM;
            blendTree.AddChild(clips.thumbCap, 0f);
            blendTree.AddChild(clips.thumbUp, 1f);
            blendTree.useAutomaticThresholds = true;
            animator.layers[layerIndex].stateMachine.defaultState = flexState;
        }


        private void CreatePointStates(AnimatorController animator, int layerIndex, HandClips clips)
        {
            BlendTree blendTree;
            AnimatorState flexState = animator.CreateBlendTreeInController("Point", out blendTree, layerIndex);
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = FLEX_PARAM;
            blendTree.AddChild(clips.indexCap, 0f);
            blendTree.AddChild(clips.indexPoint, 1f);
            blendTree.useAutomaticThresholds = true;
            animator.layers[layerIndex].stateMachine.defaultState = flexState;
        }

        private void CreateFolder(string targetFolder)
        {
            if (!System.IO.Directory.Exists(targetFolder))
            {
                System.IO.Directory.CreateDirectory(targetFolder);
            }
        }
    }
}