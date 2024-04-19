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

        public Transform modelObject;


        private InputService input;
        private BasicPlatformerController controller;
        private StateMachine<PlayerState> playerState;
        private Collider bodyCollider;

        private StateMachine<SubPlayerState> subPlayerState;
        private RailController currentRail;

        public Bounds Hitbox
            => controller.Hitbox;

        public PlayerState CurrentState
            => playerState.CurrentState;

        public SubPlayerState CurrentSubState
            => subPlayerState.CurrentState;

        public Rigidbody Body
            => controller.Body;

        public bool IsGrounded
            => controller.IsGrounded;

        private void Start()
        {
            CommandSystem.AddCommand("respawn", "Respawns the player to the world origin", RespawnPlayer);
            PlayerService.Instance.RegisterPlayer(this);

            controller = GetComponent<BasicPlatformerController>();
            input = ServiceLocator<InputService>.Service;
            bodyCollider = GetComponentInChildren<Collider>();

            playerState = new StateMachine<PlayerState>(PlayerState.Idle, InitPlayerStates());
            subPlayerState = new StateMachine<SubPlayerState>(SubPlayerState.Walking, InitSubPlayerStates());


            StartCoroutine(EnumeratorUpdate());


            ServiceLocator<CameraService>.Service.SetMouseCursorState(false);

        }

        private Dictionary<SubPlayerState, Func<StateMachine<SubPlayerState>, IEnumerator>> InitSubPlayerStates()
        {
            var states = new Dictionary<SubPlayerState, Func<StateMachine<SubPlayerState>, IEnumerator>>()
           {
               {SubPlayerState.Walking, HandleWalking },
               {SubPlayerState.Skating, HandleSkating },
               {SubPlayerState.Boosting, HandleBoosting }
           };

            return states;
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
                yield return playerState.Update();
                yield return subPlayerState.Update();
            }
        }

        private Dictionary<PlayerState, Func<StateMachine<PlayerState>, IEnumerator>> InitPlayerStates()
        {
            var states = new Dictionary<PlayerState, Func<StateMachine<PlayerState>, IEnumerator>>()
           {
               {PlayerState.Idle, HandleIdle },
               {PlayerState.Moving, HandleMovement },
               {PlayerState.WallRunning, HandleWallRunning},
               {PlayerState.Grinding, HandleGrindingOnRail }
           };

            return states;
        }

        private IEnumerator HandleGrindingOnRail(StateMachine<PlayerState> machine)
        {
            currentRail.UpdateController(this);

            var jumpInput = input.GetButton("Jump");

            if (!jumpInput)
            {
                var grindSpeed = input.GetButton("Boost") ? settings.boostGrindSpeed : settings.skateGrindSpeed;

                transform.position = currentRail.NearestPointOnRail + (currentRail.RailNormal * bodyCollider.bounds.extents.y);

                var dot = Vector3.Dot(controller.Body.velocity.normalized, currentRail.ForwardRailDirection);
                var forwardDir = dot > 0 ? currentRail.ForwardRailDirection : -currentRail.ForwardRailDirection;

                controller.Body.velocity = forwardDir * grindSpeed;
            }

            if (jumpInput || !RailService.Instance.TryGetClosestIntersectingRail(this, out _))
            {
                controller.GroundCheckOverride = false;
                controller.SetHorizontalMovement(true);
                var nextState = controller.Body.velocity.magnitude > 0.001f ? PlayerState.Moving : PlayerState.Idle;
                machine.SetNextState(nextState);
                Debug.Log($"Going to {(controller.Body.velocity.magnitude > 0.001f ? "moving" : "idle")} state");
            }


            yield return new WaitForFixedUpdate();
        }

        private IEnumerator HandleWallRunning(StateMachine<PlayerState> machine)
        {
            yield return null;
        }


        private IEnumerator HandleMovement(StateMachine<PlayerState> machine)
        {
            if (controller.Body.velocity.magnitude < 0.001f)
            {
                machine.SetNextState(PlayerState.Idle);
                Debug.Log("Going to idle state");
            }

            if (HasLandedOnARail() && subPlayerState.CurrentState != SubPlayerState.Walking)
            {
                controller.GroundCheckOverride = true;
                controller.SetHorizontalMovement(false);
                machine.SetNextState(PlayerState.Grinding);
                Debug.Log("Going to grinding state");
            }

            yield return null;
        }


        private bool CanSkate()
        {
            var isNotGrinding = playerState.CurrentState != PlayerState.Grinding;
            var isGrounded = controller.IsGrounded;
            var isNotMoving = controller.Body.velocity.magnitude < 0.1f;

            return isNotGrinding && isGrounded && isNotMoving;

        }

        private IEnumerator HandleBoosting(StateMachine<SubPlayerState> machine)
        {
            var boost = input.GetButton("Boost");
            var isGrinding = playerState.CurrentState == PlayerState.Grinding;

            if (!boost)
            {
                controller.speed = isGrinding ? settings.skateGrindSpeed : settings.skateMovementSpeed;
                machine.SetNextState(SubPlayerState.Skating);
            }

            yield return null;
        }

        private IEnumerator HandleSkating(StateMachine<SubPlayerState> machine)
        {
            var toggle = input.GetButtonDown("Skate");

            if (toggle && CanSkate())
            {
                controller.speed = settings.baseMovementSpeed;
                controller.groundFriction = 5.0f;
                machine.SetNextState(SubPlayerState.Walking);
                yield break;
            }

            var boost = input.GetButton("Boost");
            var isGrinding = playerState.CurrentState == PlayerState.Grinding;

            if (boost)
            {
                controller.speed = isGrinding ? settings.boostGrindSpeed : settings.boostMovementSpeed;
                machine.SetNextState(SubPlayerState.Boosting);
            }

            yield return null;
        }

        private IEnumerator HandleWalking(StateMachine<SubPlayerState> machine)
        {
            var toggle = input.GetButtonDown("Skate");

            if (toggle && CanSkate())
            {

                controller.speed = settings.skateMovementSpeed;
                controller.groundFriction = 3.0f;
                machine.SetNextState(SubPlayerState.Skating);

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

        private IEnumerator HandleIdle(StateMachine<PlayerState> machine)
        {
            if (controller.Body.velocity.magnitude > 0.0001f)
            {
                machine.SetNextState(PlayerState.Moving);
                Debug.Log("Going to moving state");
            }

            yield return null;
        }

    }

}
