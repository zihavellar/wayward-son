using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WaywardSon.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Config")]
        public string profilesRoot = "WaywardSon/Saves";
        public bool enableDebugJSON = true;

        private List<ISaveable> saveables = new List<ISaveable>();
        private SaveProfile currentProfile;
        private Dictionary<string, SaveSnapshot> snapshots = new Dictionary<string, SaveSnapshot>();
        private List<SaveSnapshot> stash = new List<SaveSnapshot>();
        private bool stashActive;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            DiscoverSaveables();

            if (currentProfile == null)
            {
                string[] existing = ListProfiles();
                if (existing.Length > 0)
                {
                    LoadProfile(existing[0]);
                }
                else
                {
                    CreateProfile("Default");
                    LoadProfile("Default");
                }
                DiscoverSaveables();
            }
        }

        private void DiscoverSaveables()
        {
            saveables.Clear();
            foreach (var obj in FindObjectsOfType<MonoBehaviour>())
            {
                if (obj is ISaveable s)
                    saveables.Add(s);
            }
        }

        public void RefreshSaveables()
        {
            DiscoverSaveables();
        }

        public string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, profilesRoot);
        }

        public string GetProfilePath(string profileName)
        {
            return Path.Combine(GetSavePath(), profileName);
        }

        // ─── Profile Management ────────────────────────────────────────

        public void CreateProfile(string name)
        {
            string path = GetProfilePath(name);
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(Path.Combine(path, "snapshots"));

            currentProfile = new SaveProfile
            {
                name = name,
                createdAt = System.DateTime.Now,
                lastPlayedAt = System.DateTime.Now,
                workspaceID = null
            };

            SaveProfileToDisk();
            SaveDebugProfileList();
        }

        public bool ProfileExists(string name)
        {
            return Directory.Exists(GetProfilePath(name));
        }

        public string[] ListProfiles()
        {
            string root = GetSavePath();
            if (!Directory.Exists(root)) return new string[0];
            var dirs = Directory.GetDirectories(root);
            var names = new List<string>();
            foreach (var d in dirs)
                names.Add(Path.GetFileName(d));
            return names.ToArray();
        }

        public bool LoadProfile(string name)
        {
            string path = GetProfilePath(name);
            string metaPath = Path.Combine(path, "profile.json");
            if (!File.Exists(metaPath)) return false;

            string json = File.ReadAllText(metaPath);
            currentProfile = JsonUtility.FromJson<SaveProfile>(json);
            snapshots.Clear();

            string snapDir = Path.Combine(path, "snapshots");
            if (Directory.Exists(snapDir))
            {
                foreach (var snapFile in Directory.GetFiles(snapDir, "*.json"))
                {
                    string snapJson = File.ReadAllText(snapFile);
                    var snap = JsonUtility.FromJson<SaveSnapshot>(snapJson);
                    if (snap != null)
                    {
                        snap.data ??= new GameSaveData();
                        snap.data.AfterDeserialize();
                        snapshots[snap.id] = snap;
                    }
                }
            }

            return true;
        }

        // ─── Snapshot / Commit ─────────────────────────────────────────

        public SaveSnapshot CreateSnapshot(string description = "")
        {
            if (currentProfile == null) return null;

            var snapshot = new SaveSnapshot
            {
                description = description,
                parentID = currentProfile.workspaceID
            };

            PopulateSnapshotData(snapshot.data);
            snapshot.data.BeforeSerialize();

            snapshots[snapshot.id] = snapshot;
            currentProfile.commitIDs.Add(snapshot.id);
            currentProfile.workspaceID = snapshot.id;
            currentProfile.lastPlayedAt = System.DateTime.Now;

            string snapPath = Path.Combine(GetProfilePath(currentProfile.name), "snapshots", snapshot.id + ".json");
            File.WriteAllText(snapPath, JsonUtility.ToJson(snapshot, true));

            SaveProfileToDisk();

            if (enableDebugJSON)
                SaveDebugDelta(null, snapshot);

            ApplySnapshotData(snapshot.data);

            return snapshot;
        }

        private void PopulateSnapshotData(GameSaveData data)
        {
            data.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            data.timestamp = System.DateTime.Now;

            foreach (var s in saveables)
            {
                var compData = new Dictionary<string, object>();
                s.CollectData(compData);
                data.components[s.SaveID] = compData;
            }
        }

        // ─── Revert ────────────────────────────────────────────────────

        public SaveSnapshot Revert()
        {
            if (currentProfile == null || currentProfile.workspaceID == null) return null;

            var snapshot = GetSnapshot(currentProfile.workspaceID);
            if (snapshot == null) return null;

            ApplySnapshotData(snapshot.data);
            return snapshot;
        }

        // ─── Apply (load snapshot data into scene) ─────────────────────

        public SaveSnapshot ApplySnapshot(string snapshotID)
        {
            if (currentProfile == null) return null;
            var target = GetSnapshot(snapshotID);
            if (target == null) return null;
            ApplySnapshotData(target.data);
            return target;
        }

        // ─── Checkout (with stash) ─────────────────────────────────────

        public SaveSnapshot Checkout(string snapshotID)
        {
            if (currentProfile == null) return null;

            var target = GetSnapshot(snapshotID);
            if (target == null) return null;

            if (!stashActive)
            {
                var workspaceSnap = new SaveSnapshot { description = "Stash: pre-checkout" };
                PopulateSnapshotData(workspaceSnap.data);
                stash.Add(workspaceSnap);
                stashActive = true;
            }

            ApplySnapshotData(target.data);
            return target;
        }

        public SaveSnapshot ReturnToWorkspace()
        {
            if (!stashActive || stash.Count == 0) return null;
            var stashed = stash[stash.Count - 1];
            stash.RemoveAt(stash.Count - 1);
            if (stash.Count == 0) stashActive = false;
            ApplySnapshotData(stashed.data);
            return stashed;
        }

        public bool HasStash => stashActive && stash.Count > 0;

        // ─── Apply data to scene ───────────────────────────────────────

        private void ApplySnapshotData(GameSaveData data)
        {
            foreach (var s in saveables)
            {
                if (data.components.TryGetValue(s.SaveID, out var compData))
                {
                    var dict = compData as Dictionary<string, object>;
                    if (dict != null)
                        s.ApplyData(dict);
                }
            }
            RefreshSaveables();
        }

        // ─── Delta ─────────────────────────────────────────────────────

        public SaveDelta ComputeDelta()
        {
            var workspace = new GameSaveData();
            PopulateSnapshotData(workspace);

            var delta = new SaveDelta
            {
                timestamp = System.DateTime.Now.ToString("o"),
                baseSnapshotID = currentProfile?.workspaceID,
                changes = new List<FieldChange>()
            };

            if (currentProfile?.workspaceID != null &&
                snapshots.TryGetValue(currentProfile.workspaceID, out var baseSnap))
            {
                delta.baseDescription = baseSnap.description;
                ComputeChanges(baseSnap.data, workspace, delta);
            }
            else
            {
                foreach (var kv in workspace.components)
                {
                    var change = new FieldChange
                    {
                        componentID = kv.Key,
                        fieldPath = kv.Key,
                        newValue = DictToString(kv.Value as Dictionary<string, object>),
                        type = ChangeType.Added
                    };
                    delta.changes.Add(change);
                }
            }

            return delta;
        }

        private void ComputeChanges(GameSaveData baseData, GameSaveData currentData, SaveDelta delta)
        {
            foreach (var kv in currentData.components)
            {
                var baseDict = baseData.components.TryGetValue(kv.Key, out var bv)
                    ? bv as Dictionary<string, object> : null;
                var currDict = kv.Value as Dictionary<string, object>;

                if (baseDict == null)
                {
                    delta.changes.Add(new FieldChange
                    {
                        componentID = kv.Key, fieldPath = kv.Key,
                        newValue = DictToString(currDict), type = ChangeType.Added
                    });
                    continue;
                }

                if (currDict == null) continue;

                foreach (var field in currDict)
                {
                    string oldVal = baseDict.TryGetValue(field.Key, out var ov) ? SafeString(ov) : null;
                    string newVal = SafeString(field.Value);

                    if (oldVal != newVal)
                    {
                        delta.changes.Add(new FieldChange
                        {
                            componentID = kv.Key,
                            fieldPath = field.Key,
                            oldValue = oldVal ?? "<null>",
                            newValue = newVal ?? "<null>",
                            type = ChangeType.Modified
                        });
                    }
                }
            }
        }

        // ─── Disk helpers ──────────────────────────────────────────────

        private void SaveProfileToDisk()
        {
            if (currentProfile == null) return;
            string path = Path.Combine(GetProfilePath(currentProfile.name), "profile.json");
            File.WriteAllText(path, JsonUtility.ToJson(currentProfile, true));
        }

        private void SaveDebugProfileList()
        {
            if (!enableDebugJSON) return;
#if UNITY_EDITOR
            string debugPath = Path.Combine(Application.dataPath, "Debug", "SaveDeltas");
            Directory.CreateDirectory(debugPath);
            var list = ListProfiles();
            var wrapper = new System.Collections.Generic.Dictionary<string, object>
            {
                ["profiles"] = list,
                ["activeProfile"] = (object)(currentProfile?.name ?? "none")
            };
            string json = MiniJSON.Serialize(wrapper);
            File.WriteAllText(Path.Combine(debugPath, "profiles.json"), json);
#endif
        }

        private void SaveDebugDelta(SaveSnapshot baseRef, SaveSnapshot current)
        {
            if (!enableDebugJSON) return;
#if UNITY_EDITOR
            var delta = new SaveDelta
            {
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
                baseSnapshotID = baseRef?.id ?? "none",
                baseDescription = baseRef?.description ?? "initial",
                changes = new List<FieldChange>()
            };

            var workspace = new GameSaveData();
            PopulateSnapshotData(workspace);
            ComputeChanges(current.data, workspace, delta);

            var fullData = new Dictionary<string, object>
            {
                ["snapshotID"] = current.id,
                ["description"] = current.description,
                ["timestamp"] = current.timestamp.ToString("o"),
                ["parentID"] = current.parentID ?? "none",
                ["delta"] = delta
            };

            string debugPath = Path.Combine(Application.dataPath, "Debug", "SaveDeltas");
            Directory.CreateDirectory(debugPath);
            string json = MiniJSON.Serialize(fullData);
            File.WriteAllText(Path.Combine(debugPath, $"commit_{current.id}.json"), json);
#endif
        }

        // ─── Utils ─────────────────────────────────────────────────────

        public SaveSnapshot GetSnapshot(string id)
        {
            snapshots.TryGetValue(id, out var snap);
            return snap;
        }

        public SaveProfile GetCurrentProfile() => currentProfile;
        public List<SaveSnapshot> GetCommits()
        {
            var list = new List<SaveSnapshot>();
            if (currentProfile == null) return list;
            foreach (var id in currentProfile.commitIDs)
            {
                if (snapshots.TryGetValue(id, out var snap))
                    list.Add(snap);
            }
            return list;
        }

        public List<SaveSnapshot> GetStashes() => new List<SaveSnapshot>(stash);

        private string SafeString(object val)
        {
            if (val == null) return "null";
            if (val is float f) return f.ToString("F4");
            if (val is bool) return val.ToString().ToLower();
            return val.ToString();
        }

        private string DictToString(Dictionary<string, object> dict)
        {
            if (dict == null) return "null";
            var parts = new List<string>();
            foreach (var kv in dict)
                parts.Add($"{kv.Key}={SafeString(kv.Value)}");
            return string.Join(", ", parts);
        }
    }

    internal static class MiniJSON
    {
        public static string Serialize(object obj)
        {
            if (obj == null) return "null";
            if (obj is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            if (obj is bool b) return b ? "true" : "false";
            if (obj is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is int i) return i.ToString();
            if (obj is System.DateTime dt) return "\"" + dt.ToString("o") + "\"";

            if (obj is System.Collections.IDictionary dict)
            {
                var items = new List<string>();
                foreach (var key in dict.Keys)
                {
                    items.Add(Serialize(key) + ": " + Serialize(dict[key]));
                }
                return "{\n" + string.Join(",\n", items) + "\n}";
            }

            if (obj is System.Collections.IEnumerable enumerable)
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                    items.Add(Serialize(item));
                return "[" + string.Join(", ", items) + "]";
            }

            return obj.ToString();
        }

        private static int idx;
        private static string src;

        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            idx = 0;
            src = json;
            return ParseValue();
        }

        private static object ParseValue()
        {
            SkipWhitespace();
            if (idx >= src.Length) return null;
            char c = src[idx];
            if (c == '{') return ParseObject();
            if (c == '[') return ParseArray();
            if (c == '"') return ParseString();
            if (c == 't' || c == 'f') return ParseBool();
            if (c == 'n') { idx += 4; return null; }
            return ParseNumber();
        }

        private static Dictionary<string, object> ParseObject()
        {
            var dict = new Dictionary<string, object>();
            idx++;
            SkipWhitespace();
            if (src[idx] == '}') { idx++; return dict; }
            while (true)
            {
                SkipWhitespace();
                string key = ParseString();
                SkipWhitespace();
                idx++;
                SkipWhitespace();
                dict[key] = ParseValue();
                SkipWhitespace();
                if (src[idx] == '}') { idx++; return dict; }
                idx++;
            }
        }

        private static List<object> ParseArray()
        {
            var list = new List<object>();
            idx++;
            SkipWhitespace();
            if (src[idx] == ']') { idx++; return list; }
            while (true)
            {
                list.Add(ParseValue());
                SkipWhitespace();
                if (src[idx] == ']') { idx++; return list; }
                idx++;
            }
        }

        private static string ParseString()
        {
            idx++;
            int start = idx;
            while (src[idx] != '"')
            {
                if (src[idx] == '\\') idx++;
                idx++;
            }
            string raw = src.Substring(start, idx - start);
            idx++;
            return raw.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\t", "\t");
        }

        private static object ParseNumber()
        {
            int start = idx;
            bool isFloat = false;
            while (idx < src.Length && (char.IsDigit(src[idx]) || src[idx] == '.' || src[idx] == '-' || src[idx] == 'e' || src[idx] == 'E'))
            {
                if (src[idx] == '.' || src[idx] == 'e' || src[idx] == 'E') isFloat = true;
                idx++;
            }
            string num = src.Substring(start, idx - start);
            if (isFloat)
                return float.Parse(num, System.Globalization.CultureInfo.InvariantCulture);
            return int.Parse(num, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool ParseBool()
        {
            if (src[idx] == 't') { idx += 4; return true; }
            idx += 5;
            return false;
        }

        private static void SkipWhitespace()
        {
            while (idx < src.Length && char.IsWhiteSpace(src[idx])) idx++;
        }
    }
}
