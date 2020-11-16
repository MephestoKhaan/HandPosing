using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PoseAuthoring.PoseSurfaces.Editor
{
    [CustomEditor(typeof(SphereSurface))]
    [CanEditMultipleObjects]
    public class SphereEditor : UnityEditor.Editor
    {
        private static readonly Color NONINTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.1f);
        private static readonly Color INTERACTABLE_COLOR = new Color(0f, 1f, 1f, 0.5f);

        private SphereBoundsHandle sphereHandle = new SphereBoundsHandle();

        private void OnEnable()
        {
            sphereHandle.SetColor(INTERACTABLE_COLOR);
            sphereHandle.midpointHandleDrawFunction = null;
        }

        public void OnSceneGUI()
        {
            SphereSurface surface = (target as SphereSurface);

            DrawCentre(surface);
            Handles.color = Color.white;
            DrawSphereEditor(surface);
            
            if (Event.current.type == EventType.Repaint)
            {
                DrawSurfaceVolume(surface);
            }
        }

        private void DrawCentre(SphereSurface surface)
        {
            EditorGUI.BeginChangeCheck();

            Quaternion handleRotation = (surface.relativeTo ?? surface.transform).rotation;

            Vector3 centrePosition = Handles.PositionHandle(surface.Centre, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Centre Sphere Position");
                surface.Centre = centrePosition;
            }
        }

        private void DrawSurfaceVolume(SphereSurface surface)
        {
            Handles.color = INTERACTABLE_COLOR;
            Vector3 startLine = surface.Centre;
            Vector3 endLine = startLine + surface.Rotation * Vector3.forward * surface.Radious;
            Handles.DrawDottedLine(startLine, endLine, 5);
        }

        private void DrawSphereEditor(SphereSurface surface)
        {
            float radious = surface.Radious;
            sphereHandle.radius = radious;
            sphereHandle.center = surface.Centre;

            EditorGUI.BeginChangeCheck();
            sphereHandle.DrawHandle();
        }
    }
}