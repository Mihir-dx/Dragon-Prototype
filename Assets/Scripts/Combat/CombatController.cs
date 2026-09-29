using Dragon.Core;
using UnityEngine;

namespace Dragon.Combat
{
    /// <summary>
    /// Melee auto-attack, shared by both dragons. Sweeps a sphere just in front of the
    /// dragon on a timer and hits the first valid target it finds.
    ///
    /// Auto-attacks deliberately pass forceHitReaction: false, so they never flinch the
    /// victim. That is what keeps two dragons in melee from stun-locking each other.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CombatController : MonoBehaviour
    {
        [Header("Auto-Attack Settings")]
        [Tooltip("Tag a collider must carry to be attacked.")]
        [SerializeField] private string targetTag = "Enemy";

        [Tooltip("Seconds between auto-attacks.")]
        [SerializeField] private float attackCooldown = 1.5f;

        [Tooltip("Damage per auto-attack.")]
        [SerializeField] private float attackDamage = 10f;

        [Tooltip("Animator trigger played on each auto-attack. This reuses the ability " +
                 "trigger 'TailAttack' because the Animator has no separate basic-attack " +
                 "state — add one and point this at it to make auto-attacks look distinct. " +
                 "A name that does not exist on the Animator plays nothing and reports no error.")]
        [SerializeField] private string attackTrigger = "TailAttack";

        [Header("Detection Area")]
        [Tooltip("How far in front of the dragon the detection sphere sits.")]
        [SerializeField] private float forwardOffset = 2.5f;

        [Tooltip("Radius of the detection sphere.")]
        [SerializeField] private float detectionRadius = 2f;

        [Tooltip("Layers the detection sweep considers. Defaults to everything.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        /// <summary>
        /// Reused across sweeps so the per-attack overlap check does not allocate a fresh
        /// array every time. Sized well above the two dragons this scene ever has.
        /// </summary>
        private readonly Collider[] detectionBuffer = new Collider[16];

        private Animator animator;
        private Health health;
        private float lastAttackTime;
        private int attackTriggerId;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            health = GetComponent<Health>();

            attackTriggerId = Animator.StringToHash(attackTrigger);

            if (health != null) health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied(Health _) => enabled = false;

        private void Update()
        {
            // Never interrupt an ability, a flinch or the death clip.
            if (!AnimatorParams.IsInterruptible(animator)) return;

            if (Time.time >= lastAttackTime + attackCooldown) DetectAndAttack();
        }

        private void DetectAndAttack()
        {
            Vector3 detectPosition = transform.position + transform.forward * forwardOffset;

            int count = Physics.OverlapSphereNonAlloc(
                detectPosition, detectionRadius, detectionBuffer, targetLayers);

            for (int i = 0; i < count; i++)
            {
                Collider hit = detectionBuffer[i];
                if (hit == null || !hit.CompareTag(targetTag)) continue;

                PerformAutoAttack(hit.gameObject);
                return; // Only one target per cooldown.
            }
        }

        private void PerformAutoAttack(GameObject target)
        {
            lastAttackTime = Time.time;

            animator.SetTrigger(attackTriggerId);

            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth != null && !targetHealth.IsDead)
            {
                targetHealth.TakeDamage(attackDamage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw the detection sphere in the Scene view for easy visual tuning.
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position + transform.forward * forwardOffset, detectionRadius);
        }
    }
}
