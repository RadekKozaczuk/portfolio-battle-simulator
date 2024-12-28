#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core.Enums;
using UnityEngine;

namespace UI.Model
{
    [Serializable]
    internal class UnitModel
    {
        [SerializeField]
        internal string Name;

        [SerializeField]
        internal int Amount;

        [SerializeField]
        internal Strategy Strategy;
    }
}