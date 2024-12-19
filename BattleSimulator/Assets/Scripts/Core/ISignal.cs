// ReSharper disable UnusedMemberInSuper.Global

namespace Core
{
    public interface ISignal
    {
        /// <summary>
        /// Universal signal indicating that something has changed in the inventory.
        /// An item was added, removed, moved to a different slot, or its quantity has changed.
        /// </summary>
        void InventoryChanged();

        void MissionComplete();

        void MissionFailed();

        void ToggleMuteVoiceChat();
    }
}