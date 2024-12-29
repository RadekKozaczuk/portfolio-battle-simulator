#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups.Views
{
    [DisallowMultipleComponent]
    class SettingsPopup : AbstractPopup
    {
        [SerializeField]
        TextMeshProUGUI _musicVolumeText;

        [SerializeField]
        TextMeshProUGUI _soundVolumeText;

        [SerializeField]
        Slider _musicSlider;

        [SerializeField]
        Slider _soundSlider;

        [SerializeField]
        Button _back;

        SettingsPopup()
            : base(PopupType.Settings) { }

        internal override void Initialize()
        {
            base.Initialize();

            _musicSlider.onValueChanged.AddListener(_ => _musicVolumeText.text = ((int)_musicSlider.value).ToString());
            _soundSlider.onValueChanged.AddListener(_ => _soundVolumeText.text = ((int)_soundSlider.value).ToString());
            _back.onClick.AddListener(Back);
        }

        static void Back() => PopupService.CloseCurrentPopup();
    }
}
