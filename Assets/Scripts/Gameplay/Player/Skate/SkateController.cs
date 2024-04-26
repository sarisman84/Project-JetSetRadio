using ProjectJetSetRadio.Gameplay.CustomDebug;
using ProjectJetSetRadio.Gameplay.Visual;
using Spyro;
using Spyro.Debug;
using System;
using System.Collections;
using UnityEngine;
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
        private StateMachine<PlayerState> playerState;
        private Collider bodyCollider;

        private StateMachine<SubPlayerState> subPlayerState;
        private RailController currentRail;
        private CharacterRotationController model;

        private RaycastHit leftDetection, rightDetection;

        public float CurrentWallRunDurationInSeconds { get; private set; }
        public bool HasDetectedLeftWall { get; private set; }
        public bool HasDetectedRightWall { get; private set; }
        public bool HasDetectedAWall
            => HasDetectedLeftWall || HasDetectedRightWall;

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

        public bool IsSkating
        { get; private set; }

        private void Start()
        {
            CommandSystem.AddCommand("respawn", "Respawns the player to the world origin", RespawnPlayer);

            PlayerService.Instance.RegisterPlayer(this);

            controller = GetComponent<BasicPlatformerController>();
            input = ServiceLocator<InputService>.Service;
            bodyCollider = GetComponentInChildren<Collider>();
            model = GetComponentInChildren<CharacterRotationController>();
            leftDetection = rightDetection = new RaycastHit();

            playerState = new StateMachine<PlayerState>(PlayerState.Idle);
            subPlayerState = new StateMachine<SubPlayerState>(SubPlayerState.Walking);

            InitPlayerStates();
            InitSubPlayerStates();
            InitSkateInput();

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
                yield return playerState.Update();
                yield return subPlayerState.Update();
            }
        }
        private void InitSkateInput()
        {
            var skateToggle = input.Get("Skate");
            if (skateToggle == default) return;

            skateToggle.performed += (c) =>
            {
                IsSkating = !IsSkating;
                DebugService.SetCustomData("player_input", c.action.name);
            };
        }

        private void InitSubPlayerStates()
        {
            subPlayerState.SetState(SubPlayerState.Walking, HandleWalking);
            subPlayerState.SetState(SubPlayerState.Skating, HandleSkating, OnSkateExit, OnSkateEnter);
            subPlayerState.SetState(SubPlayerState.Boosting, HandleBoosting, OnBoostEnter, OnBoostExit);

        }



        private void InitPlayerStates()
        {
            playerState.SetState(PlayerState.Idle, HandleIdle);
            playerState.SetState(PlayerState.Moving, HandleMovement);
            playerState.SetState(PlayerState.Grinding, HandleGrindingOnRail, OnGrindingEnter, OnGrindingExit);
            playerState.SetState(PlayerState.WallRunning, HandleWallRunning, OnWallRunEnter, OnWallRunExit);
        }

        private IEnumerator OnWallRunExit(StateMachine<PlayerState> machine)
        {
            controller.SetHorizontalMovement(true);
            controller.SetVerticalMovement(true);
            controller.SetGravity(true);

            yield return null;
        }

        private IEnumerator OnWallRunEnter(StateMachine<PlayerState> machine)
        {
            CurrentWallRunDurationInSeconds = settings.wallRunDurationInSeconds;

            controller.SetHorizontalMovement(false);
            controller.SetVerticalMovement(false);
            controller.SetGravity(false);

            yield return null;
        }



        private IEnumerator OnSkateEnter(StateMachine<SubPlayerState> machine)
        {
            controller.speed = settings.skateMovementSpeed;
            controller.groundFriction = 3.0f;

            yield return null;
        }

        private IEnumerator OnSkateExit(StateMachine<SubPlayerState> machine)
        {
            controller.speed = settings.baseMovementSpeed;
            controller.groundFriction = 5.0f;

            yield return null;
        }

        private IEnumerator OnBoostEnter(StateMachine<SubPlayerState> machine)
        {
            var isGrinding = playerState.CurrentState == PlayerState.Grinding;
            var isWallRunning = playerState.CurrentState == PlayerState.WallRunning;
            controller.speed =
                isGrinding ? settings.boostGrindSpeed :
                isWallRunning ? settings.boostWallRunSpeed :
                settings.boostMovementSpeed;

            yield return null;
        }

        private IEnumerator OnBoostExit(StateMachine<SubPlayerState> machine)
        {
            var isGrinding = playerState.CurrentState == PlayerState.Grinding;
            var isWallRunning = playerState.CurrentState == PlayerState.WallRunning;
            controller.speed =
                isGrinding ? settings.skateGrindSpeed :
                isWallRunning ? settings.baseWallRunSpeed :
                settings.skateMovementSpeed;


            yield return null;
        }

        private IEnumerator OnGrindingExit(StateMachine<PlayerState> machine)
        {
            controller.GroundCheckOverride = false;
            controller.SetHorizontalMovement(true);
            controller.SetGravity(true);

            yield return null;
        }

        private IEnumerator OnGrindingEnter(StateMachine<PlayerState> machine)
        {
            controller.GroundCheckOverride = true;
            controller.SetHorizontalMovement(false);
            controller.SetGravity(false);

            yield return null;
        }

        private IEnumerator HandleWallRunning(StateMachine<PlayerState> machine)
        {
            var jumpInput = input.GetButton("Jump");

            var hit = HasDetectedLeftWall ? leftDetection : rightDetection;
            var localForwardDir = Vector3.Cross(transform.up.normalized, hit.normal.normalized);
            var finalVelocity = controller.Body.velocity;
            if (!jumpInput)
            {
                var dot = Vector3.Dot(controller.Body.velocity.normalized, localForwardDir);
                var forwardDir = dot > 0 ? localForwardDir : -localForwardDir;

                var desc = new DebugDesc()
                {
                    position = transform.position + Vector3.one * 0.15f,
                    scale = new Vector3(0.05f, 0.05f, controller.Body.velocity.magnitude),
                    rotation = Quaternion.LookRotation(forwardDir),
                    color = Color.cyan
                };

                DebugService.DrawCube("player", desc);

                finalVelocity += forwardDir * controller.speed;
                finalVelocity = Vector3.ClampMagnitude(finalVelocity, controller.speed);
                finalVelocity += Vector3.up * Physics.gravity.y * (settings.fallMultiplier - 1.0f) * Time.fixedDeltaTime;
            }
            else
            {
                finalVelocity += BasicPlatformerController.CalculateJumpForce(hit.normal.normalized, settings.wallRunJumpHeight);
            }

            controller.Body.velocity = finalVelocity;


            if (!HasDetectedAWall || IsGrounded || CurrentWallRunDurationInSeconds <= 0)
            {
                var nextState = controller.Body.velocity.magnitude > 0.001f ? PlayerState.Moving : PlayerState.Idle;
                yield return machine.SetNextState(nextState);
            }

            CurrentWallRunDurationInSeconds -= Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        private IEnumerator HandleGrindingOnRail(StateMachine<PlayerState> machine)
        {
            currentRail.UpdateController(this);

            var jumpInput = input.GetButton("Jump");

            if (!jumpInput)
            {
                transform.position = currentRail.NearestPointOnRail + (currentRail.RailNormal * bodyCollider.bounds.extents.y);

                var dot = Vector3.Dot(controller.Body.velocity.normalized, currentRail.ForwardRailDirection);
                var forwardDir = dot > 0 ? currentRail.ForwardRailDirection : -currentRail.ForwardRailDirection;

                controller.Body.velocity = forwardDir * controller.speed;
                model.AlignToDirection(forwardDir, currentRail.RailNormal);
            }

            if (jumpInput || !RailService.Instance.TryGetClosestIntersectingRail(this, out _))
            {

                var nextState = controller.Body.velocity.magnitude > 0.001f ? PlayerState.Moving : PlayerState.Idle;
                yield return machine.SetNextState(nextState);
            }


            yield return new WaitForFixedUpdate();
        }


        private IEnumerator HandleMovement(StateMachine<PlayerState> machine)
        {
            var dir = controller.Body.velocity.normalized;
            dir.y = 0;
            model.AlignToDirection(dir);

            var isNotWalking = subPlayerState.CurrentState != SubPlayerState.Walking;
            var isNotWallRunning = playerState.CurrentState != PlayerState.WallRunning;

            if (HasDetectedAWall && !IsGrounded && isNotWalking && CurrentWallRunDurationInSeconds > 0)
            {
                yield return playerState.SetNextState(PlayerState.WallRunning);
            }



            if (controller.Body.velocity.magnitude < 0.001f && isNotWallRunning)
            {
                yield return machine.SetNextState(PlayerState.Idle);
                Debug.Log("Going to idle state");
            }


            if (HasLandedOnARail() && isNotWalking && isNotWallRunning)
            {

                yield return machine.SetNextState(PlayerState.Grinding);
                Debug.Log("Going to grinding state");
            }

            if (IsGrounded)
            {
                CurrentWallRunDurationInSeconds = settings.wallRunDurationInSeconds;
            }

            yield return null;
        }


        private bool CanSkate()
        {
            var isNotGrinding = playerState.CurrentState != PlayerState.Grinding;
            var isGrounded = controller.IsGrounded;


            return isNotGrinding && isGrounded;

        }

        private IEnumerator HandleBoosting(StateMachine<SubPlayerState> machine)
        {
            var boost = input.GetButton("Boost");


            if (!boost)
            {

                yield return machine.SetNextState(SubPlayerState.Skating);
            }

            yield return null;
        }

        private IEnumerator HandleSkating(StateMachine<SubPlayerState> machine)
        {
            if (!IsSkating && CanSkate())
            {
                yield return machine.SetNextState(SubPlayerState.Walking);
                yield break;
            }

            var boost = input.GetButton("Boost");

            if (boost)
            {

                yield return machine.SetNextState(SubPlayerState.Boosting);
            }

            yield return null;
        }

        private IEnumerator HandleWalking(StateMachine<SubPlayerState> machine)
        {
            if (IsSkating && CanSkate())
            {
                yield return machine.SetNextState(SubPlayerState.Skating);
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
                yield return machine.SetNextState(PlayerState.Moving);
            }

            yield return null;
        }


        private void Update()
        {
            var desc = new DebugDesc()
            {
                position = transform.position,
                scale = new Vector3(settings.wallRunDetectionRange * 2.0f, 0.15f, 0.15f),
                rotation = Quaternion.LookRotation(controller.Body.velocity.normalized),
                color = Color.red
            };

            DebugService.DrawCube("player", desc);
        }

        private void FixedUpdate()
        {
            HandleDetectingWalls();
        }

        private void HandleDetectingWalls()
        {
            var localRight = Vector3.Cross(controller.Body.velocity.normalized, transform.up.normalized);
            var localLeft = -localRight;
            HasDetectedRightWall = Physics.Raycast(transform.position, localRight, out rightDetection, settings.wallRunDetectionRange, settings.wallRunCollisionMask);
            if (!HasDetectedRightWall)
                HasDetectedLeftWall = Physics.Raycast(transform.position, localLeft, out leftDetection, settings.wallRunDetectionRange, settings.wallRunCollisionMask);
        }
    }

}
