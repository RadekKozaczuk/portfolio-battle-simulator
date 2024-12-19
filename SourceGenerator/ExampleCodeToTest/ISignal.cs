using System.Collections.Generic;
using System.Numerics;
using Some.Really.Long.Namespace;
using Core.Enums;

// ReSharper disable UnusedParameter.Global

namespace Core
{
    public interface ISignal
    {
        /// <summary>
        /// Universal signal indicating that something has changed in the inventory.
        /// An item was added, removed, moved to a different slot, or its quantity has changed.
        /// </summary>
        void InventoryChanged();

        /// <summary>
        /// Indicates that something has changed in the lobby.
        /// If LobbyName is different than null then the name changed.
        /// If Players list is different than null then something changed in one or more players.
        /// The list always represents the current state, and not only the change.
        /// </summary>
        void LobbyChanged(string lobbyName, string lobbyCode, List<(string playerName, string playerId, bool isHost)> players);

        void MissionComplete();
        void MissionFailed();
        void PlayerHpChangedSignal(int hp);
        void PlaySoundSignal(Vector3 position, Sound type);
        void PopupRequestedSignal(PopupType popupType);
        void SkillAddedSignal(SkillModel skill, int slot);
    }
}