using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fsi.DataSystem.Libraries
{
    [Serializable]
    public class Library<TID, TEntry> 
        where TEntry : ILibraryData<TID>
    {
        [SerializeField]
        private TID defaultID = default;
        public TEntry Default => GetDefault();
        
        [SerializeField]
        private List<TEntry> entries = new();
        public List<TEntry> Entries => entries;

        public void CheckDuplicates()
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }
            
            entries.RemoveAll(q => q == null);

            IEnumerable<IGrouping<TID, TEntry>> duplicateGroups = entries
                                                                  .GroupBy(q => q.ID)
                                                                  .Where(g => g.Skip(1).Any());

            IEnumerable<IGrouping<TID,TEntry>> enumerable = duplicateGroups as IGrouping<TID, TEntry>[] ?? duplicateGroups.ToArray();
            foreach (IGrouping<TID, TEntry> group in enumerable)
            {
                string names = string.Join(", ", group.Select(q => q.ID));
                Debug.LogWarning($"Library | Duplicate ID detected: '{group.Key}' used by {names}");
            }
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

        public List<TEntry> Filter()// where T : TEntry
        {
            List<TEntry> e = Entries.Where(x => x != null).ToList();
            return e;
        }

        public TEntry Random()
        {
            return Entries[UnityEngine.Random.Range(0, Entries.Count - 1)];
        }

        public TEntry GetDefault()
        {
            if (TryGetEntry(defaultID, out TEntry entry))
            {
                return entry;
            }

            throw new KeyNotFoundException($"Library | Default ID '{defaultID}' was not found in entries.");
        }
    }
}