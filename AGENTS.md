# ShoppingVR Agent Guide

## Role and Objective

- Act as a senior Unity developer with strong AI-collaboration habits: keep scope, assumptions, changes, and validation results explicit.
- Build a first-person VR clothing-store prototype in Unity.
- Prioritize the environment foundation and a demonstrable interaction loop over detailed art or production systems.
- Keep every implementation within prototype scope unless the user explicitly expands it.

## Core Experience

The prototype must support this end-to-end loop:

1. The user views and explores the store from a first-person perspective.
2. The user moves through the store.
3. The user focuses or selects a product such as a shoe, bag, or garment.
4. The focused product receives clear visual feedback.
5. The user picks up the product, rotates it to inspect it, and releases it.
6. On release or deselection, interaction state and visual feedback reset cleanly.

Use the simplest clear feedback suitable for a prototype, such as a color, material, emission, outline, or small scale change. Only one item should own the active focus unless a task explicitly requires otherwise.

## Prototype Scope

- Prefer Unity primitives and simple materials for the store, fixtures, and products. Cubes, capsules, or cylinders are acceptable stand-ins for shoes, bags, and clothing; a cylinder alone is sufficient when it proves the interaction.
- Favor a small, repeatable, stable interaction loop over content quantity or visual fidelity.
- Do not add realistic product models, detailed architecture, complex shaders, advanced animation, or large asset packs unless explicitly requested.
- Treat checkout, inventory, accounts, networking, backends, NPCs, multiplayer, virtual fitting, body tracking, analytics, localization, and save systems as out of scope.
- Do not build speculative infrastructure such as a custom dependency-injection framework, service locator, event bus, or Addressables setup for future use.
- Avoid premature optimization. Fix measured prototype problems first.

## Current Project Constraints

- Target the project's existing Unity version: `6000.6.4f1`.
- Preserve the existing Universal Render Pipeline setup (`com.unity.render-pipelines.universal` 17.6.x).
- Use the installed Input System; do not mix it with the legacy `Input` API.
- Do not assume an XR runtime is configured. At the time this guide was written, XR Interaction Toolkit, XR Plug-in Management, and OpenXR were not listed in `Packages/manifest.json`.
- Before implementing device-dependent VR behavior, inspect the current packages, Project Settings, input actions, target device, and scene configuration. An input action with an XR binding does not by itself prove that an XR runtime is ready.
- Add or update packages only when the requested feature requires it. Keep additions minimal and report the package, configuration, and target-device assumptions.
- Do not directly edit generated or local-state directories such as `Library/`, `Temp/`, `Logs/`, `obj/`, or `UserSettings/`, including `Library/PackageCache/`.
- Do not copy or modify Unity package source or third-party package code for ordinary project features.

Place new project-owned assets under a feature-oriented root when practical:

```text
Assets/ShoppingVR/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
  Tests/
```

Do not reorganize existing assets merely to match this layout.

## First-Person VR and Interaction Rules

### Camera and Locomotion

- The camera represents the user's first-person viewpoint.
- Preserve HMD position and rotation tracking. Move the player/XR origin or rig root; never overwrite the tracked camera transform every frame.
- Keep rig movement separate from the camera's local tracked pose to avoid jumps and unintended offsets.
- Use a simple, predictable locomotion method appropriate to the current setup. Keep movement and rotation speeds conservative.
- Prefer snap turning as the prototype default when a turning mode is needed; treat smooth turning as optional unless requested.
- Provide enough floor, wall, collision, and boundary handling to prevent obvious invalid movement.

### Interaction State

- Model interaction state explicitly. A useful minimal progression is `Idle -> Hover/Focus -> Selected/Grabbed -> Released`.
- Define precedence when focus, selection, grabbing, and rotation overlap so two systems do not fight over the same item.
- Clear state safely when input is canceled or an item is disabled, destroyed, released, or deselected.
- Keep a headset-free Editor test path for core logic when practical, but do not turn desktop controls into a second product feature.

### Grabbing and Releasing

- A grabbed item must follow the hand or hold point stably.
- Prevent held items from clipping deeply into the camera or pushing the player unexpectedly.
- If code changes a `Rigidbody`, `Collider`, parent, layer, or transform state during a grab, preserve the necessary prior values and restore them consistently on release.
- Choose one predictable release behavior per task: stable physics placement or return to a defined display position.

### Rotation

- Allow the active selected or grabbed item to rotate intentionally for inspection.
- Use a natural product pivot and make rotation frame-rate independent.
- Do not continue rotating without input.
- Resolve input conflicts between locomotion, turning, grabbing, and item rotation explicitly.

