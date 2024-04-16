using Spyro;
using Spyro.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static ProjectJetSetRadio.Gameplay.SkateControllerSettings;

namespace ProjectJetSetRadio.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class SkateController : MonoBehaviour
    {
        [Recursive]
        public SkateControllerSettings settings;


        private Rigidbody body;
        private InputService input;
        private MeshRenderer meshRenderer;

        private StateMachine<SkateState> skateState;

        private bool isSkating;
        private Vector2 inputDir;
        private bool isGrounded;
        private Collider[] allocatedGroundColliderArray;


        public float CurrentSpeed =>
            (skateState.CurrentState) switch
            {
                SkateState.Idle => 0,
                SkateState.Moving => isSkating ? settings.skateMovementSpeed : settings.baseMovementSpeed,
                SkateState.Boosting => settings.boostMovementSpeed,
                _ => 0
            };
        public Vector3 LocalInputDirection
        {
            get
            {
                var cam = ServiceLocator<CameraService>.Service.MainCam;
                var result = cam.transform.right * inputDir.x + cam.transform.forward * inputDir.y;
                result.y = 0;
                return result;
            }
        }

        private void Start()
        {
            CommandSystem.AddCommand("respawn", "Respawns the player to the world origin", RespawnPlayer);


            allocatedGroundColliderArray = new Collider[1];
            body = GetComponent<Rigidbody>();
            meshRenderer = GetComponentInChildren<MeshRenderer>();
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
               {SkateState.Idle, OnIdle },
               {SkateState.Moving, OnMove },
               {SkateState.Boosting, OnBoosting }
           };

            return states;
        }

        private IEnumerator OnBoosting(StateMachine<SkateState> machine)
        {
            if (!input.GetButton("Boost"))
            {
                machine.SetNextState(SkateState.Moving);
                SetDebugMat(SkateState.Moving);
                yield break;
            }

            if (inputDir == Vector2.zero)
            {
                machine.SetNextState(SkateState.Idle);
                SetDebugMat(SkateState.Idle);
            }

            yield return null;
        }


        private IEnumerator OnMove(StateMachine<SkateState> machine)
        {
            if (isSkating && input.GetButton("Boost"))
            {
                machine.SetNextState(SkateState.Boosting);
                SetDebugMat(SkateState.Boosting);
            }

            if (inputDir == Vector2.zero)
            {
                machine.SetNextState(SkateState.Idle);
                SetDebugMat(SkateState.Idle);
                yield break;
            }


            yield return null;
        }

        private IEnumerator OnIdle(StateMachine<SkateState> machine)
        {
            if (inputDir != Vector2.zero)
            {
                machine.SetNextState(SkateState.Moving);
                SetDebugMat(SkateState.Moving);
            }

            yield return null;
        }

        private void SetDebugMat(SkateState nextState)
        {
            var mat = meshRenderer.sharedMaterial;

            switch (nextState)
            {
                case SkateState.Idle:
                    mat.color = Color.white;
                    break;
                case SkateState.Moving:
                    mat.color = isSkating ? Color.cyan : Color.green;
                    break;
                case SkateState.Boosting:
                    mat.color = Color.yellow;
                    break;
                default:
                    mat.color = Color.magenta;
                    break;
            }
        }

        private void Update()
        {
            FetchInput();
        }

        private void FixedUpdate()
        {
            CheckGround();
            UpdateBody();
        }

        private void UpdateBody()
        {
            HandleHorizontalVelocity();
            HandleVerticalVelocity();
        }

        private void HandleHorizontalVelocity()
        {
            body.drag = isGrounded ? settings.movementFriction : 0;
            body.velocity += LocalInputDirection * CurrentSpeed;
            var clampedVelocity = Vector3.ClampMagnitude(body.velocity, CurrentSpeed);
            body.velocity = new Vector3(clampedVelocity.x, body.velocity.y, clampedVelocity.z);
        }

        private void HandleVerticalVelocity()
        {
            if (isGrounded && Input.GetButton("Jump"))
            {
                body.velocity += Vector3.up * Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * settings.jumpHeight);
            }

            if (body.velocity.y < 0)
            {
                body.velocity += Vector3.up * Physics.gravity.y * (settings.fallMultiplier - 1.0f) * Time.fixedDeltaTime;
            }
            else if (body.velocity.y > 0 && !input.GetButton("Jump"))
            {
                body.velocity += Vector3.up * Physics.gravity.y * (settings.lowJumpMultiplier - 1.0f) * Time.fixedDeltaTime;
            }
        }

        private void CheckGround()
        {
            var results = Physics.OverlapBoxNonAlloc(transform.position + settings.groundCollider.center, settings.groundCollider.extents, allocatedGroundColliderArray, Quaternion.identity, settings.groundCollisionMask);
            isGrounded = results > 0;
        }

        private void FetchInput()
        {
            isSkating = input.GetButtonDown("Skate") ? !isSkating : isSkating;
            inputDir = input.GetAxis("Move");
        }



        private void OnDrawGizmos()
        {
            Gizmos.color = isGrounded ? Color.red : Color.green;
            Gizmos.DrawCube(transform.position + settings.groundCollider.center, settings.groundCollider.size);

        }
    }

}
