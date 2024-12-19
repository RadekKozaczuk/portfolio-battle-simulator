#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Core.Enums;
using UI.Config;
using UI.Popups.Views;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI.Popups
{
    /// <summary>
    /// Popup systems automatically calls SetActive(true) on each instantiated popup. It is a good practice to make popup
    /// prefabs inactive so that all the changes done to the prefab during <see cref="AbstractPopup.Initialize" /> call are not visible to the player.
    /// </summary>
    static class PopupSystem
    {
        // ReSharper disable once MemberCanBePrivate.Global
        internal static AbstractPopup? CurrentPopup => Popups.Count > 0 ? Popups[0] : null;

        internal static readonly List<AbstractPopup> Popups = new();

        static Image? _blockingPanel;
        static readonly PopupConfig _config;
        static readonly UIConfig _uiConfig;

        public static void ShowPopup(PopupType popupType, bool blockingPanel = true) =>
            InstantiatePopup(_config.PopupPrefabs[(int)popupType], blockingPanel);

        public static void CloseCurrentPopup()
        {
            Assert.IsFalse(CurrentPopup == null, "You cannot call CloseCurrentPopup if there is no active popup.");

            CurrentPopup!.Close();
            GameObject popupGo = CurrentPopup.gameObject;
            popupGo.SetActive(false);
            Object.Destroy(popupGo);
            Popups.RemoveAt(0);

            if (_blockingPanel == null)
                return;

            if (Popups.Count > 0)
            {
                _blockingPanel.transform.SetSiblingIndex(Popups.Count - 1);
            }
            else
            {
                GameObject panelGo = _blockingPanel.gameObject;
                panelGo.SetActive(false);
                Object.Destroy(panelGo);
                _blockingPanel = null;
            }
        }

        static void InstantiatePopup(AbstractPopup prefab, bool blockingPanel)
        {
            if (blockingPanel && _blockingPanel == null)
                _blockingPanel = Object.Instantiate(_config.BlockingPanelPrefab, UISceneReferenceHolder.PopupContainer);

            AbstractPopup popup = Object.Instantiate(prefab, UISceneReferenceHolder.PopupContainer)!;

            popup.Initialize();
            popup.gameObject.SetActive(true);
            Popups.Insert(0, popup);

            // blocking panel should be always second from bottom
            if (_blockingPanel != null)
                _blockingPanel.transform.SetSiblingIndex(Popups.Count - 1);
        }
    }
}