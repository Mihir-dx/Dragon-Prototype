using System;
using TMPro;
using UnityEngine;

namespace Dragon.UI
{
    /// <summary>
    /// Inspector-editable look for one class of damage number. <see cref="DamagePopup"/>
    /// holds one of these for normal hits and one for heavy hits and picks between them
    /// from <c>DamageInfo.IsHeavy</c>, rather than guessing from a damage threshold.
    /// </summary>
    [Serializable]
    public struct DamagePopupStyle
    {
        [SerializeField] private Color color;

        [Tooltip("Multiplier on the prefab's authored font size.")]
        [SerializeField] private float fontScale;

        [SerializeField] private bool bold;

        public Color Color => color;
        public float FontScale => fontScale;
        public bool Bold => bold;

        public DamagePopupStyle(Color color, float fontScale, bool bold)
        {
            this.color = color;
            this.fontScale = fontScale;
            this.bold = bold;
        }
    }

    /// <summary>
    /// A single floating damage number: rises, billboards to the camera, punches in scale
    /// on spawn, then fades out. Lives on the DamagePopup prefab, so its font, material and
    /// outline are authored in the editor instead of assembled in C#.
    ///
    /// Pooled rather than destroyed — <see cref="DamagePopup"/> calls <see cref="Play"/> to
    /// restart an instance and reads <see cref="IsFinished"/> to know when one is free, so a
    /// long fight no longer allocates and destroys a GameObject per hit.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamagePopupInstance : MonoBehaviour
    {
        [Header("Motion")]
        [Tooltip("Upward speed in world units per second.")]
        [SerializeField] private float floatSpeed = 2f;

        [Tooltip("Seconds the number stays visible. The fade runs over the second half so " +
                 "the value is fully readable first.")]
        [SerializeField] private float duration = 1f;

        [Header("Scale Punch")]
        [Tooltip("Scale the number spawns at before easing down to 1.")]
        [SerializeField] private float punchScale = 1.5f;

        [Tooltip("Seconds the scale punch takes to settle.")]
        [SerializeField] private float punchDuration = 0.15f;

        private TextMeshPro label;
        private Camera billboardCamera;
        private float elapsed;
        private float baseFontSize;
        private bool initialised;

        /// <summary>True when this instance is idle and the pool may hand it out again.</summary>
        public bool IsFinished { get; private set; } = true;

        /// <summary>
        /// Caching happens here rather than in Awake because the prefab is pooled inactive:
        /// Instantiate does not run Awake on an inactive object, so <see cref="Play"/> would
        /// otherwise be the first thing to touch this component.
        /// </summary>
        private void EnsureInitialised()
        {
            if (initialised) return;

            label = GetComponent<TextMeshPro>();
            baseFontSize = label.fontSize;   // the prefab's authored size, before any scaling
            initialised = true;
        }

        /// <summary>
        /// Apply the spawner's motion tuning. Called once when the instance is created, so
        /// the numbers stay editable in one place (<see cref="DamagePopup"/>) rather than
        /// being duplicated on every pooled object.
        /// </summary>
        public void Configure(float floatSpeed, float duration)
        {
            this.floatSpeed = floatSpeed;
            this.duration = duration;
        }

        /// <summary>Show <paramref name="text"/> at a world position and start the animation.</summary>
        public void Play(string text, DamagePopupStyle style, Vector3 worldPosition, Camera camera)
        {
            EnsureInitialised();

            label.text = text;
            label.color = style.Color;
            label.fontSize = baseFontSize * style.FontScale;
            label.fontStyle = style.Bold ? FontStyles.Bold : FontStyles.Normal;

            billboardCamera = camera;
            transform.position = worldPosition;
            transform.localScale = Vector3.one * punchScale;

            elapsed = 0f;
            IsFinished = false;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (IsFinished) return;

            elapsed += Time.deltaTime;

            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            if (billboardCamera == null) billboardCamera = Camera.main;
            if (billboardCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - billboardCamera.transform.position);
            }

            transform.localScale = punchDuration > 0f && elapsed < punchDuration
                ? Vector3.one * Mathf.Lerp(punchScale, 1f, elapsed / punchDuration)
                : Vector3.one;

            float fadeStart = duration * 0.5f;
            if (elapsed > fadeStart && duration > fadeStart)
            {
                Color c = label.color;
                c.a = Mathf.Lerp(1f, 0f, (elapsed - fadeStart) / (duration - fadeStart));
                label.color = c;
            }

            if (elapsed >= duration)
            {
                IsFinished = true;
                gameObject.SetActive(false);
            }
        }
    }
}
