using System.Collections.Generic;
using Dragon.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.UI
{
    public class AbilityHUD : MonoBehaviour
    {
        [Header("Player Reference")]
        [Tooltip("Drag the Player_Dragon here. The HUD reads its Abilities list.")]
        [SerializeField] private PlayerController player;

        [Header("Ability Icons (one per ability, in order)")]
        [Tooltip("Sprite for Ability 1 (Flame Breath).")]
        [SerializeField] private Sprite ability1Icon;

        [Tooltip("Sprite for Ability 2 (Tail Swipe).")]
        [SerializeField] private Sprite ability2Icon;

        [Tooltip("Sprite for Ability 3 (Fly Strike).")]
        [SerializeField] private Sprite ability3Icon;

        [Header("Layout")]
        [Tooltip("Size of each ability slot in pixels.")]
        [SerializeField] private float slotSize = 80f;

        [Tooltip("Gap between slots in pixels.")]
        [SerializeField] private float slotSpacing = 12f;

        [Tooltip("Distance from bottom of screen in pixels.")]
        [SerializeField] private float bottomMargin = 40f;

        [Header("Cooldown Visuals")]
        [Tooltip("Color of the darkening overlay when on cooldown.")]
        [SerializeField] private Color cooldownOverlayColor = new Color(0f, 0f, 0f, 0.7f);

        [Tooltip("Color of the countdown timer text.")]
        [SerializeField] private Color cooldownTextColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Font size for the countdown number.")]
        [SerializeField] private float countdownFontSize = 28f;

        [Tooltip("Font size for the key label (1, 2, 3).")]
        [SerializeField] private float keyLabelFontSize = 16f;

        // Runtime slot data.
        private readonly List<AbilitySlot> slots = new List<AbilitySlot>();

        private struct AbilitySlot
        {
            public Ability ability;
            public Image iconImage;
            public Image cooldownOverlay;
            public TextMeshProUGUI countdownText;
            public Image borderImage;
        }

        private void Start()
        {
            if (player == null)
            {
                Debug.LogWarning("[AbilityHUD] No PlayerController assigned.");
                return;
            }

            BuildSlots();
        }

        private void Update()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                AbilitySlot slot = slots[i];
                if (slot.ability == null) continue;

                float normalized = slot.ability.CooldownNormalized;
                bool onCooldown = normalized > 0f;

                // Radial fill sweep.
                slot.cooldownOverlay.fillAmount = normalized;
                slot.cooldownOverlay.enabled = onCooldown;

                // Countdown text.
                if (onCooldown)
                {
                    float remaining = slot.ability.CooldownRemaining;
                    slot.countdownText.text = remaining.ToString("F1");
                    slot.countdownText.enabled = true;
                }
                else
                {
                    slot.countdownText.enabled = false;
                }

                // Desaturate icon when on cooldown.
                slot.iconImage.color = onCooldown ? new Color(0.4f, 0.4f, 0.4f, 1f) : Color.white;

                // Border glow when ready.
                if (slot.borderImage != null)
                {
                    slot.borderImage.color = onCooldown
                        ? new Color(0.3f, 0.3f, 0.3f, 0.8f)
                        : new Color(0.85f, 0.65f, 0.2f, 1f);
                }
            }
        }

        private void BuildSlots()
        {
            Sprite[] icons = { ability1Icon, ability2Icon, ability3Icon };
            string[] keyLabels = { "1", "2", "3" };
            IReadOnlyList<Ability> abilities = player.Abilities;

            // Container anchored to bottom-centre.
            GameObject container = new GameObject("AbilitySlots");
            container.transform.SetParent(transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.anchoredPosition = new Vector2(0f, bottomMargin);

            float totalWidth = abilities.Count * slotSize + (abilities.Count - 1) * slotSpacing;
            containerRect.sizeDelta = new Vector2(totalWidth, slotSize + 24f);

            // Add HorizontalLayoutGroup for clean spacing.
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = slotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            for (int i = 0; i < abilities.Count && i < icons.Length; i++)
            {
                AbilitySlot slot = CreateSlot(container.transform, abilities[i],
                    icons[i], keyLabels[i]);
                slots.Add(slot);
            }
        }

        private AbilitySlot CreateSlot(Transform parent, Ability ability,
            Sprite iconSprite, string keyLabel)
        {
            // Root slot
            GameObject slotObj = new GameObject($"Slot_{ability.DisplayName}");
            slotObj.transform.SetParent(parent, false);

            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotSize, slotSize + 20f);

            // Outer border
            GameObject border = new GameObject("Border");
            border.transform.SetParent(slotObj.transform, false);

            RectTransform borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(0f, 20f);
            borderRect.offsetMax = Vector2.zero;

            Image borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(0.85f, 0.65f, 0.2f, 1f); // Gold frame
            borderImg.raycastTarget = false;

            // Icon background
            GameObject iconBg = new GameObject("IconBg");
            iconBg.transform.SetParent(border.transform, false);

            RectTransform iconBgRect = iconBg.AddComponent<RectTransform>();
            iconBgRect.anchorMin = Vector2.zero;
            iconBgRect.anchorMax = Vector2.one;
            iconBgRect.offsetMin = new Vector2(3f, 3f);
            iconBgRect.offsetMax = new Vector2(-3f, -3f);

            Image bgImg = iconBg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            bgImg.raycastTarget = false;

            // Ability icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(iconBg.transform, false);

            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image iconImage = icon.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            // Cooldown overlay
            GameObject overlay = new GameObject("CooldownOverlay");
            overlay.transform.SetParent(iconBg.transform, false);

            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = cooldownOverlayColor;
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = Image.FillMethod.Radial360;
            overlayImage.fillOrigin = (int)Image.Origin360.Top;
            overlayImage.fillClockwise = false;
            overlayImage.fillAmount = 0f;
            overlayImage.enabled = false;
            overlayImage.raycastTarget = false;

            // Countdown text
            GameObject cdText = new GameObject("CountdownText");
            cdText.transform.SetParent(iconBg.transform, false);

            RectTransform cdRect = cdText.AddComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;

            TextMeshProUGUI countdownTMP = cdText.AddComponent<TextMeshProUGUI>();
            countdownTMP.text = "";
            countdownTMP.fontSize = countdownFontSize;
            countdownTMP.color = cooldownTextColor;
            countdownTMP.alignment = TextAlignmentOptions.Center;
            countdownTMP.fontStyle = FontStyles.Bold;
            countdownTMP.enabled = false;
            countdownTMP.raycastTarget = false;

            // Key label
            GameObject keyObj = new GameObject("KeyLabel");
            keyObj.transform.SetParent(slotObj.transform, false);

            RectTransform keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 0f);
            keyRect.anchorMax = new Vector2(1f, 0f);
            keyRect.pivot = new Vector2(0.5f, 0f);
            keyRect.sizeDelta = new Vector2(slotSize, 20f);
            keyRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI keyTMP = keyObj.AddComponent<TextMeshProUGUI>();
            keyTMP.text = keyLabel;
            keyTMP.fontSize = keyLabelFontSize;
            keyTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            keyTMP.alignment = TextAlignmentOptions.Center;
            keyTMP.fontStyle = FontStyles.Bold;
            keyTMP.raycastTarget = false;

            return new AbilitySlot
            {
                ability = ability,
                iconImage = iconImage,
                cooldownOverlay = overlayImage,
                countdownText = countdownTMP,
                borderImage = borderImg,
            };
        }
    }
}

// Code by AI