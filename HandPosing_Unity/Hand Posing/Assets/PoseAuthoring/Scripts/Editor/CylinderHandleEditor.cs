using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(CylinderHandle))]
[CanEditMultipleObjects]
public class CylinderHandleEditor : Editor
{
    private static readonly Color NONINTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.3f);
    private static readonly Color INTERACTABLE_COLOR = new Color(0f, 1f, 1f, 1f);


    //ArcHandle arcHandle = new ArcHandle();

    public void OnSceneGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
           // return;
        }
        CylinderHandle cylinder = (target as CylinderHandle);

        EditorGUI.BeginChangeCheck();

        Handles.color = NONINTERACTABLE_COLOR;
        Handles.DrawWireDisc(cylinder.StartPoint, cylinder.Direction, cylinder.radious);
        Handles.DrawSolidArc(cylinder.StartPoint, cylinder.Direction, cylinder.Tangent, cylinder.angle, cylinder.radious);




        Handles.color = INTERACTABLE_COLOR;
        Vector3 startAnglePosition = cylinder.StartPoint + cylinder.Tangent * cylinder.radious;
        Handles.CylinderHandleCap(
            0,
            startAnglePosition,
            Quaternion.identity,
            HandleUtility.GetHandleSize(startAnglePosition) * 0.25f,
            EventType.Repaint
        );

        Vector3 endAnglePosition = cylinder.StartPoint + Quaternion.AngleAxis(cylinder.angle, cylinder.Direction) * (cylinder.Tangent * cylinder.radious);
        Handles.CylinderHandleCap(
            0,
            endAnglePosition,
            Quaternion.identity,
            HandleUtility.GetHandleSize(endAnglePosition) * 0.25f,
            EventType.Repaint
        );



        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Cylinder Rotate");

        }
    }
}