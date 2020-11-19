using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace HandPosing.SnapSurfaces.Editor
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

            Handles.DrawLine(bottomLeft + rightRot * surface.SnapOffset.y, bottomRight + rightRot * surface.SnapOffset.x);
            Handles.DrawLine(topLeft - rightRot * surface.SnapOffset.x, topRight - rightRot * surface.SnapOffset.y);
            Handles.DrawLine(bottomLeft - forwardRot * surface.SnapOffset.z, topLeft - forwardRot * surface.SnapOffset.w);
            Handles.DrawLine(bottomRight + forwardRot * surface.SnapOffset.w, topRight + forwardRot * surface.SnapOffset.z);
        }

        private void DrawSlider(BoxSurface surface)
        {
            Handles.color = INTERACTABLE_COLOR;

            EditorGUI.BeginChangeCheck();
            Vector3 rightDir = surface.Rotation * Vector3.right;
            Vector3 forwardDir = surface.Rotation * Vector3.forward;
            Vector3 bottomRight = surface.transform.position
                + rightDir * surface.Size.x * (surface.WidthOffset);
            Vector3 bottomLeft = surface.transform.position
                - rightDir * surface.Size.x * (1f - surface.WidthOffset);
            Vector3 topRight = bottomRight + forwardDir * surface.Size.z;

            Vector3 rightHandle = DrawOffsetHandle(bottomRight + rightDir * surface.SnapOffset.x, rightDir);
            Vector3 leftHandle = DrawOffsetHandle(bottomLeft + rightDir * surface.SnapOffset.y, -rightDir);
            Vector3 topHandle = DrawOffsetHandle(topRight + forwardDir * surface.SnapOffset.z, forwardDir);
            Vector3 bottomHandle = DrawOffsetHandle(bottomRight + forwardDir * surface.SnapOffset.w, -forwardDir);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Offset Box");
                Vector4 offset = surface.SnapOffset;
                offset.x = DistanceToHandle(bottomRight, rightHandle, rightDir);
                offset.y = DistanceToHandle(bottomLeft, leftHandle, rightDir);
                offset.z = DistanceToHandle(topRight, topHandle, forwardDir);
                offset.w = DistanceToHandle(bottomRight, bottomHandle, forwardDir);
                surface.SnapOffset = offset;
            }
        }

        private Vector3 DrawOffsetHandle(Vector3 point, Vector3 dir)
        {
            float size = HandleUtility.GetHandleSize(point) * 0.2f;
            return Handles.Slider(point, dir, size, Handles.ConeHandleCap, 0f);
        }

        private float DistanceToHandle(Vector3 origin, Vector3 handlePoint, Vector3 dir)
        {
            float distance = Vector3.Distance(origin, handlePoint);
            if (Vector3.Dot(handlePoint - origin, dir) < 0f)
            {
                distance = -distance;
            }
            return distance;
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