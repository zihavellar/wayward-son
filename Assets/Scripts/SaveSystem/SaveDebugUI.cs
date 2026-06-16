using UnityEngine;

namespace WaywardSon.SaveSystem
{
    public class SaveDebugUI : MonoBehaviour
    {
        [Header("Display")]
        public bool showInSceneView = false;
        public KeyCode toggleKey = KeyCode.F8;

        [Header("Position")]
        public int buttonX = 10;
        public int buttonY = 10;
        public int buttonWidth = 120;
        public int buttonHeight = 30;
        public int spacing = 4;

        private bool visible = true;
        private Vector2 snapshotScrollPos;

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible) return;

            int y = buttonY;

            GUI.Box(new Rect(buttonX - 4, y - 4, buttonWidth + 8, buttonHeight * 2 + spacing + 8), "");

            if (GUI.Button(new Rect(buttonX, y, buttonWidth, buttonHeight), "Save (Commit)"))
            {
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.CreateSnapshot("debug-ui-save");
                    Debug.Log("[SaveDebugUI] Snapshot criado.");
                }
            }

            y += buttonHeight + spacing;

            if (GUI.Button(new Rect(buttonX, y, buttonWidth, buttonHeight), "Load (Apply)"))
            {
                if (SaveManager.Instance != null)
                {
                    var profile = SaveManager.Instance.GetCurrentProfile();
                    if (profile != null && profile.workspaceID != null)
                    {
                        SaveManager.Instance.Revert();
                        Debug.Log("[SaveDebugUI] Ultimo commit aplicado.");
                    }
                }
            }

            DrawSnapshotList(ref y);
        }

        private void DrawSnapshotList(ref int y)
        {
            var mgr = SaveManager.Instance;
            if (mgr == null) return;

            var profile = mgr.GetCurrentProfile();
            if (profile == null) return;

            var commits = mgr.GetCommits();
            if (commits.Count == 0) return;

            int listWidth = buttonWidth + 60;
            int listX = buttonX - 4;
            y += spacing + 4;

            float rowHeight = buttonHeight - 4;
            float totalHeight = commits.Count * (rowHeight + spacing);
            float maxVisibleHeight = Mathf.Min(totalHeight, 250f);

            GUI.Box(new Rect(listX, y, listWidth + 16, maxVisibleHeight + 24), "Snapshots");

            float innerWidth = listWidth - 4;
            float innerHeight = maxVisibleHeight + 4;
            snapshotScrollPos = GUI.BeginScrollView(
                new Rect(listX + 8, y + 18, listWidth, innerHeight),
                snapshotScrollPos,
                new Rect(0, 0, innerWidth - 16, totalHeight));

            float yy = 0;
            for (int i = commits.Count - 1; i >= 0; i--)
            {
                var c = commits[i];
                bool isCurrent = c.id == profile.workspaceID;

                var btnRect = new Rect(0, yy, innerWidth - 16, rowHeight);

                if (isCurrent)
                {
                    GUI.color = Color.green;
                    GUI.Box(btnRect, "");
                    GUI.color = Color.white;
                }

                string label = $"{c.timestamp:HH:mm:ss} {c.description}";
                if (GUI.Button(btnRect, label))
                {
                    if (isCurrent)
                    {
                        mgr.ApplySnapshot(c.id);
                        Debug.Log($"[SaveDebugUI] Snapshot '{c.description}' re-aplicado.");
                    }
                    else
                    {
                        mgr.Checkout(c.id);
                        Debug.Log($"[SaveDebugUI] Checkout para snapshot '{c.description}'.");
                    }
                }

                yy += rowHeight + spacing;
            }

            GUI.EndScrollView();
        }
    }
}
