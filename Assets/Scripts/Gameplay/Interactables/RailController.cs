using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

namespace ProjectJetSetRadio.Gameplay
{
    [RequireComponent(typeof(SplineContainer))]
    public class RailController : MonoBehaviour
    {
        SplineContainer rail;

        private float _railProgress;
        private float3 _nearestPositionOnRail;
        private float _distanceFromRail;

        public Vector3 boundsSizeOffset;
        public float RailProgress
            => _railProgress;
        public Vector3 NearestPointOnRail
            => _nearestPositionOnRail + (float3)transform.position;
        public float DistanceFromRail
            => _distanceFromRail;

        public Vector3 ForwardRailDirection
            => Vector3.Normalize(Rail.EvaluateTangent(_railProgress));
        public Vector3 RailNormal
            => Vector3.Normalize(Rail.EvaluateUpVector(_railProgress));



        public Bounds RailBounds
        {
            get
            {
                var bounds = Rail.Spline.GetBounds();
                bounds.size = new Vector3(bounds.size.x + boundsSizeOffset.x, bounds.size.y + boundsSizeOffset.y, bounds.size.z + boundsSizeOffset.z);
                bounds.center += transform.position + (Vector3.up * bounds.extents.y);
                return bounds;
            }
        }

        private SplineContainer Rail
        {
            get
            {
                if (!rail)
                    rail = GetComponent<SplineContainer>();

                return rail;
            }
        }


        private void Awake()
        {
            rail = GetComponent<SplineContainer>();
            RailService.Instance.RegisterRail(this);
        }




        //public bool IsPointNearTheRail(Vector3 point, float distanceThreshold = 0.5f)
        //{
        //    var distance = SplineUtility.GetNearestPoint(rail.Spline, point, out _, out _);
        //    return distance <= distanceThreshold;
        //}


        public void UpdateController(SkateController target)
        {
            Debug.Log("Updating Rail!");
            var native = new NativeSpline(rail.Spline);
            _distanceFromRail = SplineUtility.GetNearestPoint(
                native,
                target.transform.position - transform.position,
                out _nearestPositionOnRail,
                out _railProgress
                );
        }

        public bool Contains(Vector3 point)
        {
            var bounds = RailBounds;
            return bounds.Contains(point);
        }

        public bool Intersects(Bounds bounds)
        {
            var b = RailBounds;
            return b.Intersects(bounds);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(NearestPointOnRail, 0.5f);

            if (rail == null)
            {
                rail = GetComponent<SplineContainer>();
            }

            var bounds = RailBounds;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}