using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PoseAuthoring.Editor
{
    [CustomEditor(typeof(CylinderHandle))]
    [CanEditMultipleObjects]
    public class CylinderHandleEditor : UnityEditor.Editor
    {
        private static readonly Color NONINTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.1f);
        private static readonly Color INTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.5f);

        private static readonly int STARTHANDLE_HASH = "startHandle".GetHashCode();


        private ArcHandle topArc = new ArcHandle();

        private void OnEnable()
        {
            topArc.SetColorWithRadiusHandle(INTERACTABLE_COLOR, NONINTERACTABLE_COLOR.a);
        }

        public void OnSceneGUI()
        {
            CylinderHandle cylinder = (target as CylinderHandle);

            DrawEndsCaps(cylinder);
            DrawArcEditor(cylinder);
            if (Event.current.type == EventType.Repaint)
            {
                DrawCylinderVolume(cylinder);
            }
        }

        private void DrawEndsCaps(CylinderHandle cylinder)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 startPosition = Handles.PositionHandle(cylinder.StartPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(cylinder, "Change Start Cylinder Position");
                cylinder.StartPoint = startPosition;
            }
            EditorGUI.BeginChangeCheck();
            Vector3 endPosition = Handles.PositionHandle(cylinder.EndPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(cylinder, "Change Start Cylinder Position");
                cylinder.EndPoint = endPosition;
            }
        }

        private void DrawCylinderVolume(CylinderHandle cylinder)
        {
            Vector3 start = cylinder.StartPoint;
            Vector3 end = cylinder.EndPoint;

            Handles.color = NONINTERACTABLE_COLOR;
            Handles.DrawSolidArc(end,
            cylinder.Direction,
            cylinder.Tangent,
            cylinder.angle,
            cylinder.radious);
            Handles.DrawWireDisc(end, cylinder.Direction, cylinder.radious);

            Handles.DrawLine(start,end);
            Handles.DrawLine(start + cylinder.Tangent * cylinder.radious,
                end + cylinder.Tangent * cylinder.radious);
            Handles.DrawLine(start + Quaternion.AngleAxis(cylinder.angle, cylinder.Direction) * cylinder.Tangent * cylinder.radious,
                end + Quaternion.AngleAxis(cylinder.angle, cylinder.Direction) * cylinder.Tangent * cylinder.radious);
        }

        private void DrawArcEditor(CylinderHandle cylinder)
        {
            topArc.angle = cylinder.angle;
            topArc.radius = cylinder.radious;
            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                cylinder.StartPoint,
                Quaternion.LookRotation(cylinder.Tangent, cylinder.Direction),
                Vector3.one
            );
            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();

                Handles.color = Color.white;

                topArc.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(cylinder, "Change Cylinder Properties");
                    cylinder.angle = topArc.angle;
                    cylinder.radious = topArc.radius;
                }
            }
        }
    }
}