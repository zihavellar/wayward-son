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

        private string browseProfileName = "";

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

            bool hasManager = SaveManager.Instance != null;
            bool isPlaying = Application.isPlaying;

            if (hasManager && isPlaying)
            {
                DrawFullMode();
            }
            else
            {
                DrawBrowseMode(hasManager);
            }

            DrawStatusBar(hasManager);
        }

        // ─── Full Mode (Play + SaveManager Instance) ────────────────────

        private void DrawFullMode()
        {
            var mgr = SaveManager.Instance;

            EditorGUILayout.BeginHorizontal();
            DrawProfiles(mgr);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 6));
            DrawCommitsFull(mgr);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 6));
            DrawWorkspace(mgr);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawProfiles(SaveManager mgr)
        {
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

        private void DrawCommitsFull(SaveManager mgr)
        {
            EditorGUILayout.LabelField("Commits", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            commitMessage = EditorGUILayout.TextField(commitMessage, GUILayout.Width(160));
            if (GUILayout.Button("Commit Snapshot", GUILayout.Width(110)))
            {
                mgr.CreateSnapshot(string.IsNullOrEmpty(commitMessage) ? "editor-save" : commitMessage);
                commitMessage = "";
                SetStatus("Snapshot criado.");
            }
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(selectedSnapshotID))
                {
                    mgr.ApplySnapshot(selectedSnapshotID);
                    SetStatus("Snapshot aplicado ao cena.");
                }
                else
                {
                    SetStatus("Selecione um commit primeiro.");
                }
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
                    EditorGUILayout.LabelField(c.description, GUILayout.Width(80));

                    if (GUILayout.Button("Apply", GUILayout.Width(50)))
                    {
                        mgr.ApplySnapshot(c.id);
                        selectedSnapshotID = c.id;
                        SetStatus($"Snapshot '{c.description}' aplicado.");
                    }

                    if (GUILayout.Button("Checkout", GUILayout.Width(70)))
                    {
                        mgr.Checkout(c.id);
                        selectedSnapshotID = c.id;
                        SetStatus($"Checkout para commit #{i} ({c.description}).");
                    }

                    EditorGUILayout.EndHorizontal();

                    var clickRect = rect;
                    clickRect.width -= 200;
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

        private void DrawWorkspace(SaveManager mgr)
        {
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

            DrawJsonPreviewFull(mgr, delta);
        }

        private void DrawJsonPreviewFull(SaveManager mgr, SaveDelta delta)
        {
            if (!showJsonPreview) return;
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

        // ─── Browse Mode (Edit or no SaveManager) ───────────────────────

        private string[] browseProfilesCache = new string[0];
        private List<SaveSnapshot> browseCommitsCache = new List<SaveSnapshot>();

        private void DrawBrowseMode(bool hasManager)
        {
            if (!hasManager)
            {
                EditorGUILayout.HelpBox("SaveManager nao encontrado na cena. Criando browser de arquivos.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Modo Browser — visualize snapshots salvos em disco. Entre em Play Mode para funcionalidade completa.", MessageType.None);
            }

            EditorGUILayout.Space(2);

            // Profile selection
            EditorGUILayout.BeginHorizontal();
            var allProfiles = EditorSaveUtils.ListProfiles();
            browseProfilesCache = allProfiles;

            var profileNames = new List<string>(allProfiles);
            profileNames.Insert(0, "-- Selecionar Perfil --");
            int currentIdx = string.IsNullOrEmpty(browseProfileName)
                ? 0
                : System.Array.IndexOf(allProfiles, browseProfileName) + 1;

            int chosen = EditorGUILayout.Popup("Perfil", currentIdx, profileNames.ToArray(), GUILayout.Width(300));
            if (chosen > 0)
            {
                string prev = browseProfileName;
                browseProfileName = allProfiles[chosen - 1];
                if (browseProfileName != prev)
                {
                    browseCommitsCache = EditorSaveUtils.LoadAllSnapshotsFromDisk(browseProfileName);
                    selectedSnapshotID = null;
                    jsonPreviewText = "";
                    SetStatus($"Perfil '{browseProfileName}' — {browseCommitsCache.Count} snapshots.");
                }
            }

            if (!string.IsNullOrEmpty(browseProfileName))
            {
                EditorGUILayout.LabelField("Ativo:", browseProfileName, EditorStyles.boldLabel, GUILayout.Width(200));
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(browseProfileName) || browseCommitsCache.Count == 0)
            {
                EditorGUILayout.HelpBox("Selecione um perfil com snapshots.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);

            // Commits + JSON Preview side by side
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 6));
            DrawBrowseCommits();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 6));
            DrawBrowseActions();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBrowseCommits()
        {
            EditorGUILayout.LabelField("Snapshots (disco)", EditorStyles.boldLabel);

            scrollCommits = EditorGUILayout.BeginScrollView(scrollCommits, GUI.skin.box, GUILayout.ExpandHeight(true));

            for (int i = browseCommitsCache.Count - 1; i >= 0; i--)
            {
                var c = browseCommitsCache[i];
                bool isSelected = c.id == selectedSnapshotID;

                var rect = EditorGUILayout.BeginHorizontal(GUI.skin.box);

                var bg = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;
                EditorGUI.DrawRect(rect, bg);

                EditorGUILayout.LabelField($"#{i}", GUILayout.Width(24));
                EditorGUILayout.LabelField(c.timestamp.ToString("HH:mm:ss"), GUILayout.Width(60));
                EditorGUILayout.LabelField(c.description, GUILayout.Width(100));

                if (GUILayout.Button("Apply", GUILayout.Width(50)))
                {
                    selectedSnapshotID = c.id;
                    var full = EditorSaveUtils.LoadSnapshotFromDisk(browseProfileName, c.id);
                    if (full != null)
                    {
                        EditorSaveUtils.ApplySnapshotToScene(full);
                        SetStatus($"Snapshot '{c.description}' aplicado a cena (Editor).");
                    }
                }

                EditorGUILayout.EndHorizontal();

                var clickRect = rect;
                clickRect.width -= 80;
                if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
                {
                    selectedSnapshotID = c.id;
                    jsonPreviewText = EditorSaveUtils.ReadSnapshotRawJson(browseProfileName, c.id) ?? "Erro ao ler arquivo.";
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBrowseActions()
        {
            EditorGUILayout.LabelField("Acoes", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(selectedSnapshotID))
            {
                if (GUILayout.Button("Apply Snapshot Selecionado", GUILayout.Height(30)))
                {
                    var full = EditorSaveUtils.LoadSnapshotFromDisk(browseProfileName, selectedSnapshotID);
                    if (full != null)
                    {
                        EditorSaveUtils.ApplySnapshotToScene(full);
                        SetStatus($"Snapshot '{full.description}' aplicado a cena (Editor).");
                    }
                }
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("JSON Preview", EditorStyles.boldLabel);
            scrollJson = EditorGUILayout.BeginScrollView(scrollJson, GUI.skin.box, GUILayout.ExpandHeight(true));
            if (string.IsNullOrEmpty(jsonPreviewText))
            {
                EditorGUILayout.LabelField("Clique em um snapshot para ver o JSON.", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.SelectableLabel(jsonPreviewText, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();
        }

        // ─── Toolbar ────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.RefreshSaveables();

                browseCommitsCache.Clear();
                if (!string.IsNullOrEmpty(browseProfileName))
                    browseCommitsCache = EditorSaveUtils.LoadAllSnapshotsFromDisk(browseProfileName);

                Repaint();
            }
            GUILayout.FlexibleSpace();
            showJsonPreview = GUILayout.Toggle(showJsonPreview, "JSON Preview", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        // ─── Status Bar ─────────────────────────────────────────────────

        private void DrawStatusBar(bool hasManager)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(statusMessage, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (hasManager && Application.isPlaying)
            {
                var profile = SaveManager.Instance.GetCurrentProfile();
                if (profile != null)
                {
                    EditorGUILayout.LabelField($"Commits: {profile.commitIDs.Count} | Stash: {(SaveManager.Instance.HasStash ? "ativo" : "vazio")}", EditorStyles.miniLabel);
                }
            }
            else
            {
                int profileCount = EditorSaveUtils.ListProfiles().Length;
                EditorGUILayout.LabelField($"Editor Browser | Perfis: {profileCount} | Snapshots: {browseCommitsCache.Count}", EditorStyles.miniLabel);
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
