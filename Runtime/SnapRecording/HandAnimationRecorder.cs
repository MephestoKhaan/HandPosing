using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    public class HandAnimationRecorder : MonoBehaviour
    {

        [SerializeField]
        private HandPuppet puppet;
        [SerializeField]
        private Transform root;

        [SerializeField]
        private string folder = "GeneratedAnimations";
        [SerializeField]
        private HandAnimationMetaData[] animationsData;
        [SerializeField]
        private int currentAnimationIndex = 0;

        [Header("Mirroring")]
        [SerializeField]
        private bool generateMirroredClips;
        [SerializeField]
        private Handeness handeness;
        [SerializeField]
        private string handLeftPrefix = "_l_";
        [SerializeField]
        private string handRightPrefix = "_r_";

        [Header("Generation")]
        [SerializeField]
        private TextMesh label;
        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;

        [InspectorButton("RecordCurrentData")]
        public string recordCurrentData;


        private HandAnimationMetaData CurrentAnimationData
        {
            get
            {
                return animationsData[currentAnimationIndex];
            }
        }

        private void Reset()
        {
            puppet = this.GetComponent<HandPuppet>();
            root = this.GetComponentInChildren<Animator>()?.transform ?? this.transform;
        }

        private void Start()
        {
            UpdateLabel();
        }

        private void Update()
        {
            if (Input.GetKeyDown(recordKey))
            {
                RecordCurrentData();
            }
        }

        private void RecordCurrentData()
        {
            RecordPose(CurrentAnimationData);
            PrepareNextAnimation();
        }

        public void RecordPose(HandAnimationMetaData metaData)
        {
            AnimationClip clip = new AnimationClip();
            foreach (var bone in puppet.Bones)
            {
                if (metaData.IncludesBone(bone.id))
                {
                    RegisterLocalRotation(ref clip, bone.transform, GetGameObjectPath(bone.transform, root));
                }
            }
            StoreClip(clip, metaData.animationName);

            if(generateMirroredClips)
            {
                string from = handeness == Handeness.Left ? handLeftPrefix : handRightPrefix;
                string to = handeness == Handeness.Left ? handRightPrefix : handLeftPrefix;
                AnimationClip mirrorClip = new AnimationClip();
                foreach (var bone in puppet.Bones)
                {
                    if (metaData.IncludesBone(bone.id))
                    {
                        string mirrorPath = GetGameObjectPath(bone.transform, root).Replace(from, to);
                        RegisterLocalRotation(ref mirrorClip, bone.transform, mirrorPath);
                    }
                }
                StoreClip(mirrorClip, $"{metaData.animationName}{to}");
            }
        }


        private void PrepareNextAnimation()
        {
            currentAnimationIndex = (currentAnimationIndex + 1) % animationsData.Length;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label != null)
            {
                label.text = CurrentAnimationData.animationName;
            }
        }


        private void RegisterLocalRotation(ref AnimationClip clip, Transform transform, string path)
        {
            if (transform == null)
            {
                return;
            }
            Vector3 euler = transform.localEulerAngles;
            clip.SetCurve(path, typeof(Transform), "localEulerAngles.x", AnimationCurve.Constant(0f, 0.01f, euler.x));
            clip.SetCurve(path, typeof(Transform), "localEulerAngles.y", AnimationCurve.Constant(0f, 0.01f, euler.y));
            clip.SetCurve(path, typeof(Transform), "localEulerAngles.z", AnimationCurve.Constant(0f, 0.01f, euler.z));
        }

        private void StoreClip(AnimationClip clip, string animationName)
        {
#if UNITY_EDITOR
            string targetFolder = $"Assets/{folder}/";
            CreateFolder(targetFolder);
            UnityEditor.AssetDatabase.CreateAsset(clip, $"{targetFolder}/{animationName}.anim");
#endif
        }

        private void CreateFolder(string targetFolder)
        {

#if UNITY_EDITOR
            if (!System.IO.Directory.Exists(targetFolder))
            {
                System.IO.Directory.CreateDirectory(targetFolder);
            }
#endif
        }

        private static string GetGameObjectPath(Transform transform, Transform root)
        {
            string path = transform.name;
            while (transform.parent != null
                && transform.parent != root)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }
    }

    [System.Serializable]
    public struct HandAnimationMetaData
    {
        public string animationName;
        public AnimationMask mask;


        public enum AnimationMask
        {
            Full,
            Index,
            Thumb
        };

        private static readonly HashSet<BoneId> INDEX_MASK = new HashSet<BoneId>() { BoneId.Hand_Index1, BoneId.Hand_Index2, BoneId.Hand_Index3 };
        private static readonly HashSet<BoneId> THUMB_MASK = new HashSet<BoneId>() { BoneId.Hand_Thumb0, BoneId.Hand_Thumb1, BoneId.Hand_Thumb2, BoneId.Hand_Thumb3 };

        public bool IncludesBone(BoneId bone)
        {
            switch (mask)
            {
                case AnimationMask.Index: return INDEX_MASK.Contains(bone);
                case AnimationMask.Thumb: return THUMB_MASK.Contains(bone);
                default: return true;
            }
        }
    }
}