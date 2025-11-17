using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fsi.DataSystem.Libraries
{
    [Serializable]
    public class Library<TEntry, TID> where TEntry : IDataEntry<TID>
    {
        [SerializeField]
        private List<TEntry> entries = new();
        public List<TEntry> Entries => entries;
        
        public void Validate()
        {
            #if UNITY_EDITOR
            if (entries == null || entries.Count == 0)
            {
                return;
            }
            
            entries.RemoveAll(q => q == null);

            // Group all quests by their QuestDataId
            IEnumerable<IGrouping<TID, TEntry>> duplicateGroups = entries
                .GroupBy(q => q.ID)
                .Where(g => g.Skip(1).Any()); // faster than Count() > 1

            IEnumerable<IGrouping<TID,TEntry>> enumerable = duplicateGroups as IGrouping<TID, TEntry>[] ?? duplicateGroups.ToArray();
            foreach (IGrouping<TID, TEntry> group in enumerable)
            {
                string names = string.Join(", ", group.Select(q => q.ID));
                Debug.LogWarning($"Library | Duplicate ID detected: '{group.Key}' used by {names}");
            }
            #endif
        }

        public bool TryGetEntry(TID id, out TEntry entry)
        {
            foreach (TEntry e in Entries)
            {
                if (e.ID.Equals(id))
                {
                    entry = e;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public List<TID> GetIDs()
        {
            List<TID> ids = new();
            foreach (TEntry entry in Entries)
            {
                ids.Add(entry.ID);
            }

            return ids;
        }
    }
}