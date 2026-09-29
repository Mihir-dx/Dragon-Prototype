using System.Collections.Generic;
using Dragon.Core;
using TMPro;
using UnityEngine;

namespace Dragon.UI
{
    /// <summary>
    /// Spawns floating damage numbers above a dragon when it is hit.
    ///
    /// Driven by the <see cref="Health.Damaged"/> event rather than by diffing health every
    /// frame, and told by <see cref="DamageInfo.IsHeavy"/> whether a hit was an ability or
    /// an auto-attack — previously it guessed from a damage threshold that had to be kept
    /// in sync with ability damage by hand.
    ///
    /// Instances are pooled and reused, so a long fight no longer creates and destroys a
    /// GameObject per hit.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamagePopup : MonoBehaviour
    {
        [Header("Popup Settings")]
        [Tooltip("How long each damage number stays visible.")]
        [SerializeField] private float popupDuration = 1.0f;

        [Tooltip("How fast the number floats upward (world units/sec).")]
        [SerializeField] private float floatSpeed = 2.0f;

        [Tooltip("Height above the dragon's pivot where the number spawns.")]
        [SerializeField] private float spawnHeight = 3.5f;

        [Tooltip("Random horizontal spread so stacked hits don't overlap.")]
        [SerializeField] private float horizontalSpread = 0.5f;

        [Header("Font Settings")]
        [Tooltip("Base font size for damage numbers.")]
        [SerializeField] private float fontSize = 8f;

        [Tooltip("Colour for normal (auto-attack) damage.")]
        [SerializeField] private Color normalDamageColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Colour for heavy (ability) damage.")]
        [SerializeField] private Color heavyDamageColor = new Color(1f, 0.3f, 0.1f, 1f);

        [Tooltip("How much larger heavy hits render, as a multiple of the base font size.")]
        [SerializeField] private float heavyFontScale = 1.3f;

        private Health health;
        private Camera mainCamera;

        private readonly List<DamagePopupInstance> pool = new List<DamagePopupInstance>();

        private void Awake()
        {
            health = GetComponent<Health>();
            mainCamera = Camera.main;

            health.Damaged += HandleDamaged;
        }

        private void OnDestroy()
        {
            if (health != null) health.Damaged -= HandleDamaged;
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (mainCamera == null) mainCamera = Camera.main;

            DamagePopupStyle style = info.IsHeavy
                ? new DamagePopupStyle(heavyDamageColor, heavyFontScale, true)
                : new DamagePopupStyle(normalDamageColor, 1f, false);

            Vector3 position = transform.position + new Vector3(
                Random.Range(-horizontalSpread, horizontalSpread),
                spawnHeight,
                Random.Range(-horizontalSpread, horizontalSpread));

            GetFreeInstance().Play(
                Mathf.RoundToInt(info.Amount).ToString(), style, position, mainCamera);
        }

        /// <summary>
        /// Hand out an idle popup, creating one only when every existing instance is still
        /// animating. In practice the pool settles at however many numbers overlap at once.
        /// </summary>
        private DamagePopupInstance GetFreeInstance()
        {
            foreach (DamagePopupInstance instance in pool)
            {
                if (instance != null && instance.IsFinished) return instance;
            }

            DamagePopupInstance created = CreateInstance();
            pool.Add(created);
            return created;
        }

        private DamagePopupInstance CreateInstance()
        {
            // Created active so TextMeshPro initialises properly, then parked inactive
            // until the first Play() switches it on.
            var popup = new GameObject("DamagePopup");

            TextMeshPro label = popup.AddComponent<TextMeshPro>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.sortingOrder = 100;      // Render above other world-space UI.
            label.raycastTarget = false;   // Never swallow a click.

            var rect = label.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(4f, 2f);

            var instance = popup.AddComponent<DamagePopupInstance>();
            instance.Configure(floatSpeed, popupDuration);

            popup.SetActive(false);
            return instance;
        }
    }
}
