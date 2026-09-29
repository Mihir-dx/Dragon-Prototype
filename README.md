# Dragon Duel Prototype

A highly polished 1v1 dragon combat prototype showcasing MOBA/Dota-style click-to-move controls, lock-on targeting, and cooldown-gated abilities. Built in **Unity 6000.3.25f1** utilizing the Universal Render Pipeline (URP).

This project was developed with a strong emphasis on clean architecture, decoupled systems, and data-driven design, making it highly extensible and maintainable.

---

## 🎮 Gameplay Features

* **MOBA-Style Controls:** Click-to-move using Unity's `NavMeshAgent`.
* **Lock-On Targeting:** Dynamic enemy targeting with visual indicators, facilitating structured 1v1 combat.
* **Ability System:** Three distinct cooldown-gated abilities (Flame Breath, Tail Swipe, Fly Strike), along with an automated melee attack timer when in range.
* **Intelligent Enemy AI:** State-machine based AI (Idle, Chase, Attack) that navigates the arena, casts abilities, and engages the player dynamically.
* **Invulnerability Frames (i-frames):** Strategic use of abilities (e.g., Fly Strike) grants temporary invulnerability, adding depth to combat mechanics.
* **Dynamic UI (Dota 2 Inspired):** World-space floating health bars, object-pooled damage popups (distinguishing heavy/light hits), and a comprehensive Ability HUD reflecting cooldowns and active states.
* **Game Loop & Polish:** Complete flow with a Main Menu, Win/Lose states, hit-stop mechanics, camera shake on impact, and synchronized particle/audio effects.

---

## ⌨️ Controls

| Input | Action |
| --- | --- |
| **Right click** (ground) | Move to the clicked point (with visual marker) |
| **Right click** (enemy) | Approach the enemy until in melee reach |
| **Left click** (enemy) | Lock on — a target ring appears under the enemy |
| **Left click** (ground) | Clear the lock |
| **Tab / Middle mouse** | Toggle lock-on |
| **1** | Flame Breath (Ranged AoE) |
| **2** | Tail Swipe (Melee AoE) |
| **3** | Fly Strike (Gap closer with Invulnerability) |

---

## 🏗️ Technical Architecture & Design Decisions

The codebase is organized into dedicated namespaces (`Dragon.Core`, `Dragon.Player`, `Dragon.AI`, `Dragon.Combat`, `Dragon.UI`, `Dragon.VFX`, `Dragon.Audio`, `Dragon.Feedback`) to enforce strict modularity.

### Key Architectural Highlights:

**1. Event-Driven Architecture (`Health.cs` & `DragonHealthReactions.cs`)**
The `Health` component strictly manages hit points, exposing `Damaged` and `Died` C# events. It has zero knowledge of the UI, VFX, or AI. Other systems (health bars, damage popups, win screen, AI state transitions) subscribe to these events. This eliminates tight coupling and polling, allowing features to be added without modifying core logic.

**2. Data-Driven Ability System**
Abilities are treated as serialized data (`Ability` struct in `PlayerController`). Adding a new ability is as simple as defining a new array entry in the Inspector with its name, icon, damage, cooldown, and Animator trigger—no new boilerplate code is required. 

**3. Animation-Driven Combat Timing**
To ensure combat feels weighty and fair, damage application is tightly synchronized with animations using **Animation Events**. Damage is applied exactly when the physical hit occurs (or the fire breath hits), rather than instantly upon casting.

**4. Performance Optimizations (Pooling & Caching)**
* **Object Pooling:** Damage popups are frequently spawned and destroyed. Instead of instantiating prefabs on the fly, a Queue-based object pool (`DamagePopup.cs`) reuses instances, preventing GC spikes during combat.
* **Cached Animator Hashes:** All Animator state names and parameters are cached at startup as integer hashes (`AnimatorParams.cs`). The game completely avoids using `string` lookups during runtime state checks, boosting performance.

**5. Robust Input Handling (New Input System)**
Fully integrated with Unity's new Input System. Input Actions are serialized directly onto the behaviors that use them (e.g., `PlayerController`), bypassing the need for massive, centralized wrapper classes while keeping bindings exposed in the Inspector.

**6. Decoupled UI & Layout**
UI components are strictly separated from logic. The `WinScreenManager` simply tells the pre-authored canvas hierarchy to display the winner, avoiding hard-coded UI construction in C#.

---

## 📁 Project Structure

```text
Assets/Scripts/
├── Core/     Health · DragonHealthReactions · AnimatorParams · TransformUtility
├── Player/   PlayerController · Ability · TargetingSystem
├── AI/       AIController
├── Combat/   CombatController
├── UI/       MainMenuManager · WinScreenManager · AbilityHUD · HealthBarBridge · DamagePopup · ClickIndicator
├── VFX/      AbilityVFXController · ImpactVFX
├── Audio/    DragonAudio · BGMManager
└── Feedback/ CameraShake · HitFeedback
```

---

## 🚀 Getting Started

1. Clone the repository.
2. Open the project in **Unity 6000.3.25f1**.
3. Open `Assets/Scenes/Dragon_Fight.unity` to jump straight into the action, or `Assets/Scenes/Main_Menu.unity` for the full flow.
4. Hit Play!

---

## 🛠 Dependencies & Credits

* **Input System** 1.20.0
* **Universal Render Pipeline** 17.3.0
* **AI Navigation** 2.0.14
* **TextMeshPro**

**Third-Party Assets (Placeholder Art/Audio):**
* Dragon models and animations — *Four Evil Dragons HP*
* Particle effects — *Unity Technologies Particle Pack*
* Health bar prefab — *Health System For Dummies* by NoobGameDev
