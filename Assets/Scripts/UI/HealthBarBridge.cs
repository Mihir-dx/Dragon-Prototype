using Dragon.Core;
using UnityEngine;

namespace Dragon.UI
{
    /// <summary>
    /// Bridges the game's <see cref="Health"/> component to the NoobGameDev
    /// <see cref="HealthSystemForDummies"/> + <see cref="HealthBar"/> UI system.
    ///
    /// Attach to any dragon that has a Health component. It adds HealthSystemForDummies
    /// if missing, spawns the HealthBarCanvas prefab as a child, and keeps the bar
    /// floating above the dragon's head.
    ///
    /// Health values are pushed on the <see cref="Health.Damaged"/> event rather than
    /// polled, so no work happens on frames where nothing was hit. Position still updates
    /// every frame — the head bone moves with the animation, so that part is legitimate.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthBarBridge : MonoBehaviour
    {
        [Header("Health Bar Prefab")]
        [Tooltip("The HealthBarCanvas prefab from NoobGameDev's Health System.")]
        [SerializeField] private GameObject healthBarPrefab;

        [Header("Positioning")]
        [Tooltip("Optional: a specific transform (e.g. the Head bone) to follow. " +
                 "Left empty, the Head bone is found automatically at startup.")]
        [SerializeField] private Transform headBone;

        [Tooltip("Vertical offset above the head bone, in world units.")]
        [SerializeField] private float headBoneExtraHeight = 1.0f;

        [Tooltip("Fallback offset above the dragon's pivot, used only if no head bone " +
                 "is assigned or found.")]
        [SerializeField] private float heightAboveHead = 4.5f;

        private Health health;
        private HealthSystemForDummies healthSystem;
        private GameObject healthBarInstance;

        // Resolved once at startup so the per-frame reposition is a single expression.
        private Transform barAnchor;
        private float barOffset;

        // The third-party bar animates its fill from deltas, so we track what we last
        // pushed rather than assigning CurrentHealth outright.
        private float lastSyncedHealth;

        private void Awake()
        {
            health = GetComponent<Health>();

            EnsureHealthSystem();

            healthSystem.MaximumHealth = health.MaxHealth;
            healthSystem.CurrentHealth = health.MaxHealth;
            healthSystem.IsAlive = true;
            healthSystem.HasAnimationWhenHealthChanges = true;
            healthSystem.AnimationDuration = 0.15f;

            lastSyncedHealth = health.MaxHealth;

            if (headBone == null) headBone = TransformUtility.FindDeepChild(transform, "Head");

            barAnchor = headBone != null ? headBone : transform;
            barOffset = headBone != null ? headBoneExtraHeight : heightAboveHead;

            health.Damaged += HandleDamaged;
        }

        private void OnDestroy()
        {
            if (health != null) health.Damaged -= HandleDamaged;

            if (healthBarInstance != null) Destroy(healthBarInstance);
        }

        private void EnsureHealthSystem()
        {
            healthSystem = GetComponent<HealthSystemForDummies>();
            if (healthSystem != null) return;

            healthSystem = gameObject.AddComponent<HealthSystemForDummies>();

            // Added via AddComponent rather than the Inspector, its UnityEvent fields are
            // null. Initialise them or the first health change throws.
            healthSystem.OnCurrentHealthChanged = new OnCurrentHealthChanged();
            healthSystem.OnIsAliveChanged = new OnIsAliveChanged();
            healthSystem.OnMaximumHealthChanged = new OnMaximumHealthChanged();
        }

        private void Start()
        {
            SpawnHealthBar();
        }

        private void SpawnHealthBar()
        {
            if (healthBarPrefab == null)
            {
                Debug.LogWarning($"[HealthBarBridge] No healthBarPrefab assigned on {name}.");
                return;
            }

            // Spawn as a child so HealthBar.cs can find us with GetComponentInParent.
            healthBarInstance = Instantiate(healthBarPrefab, transform);

            // Cancel out the dragon's world scale so the bar renders at its intended size
            // whether the dragon is scaled 0.5 or 1.0.
            Vector3 lossyScale = transform.lossyScale;
            healthBarInstance.transform.localScale = new Vector3(
                1f / lossyScale.x,
                1f / lossyScale.y,
                1f / lossyScale.z);

            PositionHealthBar();
        }

        /// <summary>
        /// Push the change into the third-party system as a delta, which is what drives its
        /// animated fill. Uses RemainingHealth rather than the raw damage so an overkill
        /// hit does not push the bar below empty.
        /// </summary>
        private void HandleDamaged(DamageInfo info)
        {
            if (healthSystem == null) return;

            float delta = info.RemainingHealth - lastSyncedHealth;
            if (Mathf.Approximately(delta, 0f)) return;

            healthSystem.AddToCurrentHealth(delta);
            lastSyncedHealth = info.RemainingHealth;
        }

        /// <summary>
        /// Keep the bar above the dragon's head. The bar is a child (so the third-party
        /// script can find the system in its parents) but its world position is set
        /// directly, so it does not inherit the dragon's rotation.
        /// </summary>
        private void PositionHealthBar()
        {
            if (healthBarInstance == null) return;

            healthBarInstance.transform.position = barAnchor.position + Vector3.up * barOffset;
        }

        private void LateUpdate()
        {
            PositionHealthBar();
        }
    }
}
