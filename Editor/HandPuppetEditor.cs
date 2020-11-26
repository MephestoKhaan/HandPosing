using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HandPosing.Editor
{
    [CustomEditor(typeof(HandPuppet))]
    public class HandPuppetEditor : UnityEditor.Editor
    {
        public void OnSceneGUI()
        {
            HandPuppet puppet = (target as HandPuppet);

            if (puppet?.Bones != null)
            {
                DrawBonesRotator(puppet.Bones, puppet);
            }
        }


        private void DrawBonesRotator(List<BoneMap> bones, HandPuppet puppet)
        {
            float scale = 0.25f;
            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                   Vector3.zero,
                   Quaternion.identity,
                   Vector3.one * scale);

            using (new Handles.DrawingScope(handleMatrix))
            {
                foreach (var bone in bones)
                {
                    Quaternion rotation = Handles.RotationHandle(bone.transform.rotation * Quaternion.Euler(bone.rotationOffset), 
                        bone.transform.position / scale);
                    bone.rotationOffset = (Quaternion.Inverse(bone.transform.rotation) *  rotation).eulerAngles;
                }
            }
        }
    }
}
