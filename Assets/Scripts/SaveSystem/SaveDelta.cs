using System;
using System.Collections.Generic;

namespace WaywardSon.SaveSystem
{
    [Serializable]
    public class FieldChange
    {
        public string componentID;
        public string fieldPath;
        public string oldValue;
        public string newValue;
        public ChangeType type;
    }

    public enum ChangeType { Modified, Added, Removed }

    [Serializable]
    public class SaveDelta
    {
        public string timestamp;
        public string baseSnapshotID;
        public string baseDescription;
        public List<FieldChange> changes = new List<FieldChange>();

        public bool HasChanges => changes.Count > 0;
    }
}
