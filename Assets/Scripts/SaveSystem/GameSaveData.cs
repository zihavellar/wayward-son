using System;
using System.Collections.Generic;

namespace WaywardSon.SaveSystem
{
    [Serializable]
    public class GameSaveData
    {
        public string sceneName;
        public DateTime timestamp;

        [NonSerialized]
        public Dictionary<string, object> components = new Dictionary<string, object>();

        public string componentsJson;

        public void BeforeSerialize()
        {
            componentsJson = MiniJSON.Serialize(components);
        }

        public void AfterDeserialize()
        {
            components.Clear();
            if (string.IsNullOrEmpty(componentsJson)) return;
            var parsed = MiniJSON.Deserialize(componentsJson);
            if (parsed is Dictionary<string, object> dict)
            {
                foreach (var kv in dict)
                    components[kv.Key] = kv.Value;
            }
        }
    }
}
