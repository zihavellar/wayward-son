using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WaywardSon.SaveSystem.Editor
{
    public class SaveEditorWindow : EditorWindow
    {
        [MenuItem("Wayward Son/Save System")]
        public static void Open()
        {
            GetWindow<SaveEditorWindow>("Save System");
        }

        private Vector2 scrollCommits;
        private Vector2 scrollWorkspace;
        private Vector2 scrollJson;
        private string newProfileName = "";
        private string commitMessage = "";
        private string selectedSnapshotID;
        private bool showJsonPreview;
        private string jsonPreviewText = "";
        private string statusMessage = "";
        private System.DateTime lastRefresh;

        private void OnEnable()
        {
            lastRefresh = System.DateTime.MinValue;
            titleContent = new GUIContent("Save System", EditorGUIUtility.IconContent("SaveActive").image);
        }

        private void OnGUI()
        {
            if (Time.realtimeSinceStartup - (float)(System.DateTime.Now - lastRefresh).TotalSeconds > 2f)
            {
                lastRefresh = System.DateTime.Now;
                Repaint();
            }

            DrawToolbar();
            EditorGUILayout.Space();

            if (SaveManager.Instance == null)
            {
                EditorGUILayout.HelpBox("SaveManager nao encontrado na cena. Entre em Play Mode.", MessageType.Info);
                if (GUILayout.Button("Criar GameObject SaveManager (Editor)"))
                {
                    var go = new GameObject("SaveManager");
                    go.AddComponent<SaveManager>();
                    Undo.RegisterCreatedObjectUndo(go, "Create SaveManager");
                }
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entre em Play Mode para usar o Save System.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawProfiles();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 6));
            DrawCommits();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 6));
            DrawWorkspace();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.RefreshSaveables();
                Repaint();
            }
            GUILayout.FlexibleSpace();
            showJsonPreview = GUILayout.Toggle(showJsonPreview, "JSON Preview", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProfiles()
        {
            var mgr = SaveManager.Instance;
            var profiles = mgr.ListProfiles();
            var current = mgr.GetCurrentProfile();

            int selectedIdx = -1;
            var profileNames = new List<string>(profiles);
            if (current != null)
            {
                selectedIdx = System.Array.IndexOf(profiles, current.name);
            }

            profileNames.Insert(0, "-- Selecionar Perfil --");

            int guiIdx = EditorGUILayout.Popup("Perfil", selectedIdx + 1, profileNames.ToArray(), GUILayout.Width(300));
            if (guiIdx > 0 && guiIdx - 1 != selectedIdx)
            {
                string name = profiles[guiIdx - 1];
                if (mgr.LoadProfile(name))
                {
                    mgr.RefreshSaveables();
                    SetStatus($"Perfil '{name}' carregado.");
                }
                else
                {
                    SetStatus($"Falha ao carregar perfil '{name}'.");
                }
            }

            if (current != null)
            {
                EditorGUILayout.LabelField("Ativo:", current.name, EditorStyles.boldLabel, GUILayout.Width(200));
            }

            newProfileName = EditorGUILayout.TextField(newProfileName, GUILayout.Width(150));
            if (GUILayout.Button("+ Criar", GUILayout.Width(70)))
            {
                if (!string.IsNullOrEmpty(newProfileName))
                {
                    mgr.CreateProfile(newProfileName);
                    mgr.LoadProfile(newProfileName);
                    mgr.RefreshSaveables();
                    SetStatus($"Perfil '{newProfileName}' criado.");
                    newProfileName = "";
                }
            }
        }

        private void DrawCommits()
        {
            var mgr = SaveManager.Instance;

            EditorGUILayout.LabelField("Commits", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            commitMessage = EditorGUILayout.TextField(commitMessage, GUILayout.Width(180));
            if (GUILayout.Button("Commit Snapshot", GUILayout.Width(120)))
            {
                mgr.CreateSnapshot(string.IsNullOrEmpty(commitMessage) ? "editor-save" : commitMessage);
                commitMessage = "";
                SetStatus("Snapshot criado.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            scrollCommits = EditorGUILayout.BeginScrollView(scrollCommits, GUI.skin.box, GUILayout.ExpandHeight(true));

            var commits = mgr.GetCommits();

            if (commits.Count == 0)
            {
                EditorGUILayout.LabelField("Nenhum commit ainda.", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = commits.Count - 1; i >= 0; i--)
                {
                    var c = commits[i];
                    bool isSelected = c.id == selectedSnapshotID;
                    bool isWorkspace = c.id == mgr.GetCurrentProfile()?.workspaceID;

                    var rect = EditorGUILayout.BeginHorizontal(GUI.skin.box);

                    var bg = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) :
                            isWorkspace ? new Color(0.2f, 0.8f, 0.3f, 0.2f) :
                            Color.clear;
                    EditorGUI.DrawRect(rect, bg);

                    EditorGUILayout.LabelField($"#{i}", GUILayout.Width(24));
                    EditorGUILayout.LabelField(c.timestamp.ToString("HH:mm:ss"), GUILayout.Width(60));
                    EditorGUILayout.LabelField(c.description, GUILayout.Width(100));

                    if (GUILayout.Button("Checkout", GUILayout.Width(70)))
                    {
                        mgr.Checkout(c.id);
                        selectedSnapshotID = c.id;
                        SetStatus($"Checkout para commit #{i} ({c.description}). Workspace stashado.");
                    }

                    EditorGUILayout.EndHorizontal();

                    var clickRect = rect;
                    clickRect.width -= 150;
                    if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
                    {
                        selectedSnapshotID = c.id;
                        jsonPreviewText = JsonUtility.ToJson(c, true);
                        Event.current.Use();
                    }
                }
            }

            if (mgr.HasStash)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Stash (Workspace salvo)", EditorStyles.boldLabel);
                if (GUILayout.Button("Return to Workspace"))
                {
                    mgr.ReturnToWorkspace();
                    SetStatus("Workspace restaurado do stash.");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawWorkspace()
        {
            var mgr = SaveManager.Instance;

            EditorGUILayout.LabelField("Workspace", EditorStyles.boldLabel);

            if (GUILayout.Button("Revert (descartar mudancas)", GUILayout.Width(200)))
            {
                mgr.Revert();
                SetStatus("Workspace revertido ao ultimo commit.");
            }

            EditorGUILayout.Space(2);

            scrollWorkspace = EditorGUILayout.BeginScrollView(scrollWorkspace, GUI.skin.box, GUILayout.ExpandHeight(true));

            var delta = mgr.ComputeDelta();

            if (!delta.HasChanges)
            {
                EditorGUILayout.LabelField("Nenhuma mudanca no workspace.", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"Committed: {delta.baseDescription ?? "none"}", EditorStyles.miniLabel);
                EditorGUILayout.Space(2);

                foreach (var change in delta.changes)
                {
                    EditorGUILayout.BeginHorizontal();

                    var icon = change.type == ChangeType.Added ? "▲" :
                               change.type == ChangeType.Removed ? "▼" : "◆";
                    var color = change.type == ChangeType.Added ? Color.green :
                                change.type == ChangeType.Removed ? Color.red : Color.yellow;

                    GUI.color = color;
                    EditorGUILayout.LabelField(icon, GUILayout.Width(16));
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField(change.componentID, EditorStyles.miniBoldLabel, GUILayout.Width(90));
                    EditorGUILayout.LabelField(change.fieldPath, GUILayout.Width(100));

                    if (change.type == ChangeType.Modified)
                    {
                        EditorGUILayout.LabelField(change.oldValue, GUILayout.Width(80));
                        EditorGUILayout.LabelField("→", GUILayout.Width(16));
                        EditorGUILayout.LabelField(change.newValue, GUILayout.Width(80));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(change.newValue, GUILayout.Width(180));
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            if (showJsonPreview)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("JSON Preview", EditorStyles.boldLabel);
                scrollJson = EditorGUILayout.BeginScrollView(scrollJson, GUI.skin.box, GUILayout.Height(150));
                string json;
                if (!string.IsNullOrEmpty(selectedSnapshotID))
                {
                    var snap = mgr.GetSnapshot(selectedSnapshotID);
                    json = snap != null ? JsonUtility.ToJson(snap, true) : "Snapshot nao encontrado.";
                }
                else
                {
                    json = JsonUtility.ToJson(delta, true);
                }
                jsonPreviewText = json;
                EditorGUILayout.SelectableLabel(jsonPreviewText, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(statusMessage, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (SaveManager.Instance != null)
            {
                var profile = SaveManager.Instance.GetCurrentProfile();
                if (profile != null)
                {
                    EditorGUILayout.LabelField($"Commits: {profile.commitIDs.Count} | Stash: {(SaveManager.Instance.HasStash ? "ativo" : "vazio")}", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SetStatus(string msg)
        {
            statusMessage = msg;
            Debug.Log("[SaveSystem] " + msg);
        }
    }
}
