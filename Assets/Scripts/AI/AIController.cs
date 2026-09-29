using Dragon.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Dragon.AI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class AIController : MonoBehaviour
    {
        public enum AIState { Idle, Chase, Attack }

        [Header("State & Targeting")]
        public AIState currentState = AIState.Idle;

        [Tooltip("Tag used to find the player at startup.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Seconds to wait before the dragon starts chasing, so the fight does not " +
                 "begin the instant the scene loads.")]
        [SerializeField] private float startDelay = 1f;

        [Header("Combat Settings")]
        [Tooltip("Distance at which the dragon stops chasing and starts attacking.")]
        [SerializeField] private float attackRange = 8f;

        [Tooltip("Seconds between ranged fire breaths.")]
        [SerializeField] private float fireAttackCooldown = 3f;

        [Tooltip("How quickly the dragon turns to face the player while attacking.")]
        [SerializeField] private float rotationSpeed = 5f;

        [Tooltip("Closer than this, the ranged attack stops and CombatController's melee " +
                 "auto-attack takes over. Keep this at or above CombatController's " +
                 "forwardOffset or the two will disagree about what counts as melee.")]
        [SerializeField] private float meleeHandoffDistance = 3f;

        [Tooltip("Animator trigger for the ranged fire breath.")]
        [SerializeField] private string fireAttackTrigger = "FlameAttack";

        private Transform player;
        private NavMeshAgent agent;
        private Animator animator;
        private Health health;
        private float lastFireAttackTime;
        private int fireAttackTriggerId;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            health = GetComponent<Health>();

            fireAttackTriggerId = Animator.StringToHash(fireAttackTrigger);

            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null) player = playerObj.transform;

            if (health != null) health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied(Health _)
        {
            enabled = false;

            if (animator != null) animator.SetFloat(AnimatorParams.Speed, 0f);

            if (agent != null)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
                agent.enabled = false;
            }
        }

        private void Start()
        {
            Invoke(nameof(StartChasing), startDelay);
        }

        private void StartChasing() => currentState = AIState.Chase;

        private void Update()
        {
            if (player == null) return;

            // Doesn't interrupt an attack
            if (!AnimatorParams.IsInterruptible(animator))
            {
                agent.isStopped = true;
                animator.SetFloat(AnimatorParams.Speed, 0f);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            switch (currentState)
            {
                case AIState.Idle:
                    agent.isStopped = true;
                    animator.SetFloat(AnimatorParams.Speed, 0f);
                    break;

                case AIState.Chase:
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    animator.SetFloat(AnimatorParams.Speed, agent.velocity.magnitude);

                    if (distanceToPlayer <= attackRange) currentState = AIState.Attack;
                    break;

                case AIState.Attack:
                    agent.isStopped = true;
                    animator.SetFloat(AnimatorParams.Speed, 0f);
                    FacePlayer();

                    // When Player runs off, it will go back to chasing.
                    if (distanceToPlayer > attackRange)
                    {
                        currentState = AIState.Chase;
                        break;
                    }

                    // Ranged fire breath only outside melee reach.
                    bool inRangedBand = distanceToPlayer > meleeHandoffDistance
                                        && distanceToPlayer <= attackRange;

                    if (inRangedBand && Time.time >= lastFireAttackTime + fireAttackCooldown)
                    {
                        PerformFireAttack();
                    }
                    break;
            }
        }

        private void FacePlayer()
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0f; // Keep rotation horizontal.

            if (direction.sqrMagnitude <= 0.01f) return;

            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
        }

        private bool isCastingAbility;

        private void PerformFireAttack()
        {
            lastFireAttackTime = Time.time;
            isCastingAbility = true;
            animator.SetTrigger(fireAttackTriggerId);
        }

        public void OnAbilityHit()
        {
            if (!isCastingAbility) return;
            isCastingAbility = false;

            if (player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange + 1f)
            {
                var targetHealth = player.GetComponent<Health>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    targetHealth.TakeDamage(15f, forceHitReaction: true);
                }
            }
        }
    }
}

// Code by Mihir