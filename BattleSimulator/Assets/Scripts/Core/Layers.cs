#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using UnityEngine;

namespace Core
{
    public static class Layers
    {
        // Physics Layer
        public static readonly int Default = LayerMask.NameToLayer("Default");
        public static readonly int TransparentFX = LayerMask.NameToLayer("TransparentFX");
        public static readonly int IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        public static readonly int Water = LayerMask.NameToLayer("Water");
        public static readonly int UI = LayerMask.NameToLayer("UI");

        // Masks
        public static readonly int UIMask = 1 << UI;

        public static void ChangeLayer(GameObject gameObject, int layer, bool withChildren = true)
        {
            gameObject.layer = layer;
            if (withChildren)
            {
                Transform transform = gameObject.transform;
                for (int i = 0; i < transform.childCount; i++)
                    ChangeLayer(transform.GetChild(i).gameObject, layer);
            }
        }
    }
}