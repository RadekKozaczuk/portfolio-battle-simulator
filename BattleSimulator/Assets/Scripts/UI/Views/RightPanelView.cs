#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine.UI;
using UnityEngine;
using Core.Enums;
using Core.Services;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Views
{
    [DisallowMultipleComponent]
    class RightPanelView : MonoBehaviour
    {
        [SerializeField]
        Button _start;

        [SerializeField]
        Button _quit;

        void Awake()
        {
            _start.onClick.AddListener(Start);
            _quit.onClick.AddListener(Quit);
        }

        static void Start()
        {
            GameStateService.ChangeState(GameState.Gameplay);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}