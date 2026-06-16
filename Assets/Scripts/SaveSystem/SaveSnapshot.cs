using System;

namespace WaywardSon.SaveSystem
{
    [Serializable]
    public class SaveSnapshot
    {
        public string id;
        public DateTime timestamp;
        public string description;
        public string parentID;
        public GameSaveData data;

        public SaveSnapshot()
        {
            id = Guid.NewGuid().ToString("N");
            timestamp = DateTime.Now;
            description = string.Empty;
            parentID = null;
            data = new GameSaveData();
        }
    }
}
