using UnityEditor;
using UnityEngine;

namespace InteractionSystem
{
    /// <summary>
    /// Draws the detection shape in the Scene view while <see cref="InteractionManager"/> is running: the
    /// line (Line) or the sphere (Sphere), yellow while nothing is detected, green while something is.
    /// </summary>
    [InitializeOnLoad]
    internal static class InteractGizmos
    {
        private static readonly Color IdleColor = Color.yellow;
        private static readonly Color DetectingColor = Color.green;

        static InteractGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var data = InteractionManager.Data;
            var origin = InteractionManager.Origin;
            if (!EditorApplication.isPlaying || data == null || origin == null)
            {
                return;
            }

            Handles.color = InteractionManager.Current != null ? DetectingColor : IdleColor;
            var position = InteractionManager.RayOrigin;
            var radius = data.Settings.Radius;

            if (data.Settings.RaycastType == RaycastType.Line)
            {
                Handles.DrawLine(position, position + InteractionManager.RayDirection * radius, 2f);
                return;
            }

            Handles.DrawWireDisc(position, Vector3.up, radius, 2f);
            Handles.DrawWireDisc(position, Vector3.right, radius, 2f);
            Handles.DrawWireDisc(position, Vector3.forward, radius, 2f);
        }
    }
}
