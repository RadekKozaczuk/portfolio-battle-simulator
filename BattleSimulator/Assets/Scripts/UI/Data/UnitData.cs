#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core.Enums;
using UnityEngine;

namespace UI.Data
{
    [Serializable]
    internal class UnitData
    {
        [SerializeField]
        internal string Name;

        [Range(1, 100)]
        [SerializeField]
        internal int Amount;

        [SerializeField]
        internal Strategy Strategy;
    }
}