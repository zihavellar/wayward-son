using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WaywardSon.SaveSystem.Editor
{
    public static class EditorSaveUtils
    {
        public static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, "WaywardSon", "Saves");
        }

        public static string GetProfilePath(string profileName)
        {
            return Path.Combine(GetSavePath(), profileName);
        }

        public static string[] ListProfiles()
        {
            string root = GetSavePath();
            if (!Directory.Exists(root)) return new string[0];
            var dirs = Directory.GetDirectories(root);
            var names = new List<string>();
            foreach (var d in dirs)
                names.Add(Path.GetFileName(d));
            return names.ToArray();
        }

        public static SaveProfile LoadProfileFromDisk(string name)
        {
            string path = GetProfilePath(name);
            string metaPath = Path.Combine(path, "profile.json");
            if (!File.Exists(metaPath)) return null;
            string json = File.ReadAllText(metaPath);
            return JsonUtility.FromJson<SaveProfile>(json);
        }

        public static List<SaveSnapshot> LoadAllSnapshotsFromDisk(string profileName)
        {
            var result = new List<SaveSnapshot>();
            string path = GetProfilePath(profileName);
            string snapDir = Path.Combine(path, "snapshots");
            if (!Directory.Exists(snapDir)) return result;

            var profile = LoadProfileFromDisk(profileName);
            if (profile == null) return result;

            foreach (var snapFile in Directory.GetFiles(snapDir, "*.json"))
            {
                string snapJson = File.ReadAllText(snapFile);
                var snap = JsonUtility.FromJson<SaveSnapshot>(snapJson);
                if (snap != null)
                {
                    snap.data ??= new GameSaveData();
                    snap.data.AfterDeserialize();
                    result.Add(snap);
                }
            }
            return result;
        }

        public static SaveSnapshot LoadSnapshotFromDisk(string profileName, string snapshotId)
        {
            string path = Path.Combine(GetProfilePath(profileName), "snapshots", snapshotId + ".json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            var snap = JsonUtility.FromJson<SaveSnapshot>(json);
            if (snap != null)
            {
                snap.data ??= new GameSaveData();
                snap.data.AfterDeserialize();
            }
            return snap;
        }

        public static string ReadSnapshotRawJson(string profileName, string snapshotId)
        {
            string path = Path.Combine(GetProfilePath(profileName), "snapshots", snapshotId + ".json");
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        public static void ApplySnapshotToScene(SaveSnapshot snapshot)
        {
            if (snapshot?.data == null) return;

            var allMonoBehaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            var toApply = new List<ISaveable>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb is ISaveable s && mb.gameObject.scene.IsValid())
                    toApply.Add(s);
            }

            var objectsToDirty = new List<Object>();
            foreach (var s in toApply)
            {
                if (snapshot.data.components.TryGetValue(s.SaveID, out var compData))
                {
                    if (compData is Dictionary<string, object> dict)
                    {
                        Undo.RecordObject((MonoBehaviour)s, "Apply Snapshot");
                        s.ApplyData(dict);
                        objectsToDirty.Add((MonoBehaviour)s);
                    }
                }
            }

            foreach (var obj in objectsToDirty)
                EditorUtility.SetDirty(obj);

            Debug.Log($"[EditorSaveUtils] Applied snapshot '{snapshot.description}' to {objectsToDirty.Count} ISaveable objects.");
        }
    }
}
