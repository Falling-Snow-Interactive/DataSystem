using System;
using Fsi.DataSystem.Libraries;
using Fsi.Localization;
using UnityEngine;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Serializable inline data entry with optional localization references.
    /// </summary>
    /// <typeparam name="T">The identifier type for this data entry.</typeparam>
    [Serializable]
    public class SerializableDataEntry<T> : ILibraryData<T>
    {
        [Header("Data")]

        [SerializeField]
        private T id;
        /// <summary>
        /// Gets the identifier for this inline data entry.
        /// </summary>
        public T ID => id;

        [Header("Localization")]
        
        [SerializeField]
        private LocEntry locName;
        /// <summary>
        /// Gets the localization entry for the display name.
        /// </summary>
        public LocEntry LocName => locName;

        [SerializeField]
        private LocEntry locDescription;
        /// <summary>
        /// Gets the localization entry for the description text.
        /// </summary>
        public LocEntry LocDesc => locDescription;

        /// <summary>
        /// Returns the ID as a string for debugging.
        /// </summary>
        /// <returns>The ID string.</returns>
        public override string ToString()
        {
            string s = id.ToString();
            return s;
        }
    }
}