## Unity C# Conventions

### Required Class Prefix

- Every newly created project C# class must begin with the exact prefix `CS_`.
- This applies to `MonoBehaviour`, `ScriptableObject`, plain C# classes, editor classes, test classes, structs, and enums created for this project.
- The source filename must exactly match its primary Unity class name. Example: `CS_ItemInteractable` belongs in `CS_ItemInteractable.cs`.
- Do not rename existing Unity, package, third-party, or project classes only to add the prefix; such renames can break serialized references. Rename existing code only when the task explicitly calls for that migration.

### Naming and Formatting

- Use `PascalCase` for types, methods, properties, enum members, constants, and public members.
- Use `_camelCase` for private and protected instance fields.
- Use `camelCase` for parameters and local variables.
- Prefer boolean names beginning with `Is`, `Has`, `Can`, or `Should` when appropriate.
- Prefer descriptive names over abbreviations.
- Follow the formatting already established in nearby project code. Keep one primary type per file where Unity serialization expects it.

### Serialization and Dependencies

- Prefer `[SerializeField] private` over public fields used only for Inspector configuration.
- Expose write access only when another component genuinely needs it.
- Cache component references during initialization instead of repeatedly searching for them.
- Validate required references early. Use `[RequireComponent]` when the dependency is truly mandatory and stable.
- Report missing required references clearly without logging the same error every frame.
- Give Inspector values explicit units in their names or tooltips, such as seconds, meters, or degrees per second.

### Lifecycle, Input, and Performance

- Use `Awake` for local reference initialization, `Start` for initialization that depends on other objects, `Update` for frame input/state, and `FixedUpdate` for physics work when appropriate.
- Register input actions and events in `OnEnable` and unregister them symmetrically in `OnDisable`.
- Avoid repeated `Find`, `GetComponent`, LINQ, and avoidable allocations in `Update` or `FixedUpdate`.
- Use `Time.deltaTime` or the appropriate time step for frame-rate-independent behavior.
- Move physics objects through consistent `Rigidbody` and `Collider` rules rather than mixing transform writes with physics unpredictably.
- Keep editor-only APIs in an `Editor` folder or editor-only assembly.

### Design Discipline

- Give each component one clear responsibility, while avoiding unnecessary interfaces and abstraction layers in a prototype.
- Separate input collection, interaction state, visual feedback, and item motion only where doing so makes behavior easier to test or maintain.
- Replace magic numbers with named constants or serialized tuning fields.
- Add comments for non-obvious reasons, constraints, and tradeoffs, not to restate the code.
- Avoid new global singleton or static mutable state unless the requested feature clearly needs it.
- Use meaningful GameObject, layer, tag, prefab, and asset names; minimize hard-coded string identifiers.

## Scenes, Prefabs, and Assets

- Inspect the relevant scene, prefab, and existing components before editing them.
- Preserve `.meta` files. Move or rename an asset together with its `.meta` file.
- Prefer Unity Editor-supported changes for scenes and prefabs. If YAML must be edited directly, verify GUIDs and serialized references carefully.
- Do not overwrite unrelated scene settings or user work.
- Name prototype objects and materials by purpose, even when their geometry is primitive.
- Ensure temporary runtime objects do not persistently dirty the scene after Play Mode.

## Working Procedure

1. Inspect the Unity version, packages, input setup, target scene, and relevant existing scripts before changing anything.
2. Reduce the request to the smallest useful slice of the core interaction loop.
3. Reuse existing systems and change only what the task requires.
4. Keep package, project-setting, and scene-wide changes minimal and deliberate.
5. Check compilation, missing references, state cleanup, and relevant interaction behavior.
6. Report changed files, implemented behavior, validation performed, device assumptions, and any remaining limitations.

## Definition of Done

A relevant prototype task is complete only when all applicable conditions below are met:

- The project opens and the changed scripts compile without Console errors.
- The target scene enters Play Mode.
- The user has a first-person view of the basic store environment.
- Locomotion works within the requested setup.
- At least one primitive product completes the applicable focus, grab, rotate, release, and feedback-reset flow.
- Cancellation, deselection, disable, and destruction do not leave stale interaction state or produce exceptions.
- Every new project class and matching filename uses the `CS_` prefix.
- The result remains a simple primitive-based prototype and does not damage unrelated assets, scenes, input configuration, or project settings.
- Pure state logic uses Edit Mode tests when worthwhile; integrated interactions use Play Mode tests when practical.
- Validation is reported honestly. Distinguish `Editor verified` from `device verified`, and state `device verification required` when no headset test was performed.
