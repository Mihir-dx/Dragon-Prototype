using System;
using UnityEngine;

namespace Dragon.Core
{
    /// <summary>
    /// Transform helpers shared by the dragon components.
    /// Replaces the identical private FindDeepChild that used to live in both
    /// AbilityVFXController and HealthBarBridge.
    /// </summary>
    public static class TransformUtility
    {
        /// <summary>
        /// Depth-first search for a descendant by exact name, ignoring case.
        /// Used to locate skeleton bones such as "Head" that the FBX importer nests
        /// several levels deep, which is what lets the Inspector reference stay optional.
        /// Returns null when nothing matches — callers decide whether that is fatal.
        /// </summary>
        public static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName)) return null;

            foreach (Transform child in parent)
            {
                if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform found = FindDeepChild(child, childName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
