using ProjectJetSetRadio.Gameplay;
using Spyro;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectJetSetRadio
{

    public class App : MonoBehaviour
    {
        public InputActionAsset inputSettings;
        [Header("Rail Settings")]
        public LayerMask railMask;

        private void Awake()
        {
            InputService.Instance.Init(inputSettings);
            CameraService.Instance.Init();
         
        }

        private void Update()
        {
        }

        private void OnDisable()
        {
            InputService.Instance.Disable();
        }
    }
}