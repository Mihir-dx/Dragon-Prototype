using Dragon.Core;
using UnityEngine;

namespace Dragon.UI
{
    [RequireComponent(typeof(HealthSystemForDummies))]
    public class FloatingHealthBar : MonoBehaviour
    {
        [Header("Tracking")]
        [Tooltip("The Health component this bar tracks.")]
        [SerializeField] private Health targetHealth;

        [Tooltip("The bone to follow (e.g. the dragon's Head).")]
        [SerializeField] private Transform trackBone;

        [Tooltip("Extra vertical offset above the tracked bone (in world units).")]
        [SerializeField] private float worldOffset = 1.5f;

        [Header("UI Scaling")]
        [Tooltip("How much to scale the UI. The NoobGameDev prefab is massive by default.")]
        [SerializeField] private float uiScale = 0.5f;

        private HealthSystemForDummies healthSystem;
        private float lastSyncedHealth;
        private Camera mainCam;
        private RectTransform rectTransform;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystemForDummies>();
            rectTransform = GetComponent<RectTransform>();

            if (healthSystem.OnCurrentHealthChanged == null)
                healthSystem.OnCurrentHealthChanged = new OnCurrentHealthChanged();
            if (healthSystem.OnIsAliveChanged == null)
                healthSystem.OnIsAliveChanged = new OnIsAliveChanged();
            if (healthSystem.OnMaximumHealthChanged == null)
                healthSystem.OnMaximumHealthChanged = new OnMaximumHealthChanged();
        }

        private void Start()
        {
            mainCam = Camera.main;

            if (targetHealth != null)
            {
                if (trackBone == null)
                {
                    trackBone = TransformUtility.FindDeepChild(targetHealth.transform, "Head");
                    if (trackBone == null) trackBone = targetHealth.transform;
                }

                healthSystem.MaximumHealth = targetHealth.MaxHealth;
                healthSystem.CurrentHealth = targetHealth.MaxHealth;
                healthSystem.IsAlive = !targetHealth.IsDead;
                healthSystem.HasAnimationWhenHealthChanges = true;
                healthSystem.AnimationDuration = 0.15f;

                lastSyncedHealth = targetHealth.MaxHealth;
                targetHealth.Damaged += HandleDamaged;
            }
        }

        private void OnDestroy()
        {
            if (targetHealth != null)
            {
                targetHealth.Damaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (healthSystem == null) return;
            
            float delta = info.RemainingHealth - lastSyncedHealth;
            if (Mathf.Approximately(delta, 0f)) return;

            healthSystem.AddToCurrentHealth(delta);
            lastSyncedHealth = info.RemainingHealth;
        }

        private void LateUpdate()
        {
            if (targetHealth == null || mainCam == null || rectTransform == null) return;

            Vector3 worldPos = trackBone.position + Vector3.up * worldOffset;
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);
            
            bool isBehindCamera = screenPos.z < 0;

            if (isBehindCamera)
            {
                rectTransform.localScale = Vector3.zero;
            }
            else
            {
                rectTransform.position = screenPos;
                rectTransform.localScale = Vector3.one * uiScale;
            }
        }
    }
}

//Code by Mihir