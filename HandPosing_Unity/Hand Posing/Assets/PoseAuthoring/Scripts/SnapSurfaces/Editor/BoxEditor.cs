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
            //boxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Z;
        }

        public void OnSceneGUI()
        {
            BoxSurface surface = (target as BoxSurface);

            DrawCentre(surface);
            DrawBoxEditor(surface);

            if (Event.current.type == EventType.Repaint)
            {
                DrawSurfaceVolume(surface);
            }
        }

        private void DrawCentre(BoxSurface surface)
        {
            EditorGUI.BeginChangeCheck();

            Quaternion handleRotation = (surface.relativeTo ?? surface.transform).rotation;

            Vector3 centrePosition = Handles.PositionHandle(surface.Centre, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Centre Box Position");
                surface.Centre = centrePosition;
            }
        }

        private void DrawSurfaceVolume(BoxSurface surface)
        {
            Handles.color = INTERACTABLE_COLOR;
            Vector3 startLine = surface.Centre;
            Vector3 endLine = surface.transform.position;
            Handles.DrawDottedLine(startLine, endLine, 5);
        }

        private void DrawBoxEditor(BoxSurface surface)
        {
            Quaternion rot = surface.Rotation;
            Vector3 gripDir = surface.Direction.normalized;
            Vector3 size = surface.Size;
            Vector3 centre = surface.Centre;
            Vector3 right = rot * Vector3.right;
            Vector3 up = rot * Vector3.up;
            Vector3 forward = rot * Vector3.forward;
            float Xdot = Vector3.Dot(right, gripDir);
            float Ydot = Vector3.Dot(up, gripDir);
            float Zdot = Vector3.Dot(forward, gripDir);

            Vector3 snapP = surface.transform.position;

            if (Mathf.Abs(Xdot) >= Mathf.Abs(Ydot)
                 && Mathf.Abs(Xdot) >= Mathf.Abs(Zdot))
            {
                Vector3 planeN = right * Mathf.Sign(Xdot);
                size.x = DistanceToSnap(centre, snapP, planeN) * 2f;
            }
            else if (Mathf.Abs(Ydot) >= Mathf.Abs(Xdot)
                 && Mathf.Abs(Ydot) >= Mathf.Abs(Zdot))
            {
                Vector3 planeN = up * Mathf.Sign(Ydot);
                size.y = DistanceToSnap(centre, snapP, planeN) * 2f;
            }
            else
            {
                Vector3 planeN = forward * Mathf.Sign(Zdot);
                size.z = DistanceToSnap(centre, snapP, planeN) * 2f;
            }

            boxHandle.size = size;
            boxHandle.center = Vector3.zero;

            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                centre,
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
                    surface.Centre = centre + boxHandle.center;
                }
            }

        }

        private float DistanceToSnap(Vector3 centre, Vector3 snapP, Vector3 planeN)
        {
            Vector3 projectedSnap = snapP + Vector3.ProjectOnPlane(centre - snapP, planeN);
            float distance = Vector3.Distance(centre, projectedSnap);
            return distance;
        }
    }
}