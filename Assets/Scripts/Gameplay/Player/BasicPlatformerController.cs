using System.Collections;
using UnityEngine;

namespace ProjectJetSetRadio.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class BasicPlatformerController : MonoBehaviour
    {
        private Rigidbody body;
        private InputService input;
        private Vector3 rawInputDir;
        private Collider[] allocGroundColliderArray = new Collider[5];
        private bool enableHorizontalMovement = true;
        private bool enableVerticalMovement = true;
        private Collider bodyCollider;

        [Header("Horizontal Movement Settings")]
        public float speed;
        public float groundFriction;

        [Header("Ground Check Settings")]
        public Bounds groundCollisionBounds;
        public LayerMask groundLayerMask;

        [Header("Vertical Movement Settings")]
        public float jumpHeight;
        public float fallMultiplier;
        public float lowJumpMultiplier;

        public bool IsGrounded { get; private set; }
        public bool GroundCheckOverride { get; set; }

        public Collider[] GroundCollisionResults
            => allocGroundColliderArray;

        public Rigidbody Body
            => body;

        public Vector3 InputDirection
        {
            get
            {
                var cam = CameraService.Instance.MainCam;
                var result = cam.transform.right * rawInputDir.x + cam.transform.forward * rawInputDir.y;
                result.y = 0;
                return result;
            }
        }

        public Bounds Hitbox
            => bodyCollider.bounds;

        public void SetHorizontalMovement(bool newState)
            => enableHorizontalMovement = newState;
        public void SetVerticalMovement(bool newState)
            => enableVerticalMovement = newState;


        private void Awake()
        {
            input = InputService.Instance;
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponentInChildren<Collider>();

        }

        private void Update()
        {
            rawInputDir = input.GetAxis("Move");
        }


        private void FixedUpdate()
        {
            CheckGround();
            if (enableHorizontalMovement)
                HandleHorizontalVelocity();
            if (enableVerticalMovement)
                HandleVerticalVelocity();
        }


        private void HandleHorizontalVelocity()
        {
            body.drag = IsGrounded ? groundFriction : 0;
            body.velocity += InputDirection * speed;
            var clampedVelocity = Vector3.ClampMagnitude(body.velocity, speed);
            body.velocity = new Vector3(clampedVelocity.x, body.velocity.y, clampedVelocity.z);
        }

        private void HandleVerticalVelocity()
        {
            if (IsGrounded && Input.GetButton("Jump"))
            {
                body.velocity += Vector3.up * Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            }

            if (body.velocity.y < 0)
            {
                body.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1.0f) * Time.fixedDeltaTime;
            }
            else if (body.velocity.y > 0 && !input.GetButton("Jump"))
            {
                body.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1.0f) * Time.fixedDeltaTime;
            }
        }

        private void CheckGround()
        {
            var results = Physics.OverlapBoxNonAlloc(transform.position + groundCollisionBounds.center, groundCollisionBounds.extents, allocGroundColliderArray, Quaternion.identity, groundLayerMask);
            IsGrounded = results > 0 || GroundCheckOverride;
        }
    }
}