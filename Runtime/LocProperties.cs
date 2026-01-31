using System;
using Fsi.Localization;
using UnityEngine;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Localization references and helpers for displaying names and descriptions.
    /// </summary>
    [Serializable]
    public class LocProperties
    {
        [InspectorName("Name"), Tooltip("Localization entry used to get the display name of this data entry.")]
        [SerializeField]
        private LocEntry locName;
        
        /// <summary>
        /// Gets the localization entry for the display name.
        /// </summary>
        public LocEntry LocName => locName;
        
        [InspectorName("Description"), Tooltip("Localized description text for this data entry.")]
        [SerializeField]
        private LocEntry locDesc;
        
        /// <summary>
        /// Gets the localization entry for the description.
        /// </summary>
        public LocEntry LocDesc => locDesc;
        
        /// <summary>
        /// Gets the localized display name or a fallback string if missing.
        /// </summary>
        public string Name => LocName.GetLocalizedString("no_loc_name");
        
        /// <summary>
        /// Gets the localized description text or a fallback string if missing.
        /// </summary>
        public string Desc => LocDesc.GetLocalizedString("no_loc_desc");
    }
}
