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
        private Collider bodyCollider;

        private bool isSkating;
        private RailController currentRail;

        public Bounds Hitbox
            => controller.Hitbox;

        private void Start()
        {
            CommandSystem.AddCommand("respawn", "Respawns the player to the world origin", RespawnPlayer);

            controller = GetComponent<BasicPlatformerController>();
            input = ServiceLocator<InputService>.Service;
            bodyCollider = GetComponentInChildren<Collider>();

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
            currentRail.UpdateController(this);

            var jumpInput = input.GetButton("Jump");

            if (!jumpInput)
            {
                var grindSpeed = input.GetButton("Boost") ? settings.boostGrindSpeed : settings.skateGrindSpeed;

                transform.position = currentRail.NearestPointOnRail + (Vector3.up * bodyCollider.bounds.extents.y);

                var dot = Vector3.Dot(controller.Body.velocity.normalized, currentRail.ForwardRailDirection);
                var forwardDir = dot > 0 ? currentRail.ForwardRailDirection : -currentRail.ForwardRailDirection;

                controller.Body.velocity = forwardDir * grindSpeed;
            }

            if (jumpInput || !RailService.Instance.TryGetClosestIntersectingRail(this, out _))
            {
                controller.GroundCheckOverride = false;
                controller.SetHorizontalMovement(true);
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
                controller.GroundCheckOverride = true;
                controller.SetHorizontalMovement(false);
                machine.SetNextState(SkateState.Grinding);
                Debug.Log("Going to grinding state");
            }

            yield return null;
        }

        private bool HasLandedOnARail()
        {
            return
                !controller.IsGrounded &&
                controller.Body.velocity.y < 0 &&
                RailService.Instance.TryGetClosestIntersectingRail(this, out currentRail);
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
