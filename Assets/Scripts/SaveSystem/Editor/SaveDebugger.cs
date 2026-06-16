using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WaywardSon.SaveSystem.Editor
{
    [InitializeOnLoad]
    public static class SaveDebugger
    {
        static SaveDebugger()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnsureDebugDirectory();
            }
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (Application.isPlaying && SaveManager.Instance != null)
            {
                SaveManager.Instance.RefreshSaveables();
            }
        }

        [MenuItem("Wayward Son/Create Debug Deltas Folder")]
        private static void EnsureDebugDirectory()
        {
            string debugPath = Path.Combine(Application.dataPath, "Debug", "SaveDeltas");
            if (!Directory.Exists(debugPath))
            {
                Directory.CreateDirectory(debugPath);
                Debug.Log("[SaveDebugger] Debug/SaveDeltas folder created.");
            }
        }

        [MenuItem("Wayward Son/Ensure SaveManager in Scene")]
        private static void EnsureSaveManager()
        {
            if (GameObject.FindObjectOfType<SaveManager>() != null) return;

            var go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create SaveManager");
            Debug.Log("[SaveDebugger] SaveManager created in scene.");
        }
    }
}
