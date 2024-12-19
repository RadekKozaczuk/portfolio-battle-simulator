#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    /// <summary>
    /// Unity created a lot of more restrictive types like <see cref="AssetReferenceSprite" /> or <see cref="AssetReferenceGameObject" />,
    /// but for some reason forgot about the <see cref="AudioClip" />. This class fixes that problem.
    /// </summary>
    [Serializable]
    public class AssetReferenceAudioClip : AssetReferenceT<AudioClip>
    {
        AssetReferenceAudioClip(string guid)
            : base(guid) { }
    }
}