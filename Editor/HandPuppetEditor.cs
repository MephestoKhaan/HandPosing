using HandPosing.SnapRecording;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HandPosing.Editor
{
    [CustomEditor(typeof(HandGhost))]
    public class HandGhostEditor : UnityEditor.Editor
    {
        public void OnSceneGUI()
        {
            HandGhost ghost = (target as HandGhost);
            HandPuppet puppet = ghost.GetComponent<HandPuppet>();
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
                    Quaternion rotation = Handles.RotationHandle(bone.transform.rotation, bone.transform.position / scale);
                    bone.transform.rotation = rotation;
                }
            }
        }
    }
}
