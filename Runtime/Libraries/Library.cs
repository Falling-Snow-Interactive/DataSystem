using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fsi.DataSystem.Libraries
{
    /// <summary>
    /// Serializable container for library entries with default lookup helpers.
    /// </summary>
    /// <typeparam name="TID">The identifier type for entries.</typeparam>
    /// <typeparam name="TEntry">The entry type stored in this library.</typeparam>
    [Serializable]
    public class Library<TID, TEntry> 
        where TEntry : IData<TID>
    {
        [SerializeField]
        private TID defaultID;
        
        /// <summary>
        /// Gets the default entry defined by <see cref="defaultID"/>.
        /// </summary>
        /// <returns>The default entry.</returns>
        public TEntry Default => GetDefault();
        
        [SerializeField]
        private List<TEntry> entries = new();
        
        /// <summary>
        /// Gets the list of entries stored in this library.
        /// </summary>
        public List<TEntry> Entries => entries;

        /// <summary>
        /// Logs warnings when duplicate IDs are detected within the entries list.
        /// </summary>
        public void CheckDuplicates()
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }
            
            // Remove nulls so duplicate detection only considers valid entries.
            entries.RemoveAll(q => q == null);

            IEnumerable<IGrouping<TID, TEntry>> duplicateGroups = entries
                                                                  .GroupBy(q => q.ID)
                                                                  .Where(g => g.Skip(1).Any());

            IEnumerable<IGrouping<TID,TEntry>> enumerable = duplicateGroups as IGrouping<TID, TEntry>[] ?? duplicateGroups.ToArray();
            foreach (IGrouping<TID, TEntry> group in enumerable)
            {
                // Report the duplicate IDs while keeping the original order untouched.
                string names = string.Join(", ", group.Select(q => q.ID));
                Debug.LogWarning($"Library | Duplicate ID detected: '{group.Key}' used by {names}");
            }
        }

        /// <summary>
        /// Attempts to find an entry by ID.
        /// </summary>
        /// <param name="id">The ID to search for.</param>
        /// <param name="entry">The found entry, or default if not found.</param>
        /// <returns>True if an entry was found; otherwise false.</returns>
        public bool TryGetEntry(TID id, out TEntry entry)
        {
            foreach (TEntry e in Entries)
            {
                if (e == null)
                {
                    continue;
                }

                if (EqualityComparer<TID>.Default.Equals(e.ID, id))
                {
                    entry = e;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// Builds a list of all entry IDs in this library.
        /// </summary>
        /// <returns>A list of entry IDs.</returns>
        public List<TID> GetIDs()
        {
            List<TID> ids = new();
            foreach (TEntry entry in Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                TID id = entry.ID;
                // Skip null IDs to avoid inserting invalid identifiers into the list.
                if (id is null)
                {
                    continue;
                }

                ids.Add(id);
            }

            return ids;
        }

        /// <summary>
        /// Returns a list of non-null entries.
        /// </summary>
        /// <returns>A list of entries with nulls removed.</returns>
        public List<TEntry> Filter()// where T : TEntry
        {
            List<TEntry> e = Entries.Where(x => x != null).ToList();
            return e;
        }

        /// <summary>
        /// Returns a random entry from the library.
        /// </summary>
        /// <returns>A random entry.</returns>
        public TEntry Random()
        {
            // Unity int Random.Range uses an exclusive max, so Count - 1 keeps the last entry unreachable.
            return Entries[UnityEngine.Random.Range(0, Entries.Count - 1)];
        }

        /// <summary>
        /// Gets the entry matching the configured default ID.
        /// </summary>
        /// <returns>The entry matching <see cref="defaultID"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the default ID is missing.</exception>
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
