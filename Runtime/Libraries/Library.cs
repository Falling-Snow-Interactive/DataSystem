using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fsi.DataSystem.Libraries
{
    [Serializable]
    public class Library<TId, TEntry> where TEntry : IDataEntry<TId>
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
            IEnumerable<IGrouping<TId, TEntry>> duplicateGroups = entries
                                                                  .GroupBy(q => q.ID)
                                                                  .Where(g => g.Skip(1).Any()); // faster than Count() > 1

            IEnumerable<IGrouping<TId,TEntry>> enumerable = duplicateGroups as IGrouping<TId, TEntry>[] ?? duplicateGroups.ToArray();
            foreach (IGrouping<TId, TEntry> group in enumerable)
            {
                string names = string.Join(", ", group.Select(q => q.ID));
                Debug.LogWarning($"Library | Duplicate ID detected: '{group.Key}' used by {names}");
            }
            #endif
        }

        public bool TryGetEntry(TId id, out TEntry entry)
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

        public List<TId> GetIDs()
        {
            List<TId> ids = new();
            foreach (TEntry entry in Entries)
            {
                ids.Add(entry.ID);
            }

            return ids;
        }

        public List<TEntry> Filter<T>() where T : TEntry
        {
            List<TEntry> e = Entries.Where(x => x is T).ToList();
            return e;
        }
    }
}