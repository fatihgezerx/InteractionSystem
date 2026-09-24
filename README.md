# InteractionSystem

Modular, event-driven interaction system for Unity.

![InteractionSystem](ScreenShots/InteractionSystem.png)

## Overview

InteractionSystem detects what the player is looking at (or standing next to) and lets them interact
with it - press or hold a key, show a prompt like **Open Door**, run whatever you wire up. It has
**no `Update` and no coroutines**: detection runs on a cancellable **UniTask** loop at the rate you
choose, input comes from **Input System** callbacks, and every moment of an interaction is published
as an event.

Everything is configured in a single `InteractData` asset: detection settings on top, your
interactable prefabs below in named groups. One click on **Compile** makes every listed prefab
interactable - component, collider, layer and settings included.

## Features

- **No `Update`, no coroutines**: a UniTask detection loop with a configurable check interval
- **Line or Sphere detection**: a line for first-person (anything in front, like a wall, blocks it)
  or a sphere for third-person (the closest interactable in range wins)
- **Flexible origin**: pick the object by tag, optionally one of its children, then offset it with a
  local position and rotation
- **One-click Compile**: adds an `Interactable` component (only if missing), a `BoxCollider` (only if
  the object has no collider) and the `Interact` layer (created automatically), and skips prefabs that
  are already up to date
- **Grouped prompts**: a group's name is shown in front of each object's name - `Open` + `Door` =
  `Open Door` - or leave it empty to show just the name
- **Hold to interact** per object, with live progress for UI (e.g. a slider) and a timer that resets
  when the key is released or the object loses focus
- **Two ways to react**: `UnityEvent`s on each `Interactable` for no-code wiring, and `EventManager`
  events carrying an `InteractionArgs` payload for code
- **Scene view gizmo**: the line or sphere is drawn yellow while nothing is detected, green while
  something is
- Drag-to-reorder groups in the Inspector

## Setup

### Requirements

