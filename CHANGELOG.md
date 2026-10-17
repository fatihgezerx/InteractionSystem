# Changelog

## Unreleased

### Editor
- `Interactable` Inspector is now drawn from a **default Veneer layout** (`InteractableDefaultLayout`: a Prompt card and an Events card; system items in `InteractableItems` for the Press / Hold pill, the prompt hints and the collider / layer checks, the last one in the footer so it sits under the cards as before). The editor derives from `VeneerLayoutDrivenInspector<Interactable>`; customisable in the Inspector Designer. Differences: the checks are a footer item listed in the Events card of the layout. Without Veneer nothing changes. Not run in Unity yet.
- `InteractData` Inspector is now drawn from a **default Veneer layout** (`InteractDataDefaultLayout`: the same three tabs, cards and fields, notes with show-if rules instead of computed hints, system items for the state pill, the checks, the Outline System check and the Compile button in `InteractDataItems`, the prefab list as a custom widget). The editor class derives from `VeneerLayoutDrivenInspector<InteractData>`; a project can customise the design in the Inspector Designer (Customize / Reset to system default). Differences from the hand-written editor: the header title is the fixed text "Interact Data" instead of the asset name, and the Shape buttons show icons with the explanation in the row tooltip and notes. Without Veneer nothing changes. Not run in Unity yet.
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
