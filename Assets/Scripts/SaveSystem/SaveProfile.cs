using System;
using System.Collections.Generic;

namespace WaywardSon.SaveSystem
{
    [Serializable]
    public class SaveProfile
    {
        public string name;
        public DateTime createdAt;
        public DateTime lastPlayedAt;
        public List<string> commitIDs = new List<string>();
        public string workspaceID;
    }
}
