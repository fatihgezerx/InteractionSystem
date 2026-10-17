using UnityEditor;
using UnityEngine;

namespace InteractionSystem
{
    /// <summary>What <see cref="InteractCompiler.GetStatus"/> reports for one entry of an <see cref="InteractData"/>.</summary>
    public enum InteractEntryStatus
    {
        /// <summary>The entry has no prefab; Compile skips it.</summary>
        NoPrefab,

        /// <summary>The object is not a prefab asset (a scene object or a model file); Compile skips it.</summary>
        NotPrefabAsset,

        /// <summary>The same prefab is listed earlier in the list too.</summary>
        Duplicate,

        /// <summary>The prefab differs from what Compile would write; the next Compile updates it.</summary>
        NeedsCompile,

        /// <summary>The prefab already matches the entry.</summary>
        UpToDate
    }

    /// <summary>
    /// The "Compile" step of an <see cref="InteractData"/> (editor only, independent of any Inspector): gives every listed prefab an
    /// <see cref="Interactable"/> (only if missing), a <see cref="BoxCollider"/> (only if it has no collider), the <c>Interact</c>
    /// layer (created in the Tag Manager if missing), its name and hold settings. Prefabs that are already up to date are skipped,
    /// not re-saved. Also reachable from the context menu of an InteractData (the three dots of its Inspector), so it works
    /// without the studio Inspector too.
    /// </summary>
    public static class InteractCompiler
    {
        [MenuItem("CONTEXT/InteractData/Compile")]
        private static void CompileFromContextMenu(MenuCommand command)
        {
            if (command.context is InteractData data)
            {
                Compile(data);
            }
        }

        /// <summary>Compiles every prefab listed in <paramref name="data"/> and logs how many were updated.</summary>
        /// <param name="data">The asset to compile.</param>
        public static void Compile(InteractData data)
        {
            var layer = EnsureInteractLayer();
            if (layer < 0)
            {
                Debug.LogError($"[InteractData] No free user layer left for '{InteractData.LayerName}'. " +
                                "Free one in Project Settings > Tags and Layers. Compile aborted.");
                return;
            }

            var updated = 0;
            var upToDate = 0;
            foreach (var entry in data.Interactables)
            {
                var prefab = entry?.Prefab;
                if (prefab == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(path) || IsModelAsset(prefab))
                {
                    Debug.LogWarning($"[InteractData] '{prefab.name}' is not a prefab asset; skipped.");
                    continue;
                }

                var displayName = string.IsNullOrWhiteSpace(entry.DisplayName) ? prefab.name : entry.DisplayName;

                if (IsUpToDate(data, prefab, entry, displayName, layer))
                {
                    upToDate++;
                    continue;
                }

                using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    var root = scope.prefabContentsRoot;

                    if (!root.TryGetComponent<Interactable>(out var interactable))
                    {
                        interactable = root.AddComponent<Interactable>();
                    }

                    if (!root.TryGetComponent<Collider>(out _))
                    {
                        root.AddComponent<BoxCollider>();
                    }

                    root.layer = layer;
                    interactable.displayName = displayName;
                    interactable.holding = entry.Holding;
                    interactable.holdDuration = entry.Duration;

#if HAS_OUTLINE_SYSTEM
                    if (data.Settings.ShowOutline)
                    {
                        if (!root.TryGetComponent<OutlineSystem.Outline>(out var outline))
                        {
                            outline = root.AddComponent<OutlineSystem.Outline>();
                        }

                        outline.Color = data.Settings.OutlineColor;
                        outline.Width = data.Settings.OutlineWidth;
                    }
#endif
                }

                updated++;
            }

            // Make sure detection can actually see what was just put on the Interact layer.
            var mask = data.Settings.RaycastLayers;
            if ((mask.value & (1 << layer)) == 0)
            {
                Undo.RecordObject(data, "Add Interact Layer To Raycast Layers");
                mask.value |= 1 << layer;
                data.Settings.RaycastLayers = mask;
                EditorUtility.SetDirty(data);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[InteractData] Compile finished: {updated} prefab(s) updated, {upToDate} already up to date.");
        }

