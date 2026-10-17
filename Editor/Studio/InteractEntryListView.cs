using System.Collections.Generic;
using Veneer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem
{
    /// <summary>
    /// The list of <see cref="InteractEntry"/> rows of an <see cref="InteractData"/> in the studio look: a header with the item count
    /// and add / remove buttons, then a reorderable bound list whose rows show the prefab (with thumbnail), the name, a "hold to
    /// interact" chip and, while holding is on, the duration. Each row carries a state pill (compiled, needs compile, no prefab...)
    /// refreshed by one 300 ms scheduler that only runs while the list is on a panel. This replaces the old IMGUI
    /// <c>InteractEntryDrawer</c>; it edits the same serialized fields, so nothing about the saved data changes.
    /// </summary>
    internal sealed class InteractEntryListView : VisualElement
    {
        private const long TickMilliseconds = 300;
        private const string PrefabFallbackName = "Prefab's name";

        private readonly SerializedProperty entries;
        private readonly VeneerBrand brand;
        private readonly ListView list;
        private readonly Label countPill;
        private readonly List<RowState> rows = new List<RowState>(16);
        private IVisualElementScheduledItem tick;
        private int shownCount = -1;

        /// <summary>Makes the list and binds it to the serialized object of <paramref name="entriesProperty"/>.</summary>
        /// <param name="entriesProperty">The <c>interactables</c> list property.</param>
        /// <param name="brand">Brand for icon lookup.</param>
        public InteractEntryListView(SerializedProperty entriesProperty, VeneerBrand brand)
        {
            entries = entriesProperty.Copy();
            this.brand = brand;
            AddToClassList("vn-list");

            var header = new VisualElement();
            header.AddToClassList("vn-list__header");
            var title = new Label("Interactable prefabs");
            title.AddToClassList("vn-list__title");
            header.Add(title);
            countPill = VeneerStyle.Pill("0", "info");
            header.Add(countPill);
            header.Add(VeneerStyle.Spacer());
            Button add = VeneerStyle.IconButton("plus", "Add an interactable at the end.", null, brand);
            Button remove = VeneerStyle.IconButton("minus", "Remove the selected interactables (the last one when none is selected).", null, brand);
            add.clicked += AddEntry;
            remove.clicked += RemoveEntries;
            header.Add(add);
            header.Add(remove);
            Add(header);

            list = new ListView
            {
                bindingPath = entries.propertyPath,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Multiple,
                showAddRemoveFooter = false,
                showFoldoutHeader = false,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            };
            list.AddToClassList("vn-list__view");
            list.makeItem = MakeItem;
            list.bindItem = BindItem;
            list.unbindItem = UnbindItem;
            Add(list);
            list.Bind(entries.serializedObject);

            // Pills and the count are refreshed on a slow tick, and only while the list is on a panel.
            tick = schedule.Execute(Tick).Every(TickMilliseconds);
            RegisterCallback<AttachToPanelEvent>(evt => tick.Resume());
            RegisterCallback<DetachFromPanelEvent>(evt => tick.Pause());
        }

        private sealed class RowState
        {
            public VisualElement Body;
            public TextField NameField;
            public Label Pill;
            public int Index = -1;
            public int Status = -1;
            public string Placeholder;
        }

        private VisualElement MakeItem()
        {
            var body = new VisualElement();
            body.style.paddingLeft = 6f;
            body.style.paddingRight = 6f;
            body.style.paddingTop = 4f;
            body.style.paddingBottom = 4f;
            body.style.flexShrink = 0f;
            body.userData = new RowState { Body = body };
            rows.Add((RowState)body.userData);
            return body;
        }

        // The controls of a row are rebuilt on every bind: they are bound to the element at this index, and a rebuilt control cannot
        // keep a stale property path after a reorder or a removal.
        private void BindItem(VisualElement element, int index)
        {
            var state = (RowState)element.userData;
            state.Index = index;
            state.Status = -1;
            state.Placeholder = null;
            element.Clear();
            if (index < 0 || index >= entries.arraySize)
            {
                return;
            }

            SerializedProperty entry = entries.GetArrayElementAtIndex(index);

            element.Add(VeneerControls.ObjectSlot(entry.FindPropertyRelative("prefab"), "Prefab", typeof(GameObject), brand));

            SerializedProperty nameProperty = entry.FindPropertyRelative("displayName");
            var nameField = new TextField();
            nameField.BindProperty(nameProperty);
            nameField.tooltip = nameProperty.tooltip;
            nameField.style.flexGrow = 1f;
            nameField.style.flexShrink = 1f;
            nameField.style.marginLeft = 0f;
            nameField.style.marginRight = 0f;
            Label pill = VeneerStyle.Pill(string.Empty, "idle");
            VisualElement nameControl = VeneerStyle.Row();
            nameControl.Add(nameField);
            nameControl.Add(pill);
            element.Add(VeneerStyle.FieldRow("Name", nameControl, nameProperty.tooltip, out _));
            state.NameField = nameField;
            state.Pill = pill;

            SerializedProperty holding = entry.FindPropertyRelative("holding");
            VisualElement holdRow = VeneerStyle.Row(true);
            holdRow.Add(VeneerControls.ToggleChip(holding, "Hold to interact", "clock", brand));
            element.Add(holdRow);

            VisualElement duration = VeneerControls.NumberRow(entry.FindPropertyRelative("duration"), "Duration", "s");
            VeneerStyle.SetDisplay(duration, holding.boolValue);
            SerializedProperty watchedHolding = holding.Copy();
            duration.TrackPropertyValue(watchedHolding, changed => VeneerStyle.SetDisplay(duration, changed.boolValue));
            element.Add(duration);

            Refresh(state, entries.serializedObject.targetObject as InteractData);
        }

        private void UnbindItem(VisualElement element, int index)
        {
            var state = (RowState)element.userData;
            state.Index = -1;
            state.NameField = null;
            state.Pill = null;
            element.Clear();
        }

        private void AddEntry()
        {
            SerializedObject serializedObject = entries.serializedObject;
            serializedObject.Update();
            int index = entries.arraySize;
            entries.arraySize = index + 1;

            // Growing a list copies the last element: give the new one fresh defaults instead.
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("displayName").stringValue = string.Empty;
            entry.FindPropertyRelative("prefab").objectReferenceValue = null;
            entry.FindPropertyRelative("holding").boolValue = false;
            entry.FindPropertyRelative("duration").floatValue = 1f;
            serializedObject.ApplyModifiedProperties();
            list.RefreshItems();
            list.ScrollToItem(index);
        }

        private void RemoveEntries()
        {
            SerializedObject serializedObject = entries.serializedObject;
            serializedObject.Update();
            var indices = new List<int>(list.selectedIndices);
            if (indices.Count == 0 && entries.arraySize > 0)
            {
                indices.Add(entries.arraySize - 1);
            }

            indices.Sort((a, b) => b.CompareTo(a));
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index >= 0 && index < entries.arraySize)
                {
                    entries.DeleteArrayElementAtIndex(index);
                }
            }

            serializedObject.ApplyModifiedProperties();
            list.ClearSelection();
            list.RefreshItems();
        }

        private void Tick()
        {
            if (panel == null)
            {
                return;
            }

            var data = entries.serializedObject.targetObject as InteractData;
            if (data == null)
            {
                return;
            }

            int count = data.Interactables.Count;
            if (count != shownCount)
            {
                shownCount = count;
                VeneerStyle.SetPill(countPill, "info", count.ToString());
            }

            for (int i = 0; i < rows.Count; i++)
            {
                RowState state = rows[i];
                if (state.Index >= 0 && state.Body.panel != null)
                {
                    Refresh(state, data);
                }
            }
        }

        // Updates the state pill and the name placeholder (the prefab's name, which is what an empty Name falls back to).
        private static void Refresh(RowState state, InteractData data)
        {
            if (data == null || state.Pill == null || state.Index < 0 || state.Index >= data.Interactables.Count)
            {
                return;
            }

            InteractEntry entry = data.Interactables[state.Index];
            var status = (int)InteractCompiler.GetStatus(data, state.Index);
            if (status != state.Status)
            {
                state.Status = status;
                switch ((InteractEntryStatus)status)
                {
                    case InteractEntryStatus.NoPrefab:
                        VeneerStyle.SetPill(state.Pill, "warn", "No prefab");
                        break;
                    case InteractEntryStatus.NotPrefabAsset:
                        VeneerStyle.SetPill(state.Pill, "error", "Not a prefab");
                        break;
                    case InteractEntryStatus.Duplicate:
                        VeneerStyle.SetPill(state.Pill, "warn", "Duplicate");
                        break;
                    case InteractEntryStatus.NeedsCompile:
                        VeneerStyle.SetPill(state.Pill, "warn", "Needs Compile");
                        break;
                    default:
                        VeneerStyle.SetPill(state.Pill, "ok", "Compiled");
                        break;
                }
            }

            GameObject prefab = entry != null ? entry.Prefab : null;
            string wanted = prefab != null ? prefab.name : PrefabFallbackName;
            if (state.NameField != null && !string.Equals(state.Placeholder, wanted, System.StringComparison.Ordinal))
            {
                state.Placeholder = wanted;
                state.NameField.textEdition.placeholder = wanted;
            }
        }
    }
}
