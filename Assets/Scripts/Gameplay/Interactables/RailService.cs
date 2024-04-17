using Spyro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectJetSetRadio.Gameplay
{
    public class RailService
    {
        public static RailService Instance =>
                ServiceLocator<RailService>.Service;


        private Dictionary<int, RailController> railRegistry;


        public RailService()
        {
            railRegistry = new Dictionary<int, RailController>();
        }
        public void RegisterRail(RailController controller)
        {
            railRegistry.Add(controller.gameObject.GetInstanceID(), controller);
        }


        public bool TryLandingOnRail(SkateController player, out RailController targetRail)
        {
            targetRail = null;
            foreach (var rail in railRegistry.Values)
            {
                if (rail.Contains(player.transform.position))
                {
                    rail.UpdateController(player);
                    if (IsCloseToRail(rail, player))
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
