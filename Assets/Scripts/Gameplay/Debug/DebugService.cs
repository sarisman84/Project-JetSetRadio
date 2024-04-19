using Spyro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectJetSetRadio.Gameplay.CustomDebug
{
    public class DebugService
    {
        public static DebugService Instance
            => ServiceLocator<DebugService>.Service;

        private Debugger debugger;

        public void RegisterDebugger(Debugger debugger)
            => this.debugger = debugger;
    }
}
