using Spyro;
using UnityEngine;

namespace ProjectJetSetRadio.Gameplay.CustomDebug
{
    public class DebugService
    {
        public static DebugService Instance
            => ServiceLocator<DebugService>.Service;

        private Debugger debugger;

        public void RegisterDebugger(Debugger debugger)
            => this.debugger = debugger;

        public static void SetCustomData(string key, string value)
        {
            Instance.debugger.SetCustomData(key, value);
        }

        public static void DrawCube(string id, DebugDesc desc)
        {
            Instance.debugger.DrawCube(id, desc);
        }
    }
}