        /// <summary>The state of one entry, without changing anything. Cheap enough to poll a few times a second.</summary>
        /// <param name="data">The asset the entry belongs to.</param>
        /// <param name="index">Index in <see cref="InteractData.Interactables"/>.</param>
        /// <returns>The entry's state.</returns>
        public static InteractEntryStatus GetStatus(InteractData data, int index)
        {
            var entries = data.Interactables;
            var entry = index >= 0 && index < entries.Count ? entries[index] : null;
            var prefab = entry?.Prefab;
            if (prefab == null)
            {
                return InteractEntryStatus.NoPrefab;
            }

            if (!EditorUtility.IsPersistent(prefab) || IsModelAsset(prefab))
            {
                return InteractEntryStatus.NotPrefabAsset;
            }

            for (var i = 0; i < index; i++)
            {
                if (entries[i] != null && entries[i].Prefab == prefab)
                {
                    return InteractEntryStatus.Duplicate;
                }
            }

            var displayName = string.IsNullOrWhiteSpace(entry.DisplayName) ? prefab.name : entry.DisplayName;
            var layer = LayerMask.NameToLayer(InteractData.LayerName);
            return IsUpToDate(data, prefab, entry, displayName, layer) ? InteractEntryStatus.UpToDate : InteractEntryStatus.NeedsCompile;
        }

        /// <summary>Counts the entries by state (an entry is counted once).</summary>
        /// <param name="data">The asset to look at.</param>
        /// <param name="needsCompile">Entries whose prefab differs from what Compile would write.</param>
        /// <param name="problems">Entries Compile skips (no prefab, not a prefab asset).</param>
        /// <param name="duplicates">Entries that list a prefab again.</param>
        /// <param name="upToDate">Entries that already match.</param>
        public static void Analyze(InteractData data, out int needsCompile, out int problems, out int duplicates, out int upToDate)
        {
            needsCompile = 0;
            problems = 0;
            duplicates = 0;
            upToDate = 0;
            for (var i = 0; i < data.Interactables.Count; i++)
            {
                switch (GetStatus(data, i))
                {
                    case InteractEntryStatus.NeedsCompile:
                        needsCompile++;
                        break;
                    case InteractEntryStatus.UpToDate:
                        upToDate++;
                        break;
                    case InteractEntryStatus.Duplicate:
                        duplicates++;
                        break;
                    default:
                        problems++;
                        break;
                }
            }
        }

        // A model file (FBX...) is a GameObject asset too, but it cannot be edited as prefab contents.
        private static bool IsModelAsset(GameObject asset)
        {
            var type = PrefabUtility.GetPrefabAssetType(asset);
            return type == PrefabAssetType.Model || type == PrefabAssetType.NotAPrefab;
        }

        private static bool IsUpToDate(InteractData data, GameObject prefab, InteractEntry entry, string displayName, int layer)
        {
            if (!prefab.TryGetComponent<Interactable>(out var interactable)
                || !prefab.TryGetComponent<Collider>(out _)
                || prefab.layer != layer
                || interactable.displayName != displayName
                || interactable.holding != entry.Holding
                || !Mathf.Approximately(interactable.holdDuration, entry.Duration))
            {
                return false;
            }

#if HAS_OUTLINE_SYSTEM
            if (data.Settings.ShowOutline)
            {
                if (!prefab.TryGetComponent<OutlineSystem.Outline>(out var outline)
                    || outline.Color != data.Settings.OutlineColor
                    || !Mathf.Approximately(outline.Width, data.Settings.OutlineWidth))
                {
                    return false;
                }
            }
#endif

            return true;
        }

        /// <summary>Returns the Interact layer's index, adding it to the first free user layer if missing; -1 if none is free.</summary>
        private static int EnsureInteractLayer()
        {
            var existing = LayerMask.NameToLayer(InteractData.LayerName);
            if (existing >= 0)
            {
                return existing;
            }

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");

            // 0-7 are Unity's built-in layers.
            for (var i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = InteractData.LayerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[InteractData] Added layer '{InteractData.LayerName}' at index {i}.");
                    return i;
                }
            }

            return -1;
        }
    }
}
