# Changelog

## Unreleased

### Editor
- Studio-look Inspectors on top of the optional Veneer (amber accent, brand images in `Editor/Brand`):
  - `InteractData`: header card with a state pill, tabs Interactables / Detection / Origin, a custom prefab list with a
    thumbnail, name, hold chip and duration per row, a state pill per row (Compiled, Needs Compile, No prefab, Not a prefab,
    Duplicate), validation hints, an Outline card and a primary Compile button. The old `InteractEntryDrawer` is folded
    into it.
  - `Interactable`: Press / Hold pill, read-only prompt, the three events, collider and layer hints.
- Without Veneer the default Inspectors are used (`DependencyGuard` lists the kit as an optional dependency,
  define `HAS_VENEER`). The new `InteractionSystem.Editor.Studio` assembly is gated by that define.
- The Compile step moved out of the Inspector into `InteractCompiler` (same behaviour) so it also works without the kit,
  through the context menu of an Interact Data (`Compile`). A model asset (FBX) in the list is now skipped with a warning
  instead of failing.
