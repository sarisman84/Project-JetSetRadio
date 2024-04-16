using Spyro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectJetSetRadio.Gameplay
{
    public class InputService
    {
        private InputActionAsset inputSettings;

        private Dictionary<string, InputAction> inputRegistry;

        public void Init(InputActionAsset _inputSettings)
        {
            inputRegistry = new Dictionary<string, InputAction>();

            inputSettings = _inputSettings;

            foreach (var actionMap in inputSettings.actionMaps)
            {
                foreach (var action in actionMap.actions)
                {
                    inputRegistry.Add(action.name, action);
                }
            }

            Enable();

        }


        public Vector2 GetAxis(string name)
        {
            if (inputRegistry.ContainsKey(name.ToLower()))
                return inputRegistry[name.ToLower()].ReadValue<Vector2>();

            return default;
        }


        public bool GetButton(string name)
        {
            var key = name.ToLower();
            if (inputRegistry.ContainsKey(key))
                return inputRegistry[key].ReadValue<float>() > 0;

            return false;
        }

        public bool GetButtonDown(string name)
        {
            var key = name.ToLower();
            if (inputRegistry.ContainsKey(key))
            {
                var action = inputRegistry[key];
                return action.ReadValue<float>() > 0 && action.triggered;
            }

            return false;
        }


        public bool GetButtonUp(string name)
        {
            var key = name.ToLower();
            if (inputRegistry.ContainsKey(key))
            {
                var action = inputRegistry[key];
                return action.ReadValue<float>() < 1;
            }

            return false;
        }


        public void Enable()
        {
            inputSettings.Enable();
        }

        public void Disable()
        {
            inputSettings.Disable();
        }
    }
}