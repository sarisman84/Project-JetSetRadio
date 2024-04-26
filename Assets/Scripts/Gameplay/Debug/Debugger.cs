using Spyro.Debug;
using Spyro.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectJetSetRadio.Gameplay.CustomDebug
{
    public struct DebugDesc
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
        public Color color;
    }

    [RequireComponent(typeof(UIDocument))]
    public class Debugger : MonoBehaviour
    {
        enum DebugObjectType
        {
            Box,
            Sphere
        }

        private UIDocument document;
        private string currentInput;
        private bool hudState;

        private Dictionary<string, VisualElement> registeredElements = new Dictionary<string, VisualElement>();
        private Dictionary<string, string> customData = new Dictionary<string, string>();

        private Dictionary<string, List<Action>> debugActions = new Dictionary<string, List<Action>>();
        private int currentAction;
        public bool HudState
            => hudState;

        public void SetCustomData(string key, string value)
        {
            if (!customData.ContainsKey(key))
            {
                customData.Add(key, value);
                return;
            }

            customData[key] = value;
        }

        public void DrawCube(string id, DebugDesc desc)
        {
            if (!debugActions.ContainsKey(id))
            {
                InitDebugObjectRegistry(id);
            }

            debugActions[id].Add(() =>
            {
                Gizmos.color = desc.color;
                Gizmos.matrix = Matrix4x4.TRS(desc.position, desc.rotation, desc.scale);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = Matrix4x4.identity;
            });
        }

        private void InitDebugObjectRegistry(string id)
        {
            debugActions.Add(id, new List<Action>(1000));
        }

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


            foreach (var (id, element) in registeredElements)
            {
                if (id.Contains("player"))
                    container.Add(element);
            }


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

            label = registeredElements["player_input"] as Label;
            if (customData.ContainsKey("player_input"))
                label.text = $"Last Input: {customData["player_input"]}";


        }


        private void InitElements()
        {
            AddTextElement("player_state");
            AddTextElement("player_velocity");
            AddTextElement("player_sub_state");
            AddTextElement("player_input");
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

            DrawPlayerMovementAxis(player);

            foreach (var action in debugActions["player"])
            {
                action();
            }

            debugActions["player"].Clear();

        }

        private void DrawPlayerMovementAxis(SkateController player)
        {
            var ogMatrix = Gizmos.matrix;

            var velocity = player.Body.velocity;

            if (velocity.magnitude > 0.1f)
            {
                Gizmos.matrix = Matrix4x4.TRS(player.transform.position, Quaternion.LookRotation(velocity.normalized), new Vector3(0.15f, 0.15f, 3.0f));
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);

                var upDir = Vector3.Cross(velocity.normalized, player.transform.right.normalized);

                Gizmos.color = Color.green;
                Gizmos.matrix = Matrix4x4.TRS(player.transform.position, Quaternion.LookRotation(upDir), new Vector3(0.15f, 0.15f, 3.0f));
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            Gizmos.matrix = ogMatrix;
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
            Gizmos.color -= new Color(0, 0, 0, 0.5f);
            Gizmos.DrawCube(player.transform.position, player.Hitbox.size);
        }
    }
}

