using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PoseAuthoring.PoseSurfaces.Editor
{
    [CustomEditor(typeof(CylinderSurface))]
    [CanEditMultipleObjects]
    public class CylinderEditor : UnityEditor.Editor
    {
        private static readonly Color NONINTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.1f);
        private static readonly Color INTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.5f);
        private const float DRAWSURFACE_RESOLUTION = 5f;

        private ArcHandle topArc = new ArcHandle();
        private Vector3[] surfaceEdges;

        private void OnEnable()
        {
            topArc.SetColorWithRadiusHandle(INTERACTABLE_COLOR, 0f);
        }

        public void OnSceneGUI()
        {
            CylinderSurface surface = (target as CylinderSurface);

            DrawEndsCaps(surface);
            DrawArcEditor(surface);
            if (Event.current.type == EventType.Repaint)
            {
                DrawSurfaceVolume(surface);
            }
        }

        private void DrawEndsCaps(CylinderSurface surface)
        {
            EditorGUI.BeginChangeCheck();

            Quaternion handleRotation = (surface.relativeTo ?? surface.transform).rotation;

            Vector3 startPosition = Handles.PositionHandle(surface.StartPoint, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Start Cylinder Position");
                surface.StartPoint = startPosition;
            }
            EditorGUI.BeginChangeCheck();
            Vector3 endPosition = Handles.PositionHandle(surface.EndPoint, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Start Cylinder Position");
                surface.EndPoint = endPosition;
            }
        }

        private void DrawSurfaceVolume(CylinderSurface surface)
        {
            Vector3 start = surface.StartPoint;
            Vector3 end = surface.EndPoint;
            float radious = surface.Radious;

            Handles.color = INTERACTABLE_COLOR;
            Handles.DrawWireArc(end,
            surface.Direction,
            surface.StartAngleDir,
            surface.Angle,
            radious);

            Handles.DrawLine(start,end);
            Handles.DrawLine(start, start + surface.StartAngleDir * radious);
            Handles.DrawLine(start, start + surface.EndAngleDir * radious);
            Handles.DrawLine(end,end + surface.StartAngleDir * radious);
            Handles.DrawLine(end, end + surface.EndAngleDir * radious);

            int edgePoints = Mathf.CeilToInt((2 * surface.Angle) / DRAWSURFACE_RESOLUTION) + 3;
            if(surfaceEdges == null 
                || surfaceEdges.Length != edgePoints)
            {
                surfaceEdges = new Vector3[edgePoints];
            }

            Handles.color = NONINTERACTABLE_COLOR;
            int i = 0;
            for(float angle = 0f; angle < surface.Angle; angle += DRAWSURFACE_RESOLUTION)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, surface.Direction) * surface.StartAngleDir;
                surfaceEdges[i++] = start + direction * radious;
                surfaceEdges[i++] = end + direction * radious;
            }
            surfaceEdges[i++] = start + surface.EndAngleDir * radious;
            surfaceEdges[i++] = end + surface.EndAngleDir * radious;
            Handles.DrawPolyLine(surfaceEdges);

        }

        private void DrawArcEditor(CylinderSurface surface)
        {
            float radious = surface.Radious;
            topArc.angle = surface.Angle;
            topArc.radius = radious;
            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                surface.StartPoint,
                Quaternion.LookRotation(surface.StartAngleDir, surface.Direction),
                Vector3.one
            );
            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();

                Handles.color = Color.white;
                topArc.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(surface, "Change Cylinder Properties");
                    surface.Angle = topArc.angle;
                    radious = topArc.radius;
                }
            }
        }
    }
}