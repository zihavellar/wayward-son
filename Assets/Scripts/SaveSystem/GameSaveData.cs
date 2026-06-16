using System;
using System.Collections.Generic;

namespace WaywardSon.SaveSystem
{
    [Serializable]
    public class GameSaveData
    {
        public string sceneName;
        public DateTime timestamp;

        public Dictionary<string, object> components = new Dictionary<string, object>();
    }
}
