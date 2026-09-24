using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace InteractionSystem
{
    /// <summary>
    /// Custom Inspector for <see cref="InteractData"/>: an INTERACT SETTINGS box (with In Child and
    /// Child Count side by side), an INTERACTABLES box of headed groups below it, and a "Compile" button
    /// that gives every listed prefab an <see cref="Interactable"/> (only if missing), a
    /// <see cref="BoxCollider"/> (only if it has no collider), the <c>Interact</c> layer (created in the
    /// Tag Manager if missing), its group header, name and hold settings. Prefabs that are already up
    /// to date are skipped, not re-saved.
    /// </summary>
    [CustomEditor(typeof(InteractData))]
    internal sealed class InteractDataEditor : Editor
    {
        private const float HeaderHeight = 28f;
        private const float GroupHeaderHeight = 22f;

        private const string GroupInfo =
            "When naming groups, keep in mind that the group name is shown in front of the object's name " +
            "(e.g. \"Open\" + \"Door\" = \"Open Door\"). Leave a group name empty to show only the object's name.";

        // Same green as PoolData's Compile button.
        private static readonly Color CompileButtonColor = new(0.4f, 0.75f, 0.4f);

        private SerializedProperty _settings;
        private SerializedProperty _groups;
        private readonly List<ReorderableList> _groupLists = new();
        private readonly GroupDragReorder _groupReorder = new();
        private GUIStyle _headerStyle;
        private GUIStyle _groupHeaderStyle;
        private GUIStyle _hintStyle;

        // Same style as PoolData's group headers.
        private GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 20,
            fixedHeight = HeaderHeight
        };

        private GUIStyle GroupHeaderStyle => _groupHeaderStyle ??= new GUIStyle(EditorStyles.textField)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            fixedHeight = GroupHeaderHeight,
            alignment = TextAnchor.MiddleLeft
        };

        private GUIStyle HintStyle => _hintStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 0, 0, 0),
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };

        private void OnEnable()
        {
            _settings = serializedObject.FindProperty("generalSettings");
            _groups = serializedObject.FindProperty("groups");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawInteractSettings();
            EditorGUILayout.Space(10);
            DrawInteractables();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(14);

            var buttonColor = GUI.backgroundColor;
            GUI.backgroundColor = CompileButtonColor;
            if (GUILayout.Button("Compile", GUILayout.Height(34)))
            {
                Compile((InteractData)target);
            }
            GUI.backgroundColor = buttonColor;
        }

        private void DrawInteractSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("INTERACT SETTINGS", HeaderStyle, GUILayout.Height(HeaderHeight));
            EditorGUILayout.Space(4);

            var originTag = _settings.FindPropertyRelative("originTag");

            EditorGUILayout.PropertyField(_settings.FindPropertyRelative("raycastType"));
            originTag.stringValue = EditorGUILayout.TagField(new GUIContent("Origin Tag", originTag.tooltip), originTag.stringValue);
            EditorGUILayout.PropertyField(_settings.FindPropertyRelative("localPosition"));
            EditorGUILayout.PropertyField(_settings.FindPropertyRelative("localRotation"));
            DrawToggleWithValue(EditorGUILayout.GetControlRect(),
                _settings.FindPropertyRelative("inChild"), "In Child",
                _settings.FindPropertyRelative("childCount"), "Child Count");
            EditorGUILayout.PropertyField(_settings.FindPropertyRelative("radius"));
            EditorGUILayout.PropertyField(_settings.FindPropertyRelative("checkInterval"));
            EditorGUILayout.PropertyField(_settings.FindPropertyRelative("raycastLayers"));

            EditorGUILayout.EndVertical();
        }

        private void DrawInteractables()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("INTERACTABLES", HeaderStyle, GUILayout.Height(HeaderHeight));
            EditorGUILayout.HelpBox(GroupInfo, MessageType.Info);
            EditorGUILayout.Space(6);

            // Lists address their group by index, so rebuild them whenever groups are added or removed.
            if (_groupLists.Count != _groups.arraySize)
            {
                _groupLists.Clear();
                for (var i = 0; i < _groups.arraySize; i++)
                {
                    _groupLists.Add(CreateEntryList(_groups.GetArrayElementAtIndex(i).FindPropertyRelative("interactables")));
                }
            }

            var groupPendingRemoval = -1;
            _groupReorder.Begin();
            for (var i = 0; i < _groups.arraySize; i++)
            {
                if (DrawGroup(i))
                {
                    groupPendingRemoval = i;
                }

                _groupReorder.RecordGroupRect();
                EditorGUILayout.Space(6);
            }

            if (_groupReorder.End(out var from, out var to))
            {
                _groups.MoveArrayElement(from, to);
                _groupLists.Clear();
            }
            else if (groupPendingRemoval >= 0)
            {
                _groups.DeleteArrayElementAtIndex(groupPendingRemoval);
                _groupLists.Clear();
            }

            if (GUILayout.Button("+ Add Group", GUILayout.Height(28)))
            {
                // A new array element copies the last one, so clear it.
                _groups.arraySize++;
                var group = _groups.GetArrayElementAtIndex(_groups.arraySize - 1);
                group.FindPropertyRelative("header").stringValue = string.Empty;
                group.FindPropertyRelative("interactables").arraySize = 0;
                _groupLists.Clear();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>Draws one group; returns true if its remove button was clicked.</summary>
        private bool DrawGroup(int index)
        {
            var header = _groups.GetArrayElementAtIndex(index).FindPropertyRelative("header");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _groupReorder.DrawHandle(index, GroupHeaderHeight);
            var headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(GroupHeaderHeight));
            header.stringValue = EditorGUI.TextField(headerRect, header.stringValue, GroupHeaderStyle);
            if (string.IsNullOrEmpty(header.stringValue))
            {
                EditorGUI.LabelField(headerRect, "Group name (empty = object name only)", HintStyle);
            }

            var remove = GUILayout.Button("✕", GUILayout.Width(GroupHeaderHeight), GUILayout.Height(GroupHeaderHeight));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            _groupLists[index].DoLayoutList();

            EditorGUILayout.EndVertical();
            return remove;
        }

        private ReorderableList CreateEntryList(SerializedProperty entries)
        {
            return new ReorderableList(serializedObject, entries, true, false, true, true)
            {
                elementHeightCallback = index =>
                    EditorGUI.GetPropertyHeight(entries.GetArrayElementAtIndex(index)) + 4f,
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.y += 2f;
                    rect.height -= 4f;
                    EditorGUI.PropertyField(rect, entries.GetArrayElementAtIndex(index), GUIContent.none);
                }
            };
        }

        /// <summary>A toggle on the left and a value on the right, greyed out unless the toggle is on.</summary>
        internal static void DrawToggleWithValue(Rect rect, SerializedProperty toggle, string toggleLabel,
            SerializedProperty value, string valueLabel)
        {
            var halfWidth = (rect.width - 8f) * 0.5f;
            var leftRect = new Rect(rect.x, rect.y, halfWidth, rect.height);
            var rightRect = new Rect(rect.x + halfWidth + 8f, rect.y, halfWidth, rect.height);

            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80f;

            EditorGUI.PropertyField(leftRect, toggle, new GUIContent(toggleLabel, toggle.tooltip));
            using (new EditorGUI.DisabledScope(!toggle.boolValue))
            {
                EditorGUI.PropertyField(rightRect, value, new GUIContent(valueLabel, value.tooltip));
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private static void Compile(InteractData data)
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
            foreach (var group in data.Groups)
            foreach (var entry in group.Interactables)
            {
                var prefab = entry?.Prefab;
                if (prefab == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning($"[InteractData] '{prefab.name}' is not a prefab asset; skipped.");
                    continue;
                }

                var header = group.Header?.Trim() ?? string.Empty;
                var displayName = string.IsNullOrWhiteSpace(entry.DisplayName) ? prefab.name : entry.DisplayName;

                if (IsUpToDate(prefab, entry, header, displayName, layer))
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
                    interactable.header = header;
                    interactable.displayName = displayName;
                    interactable.holding = entry.Holding;
                    interactable.holdDuration = entry.Duration;
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

        private static bool IsUpToDate(GameObject prefab, InteractEntry entry, string header, string displayName, int layer)
        {
            return prefab.TryGetComponent<Interactable>(out var interactable)
                   && prefab.TryGetComponent<Collider>(out _)
                   && prefab.layer == layer
                   && interactable.header == header
                   && interactable.displayName == displayName
                   && interactable.holding == entry.Holding
                   && Mathf.Approximately(interactable.holdDuration, entry.Duration);
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

    /// <summary>Draws an <see cref="InteractEntry"/> as Name, Prefab, then Holding and Duration side by side.</summary>
    [CustomPropertyDrawer(typeof(InteractEntry))]
    internal sealed class InteractEntryDrawer : PropertyDrawer
    {
        private const int LineCount = 3;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * LineCount + EditorGUIUtility.standardVerticalSpacing * (LineCount - 1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var nameProperty = property.FindPropertyRelative("displayName");
            var prefabProperty = property.FindPropertyRelative("prefab");

            EditorGUI.PropertyField(LineRect(position, 0), nameProperty, new GUIContent("Name", nameProperty.tooltip));
            EditorGUI.PropertyField(LineRect(position, 1), prefabProperty, new GUIContent("Prefab"));
            InteractDataEditor.DrawToggleWithValue(LineRect(position, 2),
                property.FindPropertyRelative("holding"), "Holding",
                property.FindPropertyRelative("duration"), "Duration");
        }

        private static Rect LineRect(Rect position, int index)
        {
            var line = EditorGUIUtility.singleLineHeight;
            return new Rect(position.x, position.y + index * (line + EditorGUIUtility.standardVerticalSpacing), position.width, line);
        }
    }
}
