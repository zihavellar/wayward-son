using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WaywardSon.SaveSystem
{
    public class SaveCommands : MonoBehaviour
    {
        [Header("Auto-Save")]
        public bool enableAutoSave = true;
        public float autoSaveInterval = 120f;

        [Header("Manual Save")]
        public KeyCode saveKey = KeyCode.F5;

        private float lastAutoSaveTime;

        private void Start()
        {
            lastAutoSaveTime = Time.time;
            if (enableAutoSave)
                StartCoroutine(AutoSaveRoutine());
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                ManualSave();
            }
        }

        private IEnumerator AutoSaveRoutine()
        {
            while (enableAutoSave)
            {
                yield return new WaitForSeconds(autoSaveInterval);
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.CreateSnapshot("auto-save");
                    Debug.Log("[SaveCommands] Auto-save concluido.");
                }
            }
        }

        public void ManualSave()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[SaveCommands] SaveManager nao encontrado.");
                return;
            }

            SaveManager.Instance.CreateSnapshot("manual-save");
            Debug.Log("[SaveCommands] Save manual concluido. (F5)");
        }

        public void ManualLoad()
        {
            if (SaveManager.Instance == null) return;

            var profile = SaveManager.Instance.GetCurrentProfile();
            if (profile != null)
            {
                SaveManager.Instance.Revert();
                Debug.Log("[SaveCommands] Load concluido: ultimo save restaurado.");
            }
        }
    }
}
