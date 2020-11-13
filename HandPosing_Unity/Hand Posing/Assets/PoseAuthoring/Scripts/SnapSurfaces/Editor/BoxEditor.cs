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
            boxHandle.handleColor = INTERACTABLE_COLOR;
            boxHandle.wireframeColor = NONINTERACTABLE_COLOR;
            boxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Z;
        }

        public void OnSceneGUI()
        {
            BoxSurface surface = (target as BoxSurface);

            DrawRotator(surface);
            DrawBoxEditor(surface);
            DrawSlider(surface);

            if (Event.current.type == EventType.Repaint)
            {
                DrawSnapLines(surface);
            }
        }

        private void DrawSnapLines(BoxSurface surface)
        {
            Handles.color = INTERACTABLE_COLOR;

            Vector3 rightRot = surface.Rotation * Vector3.right;
            Vector3 forwardRot = surface.Rotation * Vector3.forward;
            Vector3 forwardOffset = forwardRot * surface.Size.z;

            Vector3 bottomLeft = surface.transform.position - rightRot * surface.Size.x * (1f - surface.WidthOffset);
            Vector3 bottomRight = surface.transform.position + rightRot * surface.Size.x * (surface.WidthOffset);
            Vector3 topLeft = bottomLeft + forwardOffset;
            Vector3 topRight = bottomRight + forwardOffset;

            Handles.DrawLine(bottomLeft + rightRot * surface.SnapOffset, bottomRight + rightRot * surface.SnapOffset);
            Handles.DrawLine(topLeft - rightRot * surface.SnapOffset, topRight - rightRot * surface.SnapOffset);
            Handles.DrawLine(bottomLeft - forwardRot * surface.SnapOffset, topLeft - forwardRot * surface.SnapOffset);
            Handles.DrawLine(bottomRight + forwardRot * surface.SnapOffset, topRight + forwardRot * surface.SnapOffset);
        }

        private void DrawSlider(BoxSurface surface)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 rightRot = surface.Rotation * Vector3.right;
            Vector3 bottomRight = surface.transform.position
                + rightRot * surface.Size.x * (surface.WidthOffset);

            Vector3 offset = Handles.Slider(bottomRight + rightRot * surface.SnapOffset, rightRot);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Offset Box");
                float distance = Vector3.Distance(bottomRight, offset);
                if (Vector3.Dot(offset - bottomRight, rightRot) < 0f)
                {
                    distance = -distance;
                }
                surface.SnapOffset = distance;
            }
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

        private float RemapClamped(float value, (float, float) from, (float, float) to)
        {
            value = Mathf.Clamp(value, from.Item1, from.Item2);
            return to.Item1 + (value - from.Item1) * (to.Item2 - to.Item1) / (from.Item2 - from.Item1);
        }
    }
}