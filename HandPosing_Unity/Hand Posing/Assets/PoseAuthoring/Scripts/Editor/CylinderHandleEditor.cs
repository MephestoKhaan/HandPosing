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


        ArcHandle topArc = new ArcHandle();

        public void OnSceneGUI()
        {
            CylinderHandle cylinder = (target as CylinderHandle);


            if (Event.current.type == EventType.Repaint)
            {
                DrawCylinderVolume(cylinder);
            }

            DrawArcEditor(cylinder);
        }

        private void DrawCylinderVolume(CylinderHandle cylinder)
        {
            Handles.color = NONINTERACTABLE_COLOR;
            Handles.DrawSolidArc(cylinder.EndPoint,
            cylinder.Direction,
            cylinder.Tangent,
            cylinder.angle,
            cylinder.radious);
            Handles.DrawWireDisc(cylinder.EndPoint, cylinder.Direction, cylinder.radious);

            Handles.DrawDottedLine(cylinder.StartPoint,
                cylinder.EndPoint,
                1f);
            Handles.DrawLine(cylinder.StartPoint + cylinder.Tangent * cylinder.radious,
                cylinder.EndPoint + cylinder.Tangent * cylinder.radious);
            Handles.DrawLine(cylinder.StartPoint + Quaternion.AngleAxis(cylinder.angle, cylinder.Direction) * cylinder.Tangent * cylinder.radious,
                cylinder.EndPoint + Quaternion.AngleAxis(cylinder.angle, cylinder.Direction) * cylinder.Tangent * cylinder.radious);
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

                Handles.color = INTERACTABLE_COLOR;
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