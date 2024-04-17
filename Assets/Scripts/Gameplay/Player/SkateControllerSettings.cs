using Spyro;
using System.Collections;
using UnityEngine;

namespace ProjectJetSetRadio.Gameplay
{
    [CreateAssetMenu(fileName = "New Skate Controller Settings", menuName = "Gameplay/Skate Controller Settings", order = 0)]
    public class SkateControllerSettings : ScriptableObject
    {
        public enum SkateState
        {
            Idle,
            Moving,
            Grinding,
            WallRunning,
        }
        [Header("Horizonal Movement")]
        [Header("Speed")]
        public float baseMovementSpeed;
        public float skateMovementSpeed;
        public float boostMovementSpeed;

        [Header("Smoothing")]
        public float movementFriction;

        [Header("Grind Settings")]
        public float skateGrindSpeed;
        public float boostGrindSpeed;
        public float grindDirectionThreshold = .25f;
        public float railDetectionRangeFromPlayer = 5.0f;


        [Header("Vertical Movement")]
        [Header("Jump Settings")]
        public float jumpHeight = 1.0f;
        public float fallMultiplier = 2.5f;
        public float lowJumpMultiplier = 2f;

        [Header("Collision")]
        public Bounds groundCollider;
        public LayerMask groundCollisionMask;
    }
}