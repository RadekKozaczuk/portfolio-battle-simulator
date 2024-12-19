#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Core.Services
{
    /// <summary>
    /// In our architecture every project starts from BootScene.
    /// Designers, programmers, and artists alike occasionally jump between scenes while using the editor.
    /// Sometimes they may forget to come back to BootScene before pressing the Play button.
    /// For their convenience this script automates this process as, at least for now, there is zero reason to not start from BootScene.
    /// </summary>
    [InitializeOnLoad]
    static class BootSceneStartService
    {
        static BootSceneStartService()
        {
            // todo: probably should be awaited or maybe react on something as there are errors unfortunately
            var entryScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/BootScene.unity");
            if (entryScene == null)
                Debug.LogError("BootScene not be found."); // todo: this is sometimes thrown for no reason at the start of Unity
            else
                EditorSceneManager.playModeStartScene = entryScene;
        }
    }
}
#endif