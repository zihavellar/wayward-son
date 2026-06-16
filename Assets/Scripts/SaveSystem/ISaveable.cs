using System.Collections.Generic;

namespace WaywardSon.SaveSystem
{
    public interface ISaveable
    {
        string SaveID { get; }
        void CollectData(Dictionary<string, object> data);
        void ApplyData(Dictionary<string, object> data);
    }
}