- Unity 2022.3 LTS or newer (developed on Unity 6)
- [UniTask](https://github.com/Cysharp/UniTask)
- **The new Input System** (1.8 or newer, for project-wide actions)
- An **EventSystem** providing `EventManager` and `EventTypes` in an assembly named
  `EventSystem.Runtime` (see below)

### Installation

**1. Install the dependencies first.** The system won't compile without them:

- **UniTask**: open `Window > Package Manager`, click `+ > Add package from git URL...` and paste:
  ```
  https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
  ```
- **Input System**: `Window > Package Manager > Unity Registry > Input System > Install`.
- **EventSystem**: an `EventManager` / `EventTypes` in an assembly named `EventSystem.Runtime`
  (see [EventSystem](#eventsystem) below).

**2. Add InteractionSystem.** Clone or download this repository, then copy its contents into a folder
under `Assets/` (e.g. `Assets/Scripts/InteractionSystem/`). The system comes with its own assembly
definitions.

### EventSystem

InteractionSystem publishes its events through an `EventManager` that isn't part of this repository.
It uses `EventManager.RegisterEvent<T>`, `UnregisterEvent<T>` and `InvokeEvent<T>`, keyed by an
`EventTypes` enum. Add these four members to your `EventTypes` enum:

```csharp
public enum EventTypes
{
    // ...your own events

    OnFocus,
    OnLoseFocus,
    OnInteract,
    OnInteracting,
}
```

### Input (important)

This system works with the **new Input System**. For it to work, the project-wide input actions
(**Project Settings > Input System Package > Project-wide Actions**) must contain an action named
**`Interact`**. Assign whatever key you want to it (e.g. `E` on the keyboard, `Button West` on a
gamepad).

Interaction fires the moment the key is pressed, or after holding it for **Duration** seconds on
objects marked **Holding**. Holding is configured per object in `InteractData`, so you don't need
the **Hold** interaction that Unity's default template puts on `Interact`. You can remove it.

## Quick Start

**1. Create the data asset** via `Create > Interaction System > Interact Data`. Its Inspector has two
blocks: **INTERACT SETTINGS** and **INTERACTABLES**.

| Field | Meaning |
|---|---|
| Raycast Type | `Line` (FPS) or `Sphere` (TPS) |
| Origin Tag | Detection starts from the object with this tag |
| Local Position / Local Rotation | Where detection starts and which way the line points, relative to that object (its local space) |
| In Child / Child Count | If In Child is on, detection starts from the child at index Child Count instead (e.g. the camera under the player) |
| Radius | `Line`: length of the ray. `Sphere`: radius of the sphere (0.1 - 5) |
| Check Interval | Seconds between checks (0.01 - 1) |
| Raycast Layers | Only objects on these layers are detected |
| Interactables | Groups of prefabs. Each group has a **name**; each prefab has a **Name** (empty = the prefab's name) and **Holding** / **Duration** |

![Interact Data inspector with its settings and a group of interactables](ScreenShots/Inspector.png)

`Line` never sees through anything: if an object that isn't on Raycast Layers (a wall) is in front
of an interactable, nothing is detected. `Sphere` detects every interactable within Radius, walls or
not, and focuses the closest one.

A **group's name** is shown in front of the name of every object in it: a group named `Open` holding
a `Door` gives `Open Door`, and a group named `Collect` holding a `Battery` gives `Collect Battery`.
Use as many groups as you like, a single group (e.g. `Interact`), or leave the group name empty to
show only the object's name. Reorder groups by dragging the handle on the left of a group's name.

**2. Drag your prefabs into the groups and click Compile.** Every prefab gets:
- an `Interactable` component, only if it doesn't have one yet,
- a `BoxCollider`, only if it has no collider yet,
- the `Interact` layer (created automatically if it doesn't exist),
- its group name, Name and Holding / Duration settings.

Compiling again after adding or changing entries only touches the prefabs that changed.

![Interactable component with its On Focus, On Interact and On Lose Focus events](ScreenShots/Interactable.png)

**3. Initialize once, e.g. from your GameManager:**

```csharp
using InteractionSystem;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private InteractData interactData;

    private void Start() => InteractionManager.Initialize(interactData);
    private void OnDestroy() => InteractionManager.Shutdown();
}
```

The object with the Origin Tag must exist in the scene when `Initialize` is called. While the game
is running, the Scene view draws the line or the sphere: **yellow** while nothing is detected,
**green** while something is.

## Events

Each `Interactable` has three `UnityEvent`s, wired from the Inspector:

- **On Focus**: the object started being detected
- **On Interact**: Interact was pressed (or held for Duration) while the object is focused
- **On Lose Focus**: the object stopped being detected

For example, with [PoolSystem](https://github.com/fatihgezerx/PoolSystem), drag a pooled object's
`Poolable` into **On Interact** and pick `ReleaseSelf` to return it to its pool when interacted with.

The same moments are also published through `EventManager` with an `InteractionArgs` payload
(`Value` = the `Interactable`):

| `args.Value.` | Example | Meaning |
|---|---|---|
| `FullName` | `Open Door` | Group name + object name, ready for UI. Just the name if the group has no name |
| `Header` | `Open` | The group name alone (may be empty) |
| `DisplayName` | `Door` | The object name alone |
| `Holding` / `HoldDuration` | `true` / `2` | Hold settings |

```csharp
private void OnEnable()  => EventManager.RegisterEvent<InteractionArgs>(EventTypes.OnFocus, OnFocus);
private void OnDisable() => EventManager.UnregisterEvent<InteractionArgs>(EventTypes.OnFocus, OnFocus);

private void OnFocus(InteractionArgs args) => promptLabel.text = args.Value.FullName; // "Open Door"
```

`EventTypes.OnFocus`, `EventTypes.OnLoseFocus`, `EventTypes.OnInteract` and `EventTypes.OnInteracting`
all use `InteractionArgs`.

## Holding

For a **Holding** object, Interact must be held for **Duration** seconds. The timer resets if the
key is released or the object loses focus. `On Focus` fires only once per object until it loses
focus, so moving the aim around on the same object never restarts the timer.

While a Holding object is being held, `EventTypes.OnInteracting` is published with
`InteractionArgs.Elapsed` = seconds held so far:
- with 0 when the key is pressed,
- every frame while it is held,
- with 0 again when the hold ends (key released, focus lost, or completed right after `OnInteract`).

Example: a hold slider in the UI. The maximum comes from the focused object, the current value from
`OnInteracting`. No `Update` is needed:

```csharp
using EventSystem;
using InteractionSystem;
using UnityEngine;
using UnityEngine.UI;

public class HoldSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void OnEnable()
    {
        EventManager.RegisterEvent<InteractionArgs>(EventTypes.OnFocus, OnFocus);
        EventManager.RegisterEvent<InteractionArgs>(EventTypes.OnLoseFocus, OnLoseFocus);
        EventManager.RegisterEvent<InteractionArgs>(EventTypes.OnInteracting, OnInteracting);
    }

    private void OnDisable()
    {
        EventManager.UnregisterEvent<InteractionArgs>(EventTypes.OnFocus, OnFocus);
        EventManager.UnregisterEvent<InteractionArgs>(EventTypes.OnLoseFocus, OnLoseFocus);
        EventManager.UnregisterEvent<InteractionArgs>(EventTypes.OnInteracting, OnInteracting);
    }

    private void OnFocus(InteractionArgs args)
    {
        slider.gameObject.SetActive(args.Value.Holding);
        slider.maxValue = args.Value.HoldDuration;
        slider.value = 0f;
    }

    private void OnLoseFocus(InteractionArgs args) => slider.gameObject.SetActive(false);

    private void OnInteracting(InteractionArgs args) => slider.value = args.Elapsed;
}
```

`OnInteracting` reuses one `InteractionArgs` instance to avoid allocating every frame, so never
keep a reference to it past the callback.

## License

[MIT License](LICENSE)
