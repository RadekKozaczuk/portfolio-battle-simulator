#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core.Enums;
using Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups.Views
{
    [DisallowMultipleComponent]
    class VictoryPopup : AbstractPopup
    {
        [SerializeField]
        TextMeshProUGUI _title;

        [SerializeField]
        TextMeshProUGUI _message;

        [SerializeField]
        Button _back;

        VictoryPopup()
            : base(PopupType.Victory) { }

        internal override void Initialize()
        {
            base.Initialize();

            _back.onClick.AddListener(Back);
        }

        internal void Initialize(int armiesLeft)
        {
            if (armiesLeft == 0)
            {
                _title.text = "Tie!";
                _message.text = "No units left.";
            }
            else if (armiesLeft == 1)
            {
                _title.text = "Victory!";
                _message.text = "Congratulations";
            }
            else
            {
                throw new Exception("Invalid number of armies.");
            }
        }

        static void Back()
        {
            PopupService.CloseCurrentPopup();
            GameStateService.ChangeState(GameState.MainMenu);
        }
    }
}
