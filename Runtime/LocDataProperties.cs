using System;
using Fsi.Localization;
using UnityEngine;

namespace Fsi.DataSystem
{
    [Serializable]
    public class LocDataProperties
    {
        [InspectorName("Name"), Tooltip("Localization entry used to get the display name of this data entry.")]
        [SerializeField]
        private LocEntry locName;
        public LocEntry LocName => locName;
        
        [InspectorName("Description"), Tooltip("Localized description text for this data entry.")]
        [SerializeField]
        private LocEntry locDesc;
        public LocEntry LocDesc => locDesc;
        
        public string Name => LocName.GetLocalizedString("no_loc_name");
        
        /// <summary>
        /// Gets the localized description text for this data entry.
        /// </summary>
        public string Desc => LocDesc.GetLocalizedString("no_loc_desc");
    }
}