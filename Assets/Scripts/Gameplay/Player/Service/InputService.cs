using ProjectJetSetRadio.Gameplay.CustomDebug;
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
        public static InputService Instance =>
                ServiceLocator<InputService>.Service;

        private InputActionAsset inputSettings;

        private Dictionary<string, InputAction> inputRegistry;
        private Dictionary<string, bool> releasedInputRegistry;
        private Dictionary<string, bool> pressedInputRegistry;

        public void Init(InputActionAsset _inputSettings)
        {
            inputRegistry = new Dictionary<string, InputAction>();
            releasedInputRegistry = new Dictionary<string, bool>();
            pressedInputRegistry = new Dictionary<string, bool>();

            inputSettings = _inputSettings;

            foreach (var actionMap in inputSettings.actionMaps)
            {
                foreach (var action in actionMap.actions)
                {
                    inputRegistry.Add(action.name, action);
                    releasedInputRegistry.Add(action.name, false);
                    pressedInputRegistry.Add(action.name, false);

                    action.canceled += (c) => { releasedInputRegistry[action.name] = true; };
                    action.started += (c) => { pressedInputRegistry[action.name] = true; };
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
            {
                var result = inputRegistry[key].ReadValue<float>() > 0;
                if (result)
                {
                    DebugService.SetCustomData("player_input", inputRegistry[key].name);
                }
                return result;
            }


            return false;
        }


        public InputAction Get(string name)
        {
            var key = name.ToLower();
            if (inputRegistry.ContainsKey(key))
                return inputRegistry[key];

            return default;
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