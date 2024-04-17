using Spyro;
using Spyro.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using static ProjectJetSetRadio.Gameplay.SkateControllerSettings;

namespace ProjectJetSetRadio.Gameplay
{
    [RequireComponent(typeof(BasicPlatformerController))]
    public class SkateController : MonoBehaviour
    {
        [Recursive]
        public SkateControllerSettings settings;


        private InputService input;
        private BasicPlatformerController controller;

        private StateMachine<SkateState> skateState;

        private bool isSkating;
        private RailController currentRail;

        private void Start()
        {
            CommandSystem.AddCommand("respawn", "Respawns the player to the world origin", RespawnPlayer);

            controller = GetComponent<BasicPlatformerController>();
            input = ServiceLocator<InputService>.Service;

            skateState = new StateMachine<SkateState>(SkateState.Idle, InitStates());

            StartCoroutine(EnumeratorUpdate());


            ServiceLocator<CameraService>.Service.SetMouseCursorState(false);
        }

        private bool RespawnPlayer(object[] arg)
        {
            transform.position = Vector3.zero;
            Debug.Log("Respawned player to world origin!");
            return true;
        }

        private IEnumerator EnumeratorUpdate()
        {
            while (true)
            {
                yield return skateState.Update();
            }
        }

        private Dictionary<SkateState, Func<StateMachine<SkateState>, IEnumerator>> InitStates()
        {
            var states = new Dictionary<SkateState, Func<StateMachine<SkateState>, IEnumerator>>()
           {
               {SkateState.Idle, HandleIdle },
               {SkateState.Moving, HandleMovement },
               {SkateState.WallRunning, HandleWallRunning},
               {SkateState.Grinding, HandleGrindingOnRail }
           };

            return states;
        }

        private IEnumerator HandleGrindingOnRail(StateMachine<SkateState> machine)
        {



            if (!HasLandedOnARail())
            {
                var nextState = controller.Body.velocity.magnitude > 0.001f ? SkateState.Moving : SkateState.Idle;
                machine.SetNextState(nextState);
                Debug.Log($"Going to {(controller.Body.velocity.magnitude > 0.001f ? "moving" : "idle")} state");
            }


            yield return new WaitForFixedUpdate();
        }

        private IEnumerator HandleWallRunning(StateMachine<SkateState> machine)
        {
            yield return null;
        }


        private IEnumerator HandleMovement(StateMachine<SkateState> machine)
        {
            var skateSpeed = input.GetButton("Boost") ? settings.boostMovementSpeed : settings.skateMovementSpeed;
            controller.speed = isSkating ? skateSpeed : settings.baseMovementSpeed;


            if (controller.Body.velocity.magnitude < 0.001f)
            {
                machine.SetNextState(SkateState.Idle);
                Debug.Log("Going to idle state");
            }

            if (HasLandedOnARail())
            {
                //controller.SetHorizontalMovement(false);
                machine.SetNextState(SkateState.Grinding);
                Debug.Log("Going to grinding state");
            }

            yield return null;
        }

        private bool HasLandedOnARail()
        {
            if (!controller.IsGrounded)
                return false;

            return RailService.Instance.TryLandingOnRail(this, out currentRail);
        }

        private IEnumerator HandleIdle(StateMachine<SkateState> machine)
        {
            if (controller.Body.velocity.magnitude > 0.0001f)
            {
                machine.SetNextState(SkateState.Moving);
                Debug.Log("Going to moving state");
            }

            yield return null;
        }


    }

}
