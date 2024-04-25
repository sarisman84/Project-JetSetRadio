using Spyro;
using System.Collections;
using UnityEngine;

namespace ProjectJetSetRadio.Gameplay
{
    [CreateAssetMenu(fileName = "New Skate Controller Settings", menuName = "Gameplay/Skate Controller Settings", order = 0)]
    public class SkateControllerSettings : ScriptableObject
    {
        public enum PlayerState
        {
            Idle,
            Moving,
            Grinding,
            WallRunning,
        }

        public enum SubPlayerState
        {
            Walking,
            Skating,
            Boosting
        }

        [Header("Horizonal Movement")]
        [Header("General")]
        public float baseMovementSpeed;
        public float skateMovementSpeed;
        public float boostMovementSpeed;

        [Header("Smoothing")]
        public float movementFriction;

        [Header("Grind Movement")]
        [Header("General")]
        public float skateGrindSpeed;
        public float boostGrindSpeed;
        public float grindDirectionThreshold = .25f;
        public float railDetectionRangeFromPlayer = 5.0f;

        [Header("Wall Running")]
        [Header("General")]
        public float baseWallRunSpeed;
        public float boostWallRunSpeed;

        [Header("Wall Detection")]
        public LayerMask wallRunCollisionMask;
        public float wallRunDetectionRange = 5.0f;


        [Header("Vertical Movement")]
        [Header("Jump Settings")]
        public float jumpHeight = 1.0f;
        public float fallMultiplier = 2.5f;
        public float lowJumpMultiplier = 2f;

        [Header("Ground Collision")]
        public Bounds groundCollider;
        public LayerMask groundCollisionMask;


    }
}