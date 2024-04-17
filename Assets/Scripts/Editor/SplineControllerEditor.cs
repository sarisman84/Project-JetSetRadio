using ProjectJetSetRadio.Gameplay;
using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectJetSetRadio.CustomEditors.Gameplay
{
    [CustomEditor(typeof(SplineController))]
    public class SplineControllerEditor : Editor
    {
        private SplineController controller;
        private Plane cursorCollisionPlane;

        private const float segmentSelectDistanceThreshold = 0.1f;
        private int selectedSegmentIndex = -1;

        private Path Path
            => controller.path;

        private void OnEnable()
        {
            controller = target as SplineController;
            if (controller.path == null || controller.path.points == null)
            {
                controller.InitDefaultPath();
                Debug.Log("Init default path");
            }

            cursorCollisionPlane = new Plane(Vector3.up, Path.pathOrigin.y);
        }


        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var initPathButton = new Button();
            initPathButton.text = "Init Path";
            initPathButton.RegisterCallback<ClickEvent>((e) =>
            {
                Undo.RecordObject(controller, "Create new");
                controller.InitDefaultPath();
                SceneView.RepaintAll();
            });
            root.Add(initPathButton);


            var toggleClosedLoop = new Button();
            toggleClosedLoop.text = "Toggle Closed Loop";
            toggleClosedLoop.RegisterCallback<ClickEvent>((e) =>
            {
                Undo.RecordObject(controller, "Toggle closed");
                Path.ToggleClosed();
                SceneView.RepaintAll();
            });
            root.Add(toggleClosedLoop);


            var autoSetControlPointsToggleButton = new Toggle();
            autoSetControlPointsToggleButton.text = "Auto-Set Control Points";
            autoSetControlPointsToggleButton.RegisterCallback<ChangeEvent<bool>>((e) =>
            {
                Undo.RecordObject(controller, "Auto-set control points");
                Path.AutoSetControlPoints = e.newValue;
                SceneView.RepaintAll();
            });
            root.Add(autoSetControlPointsToggleButton);

            return root;
        }

        private void OnSceneGUI()
        {
            Input();
            Draw();
        }

        private void Input()
        {
            Event guiEvent = Event.current;

            var ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);

            if (!cursorCollisionPlane.Raycast(ray, out var dist))
                return;
            var mousePos = ray.GetPoint(dist);
            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
            {
                if (selectedSegmentIndex != -1)
                {
                    Undo.RecordObject(controller, "Split Segment");
                    Path.SplitSegment(mousePos, selectedSegmentIndex);
                }
                else if (!Path.isClosed)
                {
                    Undo.RecordObject(controller, "Add Segment");
                    Path.AddSegment(mousePos);
                }

            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
            {
                var minDistToAnchor = 0.05f;
                var closestAnchorIndex = -1;

                for (int i = 0; i < Path.NumPoints; i += 3)
                {
                    var pDist = Vector3.Distance(mousePos, Path[i]);
                    if (pDist < minDistToAnchor)
                    {
                        minDistToAnchor = pDist;
                        closestAnchorIndex = i;
                    }
                }

                if (closestAnchorIndex != -1)
                {
                    Undo.RecordObject(controller, "Delete segment");
                    Path.DeleteSegment(closestAnchorIndex);
                }
            }

            if (guiEvent.type == EventType.MouseMove)
            {
                var minDstToSegment = segmentSelectDistanceThreshold;
                var newSelectedSegmentIndex = -1;
                for (int i = 0; i < Path.NumSegments; ++i)
                {
                    var points = Path.GetPointsInSegment(i);
                    var dst = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
                    if (dst < minDstToSegment)
                    {
                        minDstToSegment = dst;
                        newSelectedSegmentIndex = i;
                    }
                }

                if (newSelectedSegmentIndex != selectedSegmentIndex)
                {
                    selectedSegmentIndex = newSelectedSegmentIndex;
                    HandleUtility.Repaint();
                }
            }

        }

        void Draw()
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            using (var scope = new Handles.DrawingScope())
            {
                var offset = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                Handles.color = Color.green - offset;
                Handles.DrawSolidDisc(Path.pathOrigin, Vector3.up, 100.0f);
                Handles.color = scope.originalColor;

                for (int i = 0; i < Path.NumSegments; ++i)
                {
                    var points = Path.GetPointsInSegment(i);
                    Handles.color = Color.black;
                    Handles.DrawLine(points[1], points[0]);
                    Handles.DrawLine(points[2], points[3]);
                    Handles.color = selectedSegmentIndex == i && Event.current.shift ? Color.yellow : Color.green;
                    Handles.DrawBezier(points[0], points[3], points[1], points[2], Color.green, null, 5);
                    continue;

                }

                for (int i = 0; i < Path.NumPoints; ++i)
                {
                    Handles.color = i % 3 == 0 ? Color.magenta : Color.white;
                    var size = i % 3 == 0 ? 0.1f : 0.05f;
                    var newPos = Handles.FreeMoveHandle(Path[i], size, Vector3.zero, Handles.SphereHandleCap);
                    if (Path[i] != newPos)
                    {
                        Undo.RecordObject(controller, "Move Point");
                        Path.MovePoint(i, newPos);
                    }
                }

            }
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        }
    }
}