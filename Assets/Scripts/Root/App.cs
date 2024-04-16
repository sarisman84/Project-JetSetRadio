using ProjectJetSetRadio.Gameplay;
using Spyro;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectJetSetRadio
{

    public class App : MonoBehaviour
    {
        [Recursive]
        public InputActionAsset inputSettings;

        private void Awake()
        {
            ServiceLocator<InputService>.Service.Init(inputSettings);
            ServiceLocator<CameraService>.Service.Init();
        }


        private void OnDisable()
        {
            ServiceLocator<InputService>.Service.Disable();
        }
    }
}