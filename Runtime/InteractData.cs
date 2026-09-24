using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem
{
    /// <summary>The shape of detection.</summary>
    public enum RaycastType
    {
        /// <summary>A ray <c>Radius</c> long. The first thing it hits decides - walls block it. Suited to first-person games.</summary>
        Line,

        /// <summary>A sphere of <c>Radius</c> around the origin; the closest interactable inside wins. Suited to third-person games.</summary>
        Sphere
    }

    /// <summary>One interactable prefab with its name and hold settings, written onto its <see cref="Interactable"/> by Compile.</summary>
    [Serializable]
    public sealed class InteractEntry
    {
        [Tooltip("The object's name, readable at runtime as Interactable.DisplayName (e.g. for UI). Empty = the prefab's name.")]
        [SerializeField] private string displayName;

        [SerializeField] private GameObject prefab;

        [Tooltip("Interact must be held for Duration seconds instead of just pressed.")]
        [SerializeField] private bool holding;

        [Tooltip("Seconds Interact must be held. Used only when Holding is on.")]
        [Min(0f)] [SerializeField] private float duration = 1f;

        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public bool Holding => holding;
        public float Duration => duration;
    }

    /// <summary>
    /// A named group of <see cref="InteractEntry"/> items. Its <see cref="Header"/> is shown in front of
    /// every object's name in the group (e.g. "Open" + "Door" = "Open Door"); leave it empty to show the
    /// name only.
    /// </summary>
    [Serializable]
    public sealed class InteractGroup
    {
        [Tooltip("Shown in front of every object's name in this group, e.g. \"Open\" -> \"Open Door\". Empty = name only.")]
        [SerializeField] private string header = string.Empty;

        [SerializeField] private List<InteractEntry> interactables = new();

        public string Header => header;

        /// <summary>The prefabs in this group, with their names and hold settings.</summary>
        public List<InteractEntry> Interactables => interactables;
    }

    /// <summary>Detection settings, shown as the "Interact Settings" block of an <see cref="InteractData"/>.</summary>
    [Serializable]
    public sealed class InteractSettings
    {
        [SerializeField] private RaycastType raycastType = RaycastType.Line;

        [Tooltip("Detection starts from the object with this tag (e.g. the player or the camera).")]
        [SerializeField] private string originTag = "Player";

        [Tooltip("Where detection starts, relative to the origin object (its local space).")]
        [SerializeField] private Vector3 localPosition;

        [Tooltip("Which way detection points, in degrees relative to the origin object's rotation.")]
        [SerializeField] private Vector3 localRotation;

        [Tooltip("Start detection from one of the tagged object's children instead of the object itself.")]
        [SerializeField] private bool inChild;

        [Tooltip("Index of the child detection starts from. Used only when In Child is on.")]
        [Min(0)] [SerializeField] private int childCount;

        [Tooltip("Line: the length of the ray. Sphere: the radius of the sphere.")]
        [Range(0.1f, 5f)] [SerializeField] private float radius = 3f;

        [Tooltip("Seconds between detection checks.")]
        [Range(0.01f, 1f)] [SerializeField] private float checkInterval = 0.05f;

        [Tooltip("Only objects on these layers are detected. With Line, anything else in front of them blocks detection.")]
        [SerializeField] private LayerMask raycastLayers;

        public RaycastType RaycastType => raycastType;
        public string OriginTag => originTag;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalRotation => localRotation;
        public bool InChild => inChild;
        public int ChildCount => childCount;
        public float Radius => radius;
        public float CheckInterval => checkInterval;

        public LayerMask RaycastLayers
        {
            get => raycastLayers;
            set => raycastLayers = value;
        }
    }

    /// <summary>
    /// Everything the interaction system needs: general (detection) settings on top, the objects that
    /// should be interactable below. Press "Compile" to give every listed prefab an
    /// <see cref="Interactable"/> component (only if it doesn't have one), a <see cref="BoxCollider"/>
    /// (only if it has no collider), the <c>Interact</c> layer, its group header, name and hold settings. At runtime,
    /// hand this asset to <see cref="InteractionManager.Initialize"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Interaction System/Interact Data", fileName = "NewInteractData")]
    public sealed class InteractData : ScriptableObject
    {
        /// <summary>The layer Compile assigns to every interactable.</summary>
        public const string LayerName = "Interact";

        [SerializeField] private InteractSettings generalSettings = new();

        [SerializeField] private List<InteractGroup> groups = new() { new InteractGroup() };

        /// <summary>Detection settings.</summary>
        public InteractSettings Settings => generalSettings;

        /// <summary>The groups of prefabs Compile makes interactable.</summary>
        public List<InteractGroup> Groups => groups;

        private void Reset()
        {
            generalSettings.RaycastLayers = LayerMask.GetMask(LayerName);
        }
    }
}
