using Spyro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectJetSetRadio.Gameplay
{
    public class PlayerService
    {
        public static PlayerService Instance
            => ServiceLocator<PlayerService>.Service;

        public SkateController Player
            => player;
        public SkateControllerSettings PlayerSettings
            => player.settings;

        private SkateController player;

        public void RegisterPlayer(SkateController controller)
            => player = controller;


    }
}
