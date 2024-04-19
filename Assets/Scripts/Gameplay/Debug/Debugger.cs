using Spyro.Debug;
using Spyro.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectJetSetRadio.Gameplay.CustomDebug
{
    [RequireComponent(typeof(UIDocument))]
    public class Debugger : MonoBehaviour
    {
        private UIDocument document;
        private string currentInput;
        private bool hudState;

        private Dictionary<string, VisualElement> registeredElements = new Dictionary<string, VisualElement>();

        public bool HudState
            => hudState;

        private void Awake()
        {
            DebugService.Instance.RegisterDebugger(this);
            document = GetComponent<UIDocument>();
            CommandSystem.AddCommand("show_debug", "Toggles a display given an argument", OnToggleDebug, new Arg("player"));
            InitElements();
            SetDocumentActive(false);

        }

        private bool OnToggleDebug(object[] arg)
        {
            if (arg.Length == 0 || arg == null || arg[0] is not string)
            {
                return false;
            }
            var type = arg[0] as string;

            if (currentInput == type)
            {
                ToggleDocument();
                return true;
            }
            currentInput = type;
            SetDocumentActive(true);
            switch (type)
            {
                case "player":
                    DisplayPlayerInfo();
                    break;
            }

            return true;
        }

        private void ToggleDocument()
        {
            SetDocumentActive(!hudState);
        }

        private void SetDocumentActive(bool active)
        {
            hudState = active;
            document.rootVisualElement.style.display = hudState ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void DisplayPlayerInfo()
        {
            var root = document.rootVisualElement;
            root.Clear();

            var container = new VisualElement();
            container.style.width = 300.0f;
            container.style.height = Length.Auto();
            container.style.backgroundColor = new Color(0, 0, 0, 0.25f);


            container.Add(registeredElements["player_state"]);
            container.Add(registeredElements["player_sub_state"]);
            container.Add(registeredElements["player_velocity"]);


            root.Add(container);
        }


        private void UpdatePlayerInfo()
        {
            var player = PlayerService.Instance.Player;

            var playerState = player.CurrentState;
            var subPlayerState = player.CurrentSubState;

            var label = registeredElements["player_state"] as Label;
            label.text = $"State: {playerState}";

            label = registeredElements["player_sub_state"] as Label;
            label.text = $"Sub State: {subPlayerState}";

            label = registeredElements["player_velocity"] as Label;
            label.text = $"Velocity: {player.Body.velocity}";


        }


        private void InitElements()
        {
            AddTextElement("player_state");
            AddTextElement("player_velocity");
            AddTextElement("player_sub_state");
        }


        private void AddTextElement(string id)
        {
            var textElement = new Label();
            textElement.style.color = Color.white;

            registeredElements.Add(id, textElement);
        }


        private void Update()
        {
            if (!hudState)
                return;

            switch (currentInput)
            {
                case "player":
                    UpdatePlayerInfo();
                    break;
            }
        }


        private void OnDrawGizmos()
        {
            if (!hudState) return;

            switch (currentInput)
            {
                case "player":
                    DrawPlayerDebug();
                    break;
            }
        }

        private void DrawPlayerDebug()
        {
            if (PlayerService.Instance.Player == null)
                return;

            var player = PlayerService.Instance.Player;
            DrawPlayerState(player);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.transform.position, player.settings.railDetectionRangeFromPlayer);

            Gizmos.color = player.IsGrounded ? Color.red : Color.green;
            Gizmos.DrawCube(player.transform.position + player.settings.groundCollider.center, player.settings.groundCollider.size);
        }

        private static void DrawPlayerState(SkateController player)
        {
            switch (player.CurrentState)
            {
                case SkateControllerSettings.PlayerState.Idle:
                    Gizmos.color = Color.clear;
                    break;
                case SkateControllerSettings.PlayerState.Moving:
                    Gizmos.color = Color.cyan;
                    break;
                case SkateControllerSettings.PlayerState.Grinding:
                    Gizmos.color = Color.yellow;
                    break;
                case SkateControllerSettings.PlayerState.WallRunning:
                    Gizmos.color = Color.red;
                    break;
            }
            Gizmos.DrawCube(player.transform.position, player.Hitbox.size);
        }
    }
}

