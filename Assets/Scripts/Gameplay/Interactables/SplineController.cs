using Spyro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectJetSetRadio.Gameplay
{
    [Serializable]
    public class Path
    {
        [SerializeField, HideInInspector]
        public List<Vector3> points;
        [SerializeField, HideInInspector]
        public bool isClosed;
        [SerializeField, HideInInspector]
        public bool autoSetControlPoints;
        [SerializeField, HideInInspector]
        public Vector3 pathOrigin;

        public Path(Vector3 center)
        {
            pathOrigin = center;
            points = new List<Vector3>()
            {
                center + Vector3.forward,
                center + Vector3.forward + Vector3.back * 0.5f,
                center + Vector3.back + Vector3.forward * 0.5f,
                center + Vector3.back
            };
        }
        public Vector3 this[int i]
            => points[i];
        public int NumPoints
            => points.Count;
        public int NumSegments
            => points.Count / 3;
        public bool AutoSetControlPoints
        {
            get => autoSetControlPoints;
            set
            {
                if (autoSetControlPoints != value)
                {
                    autoSetControlPoints = value;
                    if (autoSetControlPoints)
                    {
                        AutoSetAllControlPoints();
                    }
                }
            }
        }

        public Vector3 EvaluateDelta(Vector3 position)
        {
            throw new NotImplementedException();
        }

        public void AddSegment(Vector3 anchorPos)
        {
            points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
            points.Add((points[points.Count - 1] + anchorPos) * 0.5f);
            points.Add(anchorPos);


            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(points.Count - 1);
            }
        }
        public void SplitSegment(Vector3 anchorPos, int segmentIndex)
        {
            points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPos, Vector3.zero });
            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
            }
            else
            {
                AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
            }
        }

        public void DeleteSegment(int anchorIndex)
        {
            if (NumSegments > 2 || !isClosed && NumSegments > 1)
            {
                if (anchorIndex == 0)
                {
                    if (isClosed)
                    {
                        points[points.Count - 1] = points[2];
                        points.RemoveRange(0, 3);
                    }
                    else if (anchorIndex == points.Count - 1 && !isClosed)
                    {
                        points.RemoveRange(anchorIndex - 2, 3);
                    }
                }
                else
                {
                    points.RemoveRange(anchorIndex - 1, 3);
                }
            }
        }
        public Vector3[] GetPointsInSegment(int i)
        {
            return new Vector3[] {
                points[i * 3],
                points[i * 3 + 1],
                points[i * 3 + 2],
                points[LoopIndex(i * 3 + 3)]
            };
        }

        public void MovePoint(int i, Vector3 pos)
        {
            var deltaMove = pos - points[i];
            points[i] = pos;

            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(i);
            }
            else
            {
                if (i % 3 == 0) //If Anchor
                {
                    if (i + 1 < points.Count || isClosed)
                    {
                        points[LoopIndex(i + 1)] += deltaMove;
                    }

                    if (i - 1 >= 0 || isClosed)
                    {
                        points[LoopIndex(i - 1)] += deltaMove;
                    }
                }
                else
                {
                    var nextPointIsAnchor = (i + 1) % 3 == 0;
                    var correspondingControlIndex = nextPointIsAnchor ? i + 2 : i - 2;
                    var anchorIndex = nextPointIsAnchor ? i + 1 : i - 1;

                    if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count || isClosed)
                    {
                        var dist = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspondingControlIndex)]).magnitude;
                        var dir = (points[LoopIndex(anchorIndex)] - pos).normalized;

                        points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorIndex)] + dir * dist;
                    }
                }
            }

        }

        public void ToggleClosed()
        {
            isClosed = !isClosed;

            if (isClosed)
            {
                points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                points.Add(points[0] * 2 - points[1]);

                if (autoSetControlPoints)
                {
                    AutoSetAnchorControlPoints(0);
                    AutoSetAnchorControlPoints(points.Count - 3);
                }
            }
            else
            {
                points.RemoveRange(points.Count - 2, 2);
                if (autoSetControlPoints)
                {
                    AutoSetStartAndEndControls();
                }
            }
        }

        void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
        {
            for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3)
            {
                if (i >= 0 && i < points.Count || isClosed)
                {
                    AutoSetAnchorControlPoints(LoopIndex(i));
                }
            }

            AutoSetStartAndEndControls();
        }

        void AutoSetAllControlPoints()
        {
            for (int i = 0; i < points.Count; i += 3)
            {
                AutoSetAnchorControlPoints(i);
            }

            AutoSetStartAndEndControls();
        }

        void AutoSetAnchorControlPoints(int anchorIndex)
        {
            var anchorPos = points[anchorIndex];
            var dir = Vector3.zero;
            var neighbourDists = new float[2];

            if (anchorIndex - 3 >= 0 || isClosed)
            {
                var offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
                dir += offset.normalized;
                neighbourDists[0] = offset.magnitude;
            }

            if (anchorIndex + 3 >= 0 || isClosed)
            {
                var offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
                dir -= offset.normalized;
                neighbourDists[1] = -offset.magnitude;
            }

            dir.Normalize();

            for (int i = 0; i < 2; ++i)
            {
                var controlIndx = anchorIndex + i * 2 - 1;
                if (controlIndx >= 0 && controlIndx < points.Count || isClosed)
                {
                    points[LoopIndex(controlIndx)] = anchorPos + dir * neighbourDists[i] * 0.5f;
                }
            }
        }


        private void AutoSetStartAndEndControls()
        {
            if (!isClosed)
            {
                points[1] = (points[0] + points[2]) * 0.5f;
                points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * 0.5f;
            }
        }

        private int LoopIndex(int i)
        {
            return (i + points.Count) % points.Count;
        }

    }
    public class SplineController : MonoBehaviour
    {
        [HideInInspector]
        public Path path;

        public void InitDefaultPath()
        {
            path = new Path(transform.position);
        }

        private void Reset()
        {
            InitDefaultPath();
        }



    }
}

