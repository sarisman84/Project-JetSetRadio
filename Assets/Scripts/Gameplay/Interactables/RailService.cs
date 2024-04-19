using Spyro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using Object = UnityEngine.Object;

namespace ProjectJetSetRadio.Gameplay
{
    public class RailService
    {
        public static RailService Instance =>
                ServiceLocator<RailService>.Service;


        private Dictionary<int, RailController> railRegistry;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ManualReset()
        {
            if (Instance != null)
                Instance.railRegistry = new Dictionary<int, RailController>();
        }


        public RailService()
        {
            railRegistry = new Dictionary<int, RailController>();
        }
        public void RegisterRail(RailController controller)
        {
            railRegistry.Add(controller.gameObject.GetInstanceID(), controller);
        }


        public bool TryGetClosestIntersectingRail(SkateController player, out RailController targetRail)
        {
            targetRail = null;
            foreach (var rail in railRegistry.Values)
            {
                rail.UpdateController(player);
                if (IsCloseToRail(rail, player))
                {
                    if (rail.Intersects(player.Hitbox))
                    {
                        targetRail = rail;
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        private bool IsCloseToRail(RailController rail, SkateController player)
        {
            var threshold = player.settings.railDetectionRangeFromPlayer;
            var pos = rail.NearestPointOnRail;

            return Vector3.Distance(player.transform.position, pos) <= threshold;
        }
    }
}
