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
            Vector3 snapP = surface.transform.position;


            float rightDistance = DistanceToPoint(centre, right * size.x * 0.5f, snapP);
            centre.x += rightDistance * 0.5f;
            size.x += Mathf.Abs(rightDistance);

            float upDistance = DistanceToPoint(centre, up * size.y * 0.5f, snapP);
            centre.y += upDistance * 0.5f;
            size.y += Mathf.Abs(upDistance);

            float forwardDistance = DistanceToPoint(centre, forward * size.z * 0.5f, snapP);
            centre.z += forwardDistance * 0.5f;
            size.z += Mathf.Abs(forwardDistance);

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
                    surface.Centre = centre + boxHandle.center;//rotation?
                }
            }

        }

        private float DistanceToPoint(Vector3 centre, Vector3 dir, Vector3 point)
        {
            Vector3 pointProj = centre + Vector3.Project(point - centre, dir.normalized);
            Vector3 start = centre - dir;
            Vector3 end = centre + dir;
            if (Vector3.Dot(end - start, pointProj - start) < 0f)
            {
                return -Vector3.Distance(pointProj, start);
            }
            if (Vector3.Dot(start - end, pointProj - end) < 0f)
            {
                return Vector3.Distance(pointProj, end);
            }
            return 0f;
        }
    }
}