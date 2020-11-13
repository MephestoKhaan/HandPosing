using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PoseAuthoring.PoseSurfaces.Editor
{
    [CustomEditor(typeof(BoxSurface))]
    [CanEditMultipleObjects]
    public class BoxEditor : UnityEditor.Editor
    {
        private static readonly Color NONINTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.1f);
        private static readonly Color INTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.5f);

        private BoxBoundsHandle boxHandle = new BoxBoundsHandle();

        private void OnEnable()
        {
            boxHandle.SetColor(INTERACTABLE_COLOR);
            boxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Z;
        }

        public void OnSceneGUI()
        {
            BoxSurface surface = (target as BoxSurface);

            DrawRotator(surface);
            DrawBoxEditor(surface);
        }

        private void DrawRotator(BoxSurface surface)
        {
            EditorGUI.BeginChangeCheck();
            Quaternion rotation = Handles.RotationHandle(surface.Rotation, surface.transform.position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Rotation Box");
                surface.Rotation = rotation;
            }
        }

        private void DrawBoxEditor(BoxSurface surface)
        {
            Quaternion rot = surface.Rotation;
            Vector3 size = surface.Size;

            Vector3 snapP = surface.transform.position;

            boxHandle.size = size;
            float widthPos = Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, surface.WidthOffset);
            boxHandle.center = new Vector3(widthPos, 0f, size.z * 0.5f);

            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                snapP,
                rot,
                Vector3.one
            );

            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();
                boxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(surface, "Change Box Properties");

                    surface.Size = boxHandle.size;
                    float width = boxHandle.size.x;
                    surface.WidthOffset = width != 0f ? (boxHandle.center.x + width * 0.5f) / width : 0f;
                }
            }
        }
    }
}