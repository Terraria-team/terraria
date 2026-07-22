using System;
using UnityEngine;

namespace Shared.DataDefinitions
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName = "Enemy";
        public EnemyBehaviorType behaviorType = EnemyBehaviorType.Fighter;
        public Sprite sprite;

        [Header("Movement")]
        public float moveSpeed = 3f;
        public float jumpForce = 8f;

        [Header("Detection")]
        public float detectionRange = 8f;
        public float loseTargetRange = 12f;

        [Header("Combat")]
        public float attackRange = 1.2f;
        public float attackCooldown = 1.5f;
        public float attackDamage = 10f;

        [Header("Navigation Raycasts")]
        public float stepCheckDistance = 0.7f;
        public float gapCheckDepth = 2.5f;
        public float wallCheckHeight = 0.5f;
        //determines if wall is jumpable
        public float headClearHeight = 2.2f;
        
        [Header("Flyer")]
        public bool useSinusoidalFlight = false;
        public float sinFrequency = 2f;
        public float sinAmplitude = 1.5f;

        [Header("Shooter")]
        public float preferredShootDistance = 6f;
        public float projectileSpeed = 8f;
        public float projectileLifetime = 4f;
        public bool projectilePassesThroughWalls = false;
    }
}
