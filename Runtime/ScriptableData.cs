using System;
using System.Collections.Generic;
using Fsi.DataSystem.Selectors;
using Fsi.Localization;
using UnityEngine;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Base ScriptableObject data entry that provides an ID and localized name/description for use
    /// in data-driven systems and selectors.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ScriptableData<T> : ScriptableObject, IDataEntry<T>, ISelectorData<T>, ISerializationCallbackReceiver
    {
        #region Asset Menu Constants
        
        protected const string Menu = "Fsi/";
        
        #endregion
        
        #region Inspector Fields

        [HideInInspector]
        [SerializeField]
        private new string name;
        
        [Header("Data")]
        
        [Tooltip("Unique identifier for this data entry.")]
        [SerializeField]
        private T id;
        
        [Header("Localization")]
        
        [Tooltip("Localization entry used to get the display name of this data entry.")]
        [SerializeField]
        private LocEntry locName;
        
        [Tooltip("Localized plural name. Falls back to the singular name if unset.")]
        [SerializeField]
        private LocEntry locPlural;
        
        [Tooltip("Localized description text for this data entry.")]
        [SerializeField]
        private LocEntry locDesc;

        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the unique identifier for this data entry.
        /// </summary>
        public T ID => id;
        
        /// <summary>
        /// Gets the localization entry used for this data entry's display name.
        /// </summary>
        public LocEntry LocName => locName;
        
        /// <summary>
        /// Gets the localization entry used for this data entry's plural name.
        /// Falls back to <see cref="LocName"/> if no plural is set.
        /// </summary>
        public LocEntry LocPlural =>locPlural.IsSet ? locPlural : LocName;
        
        /// <summary>
        /// Gets the localization entry used for this data entry's description.
        /// </summary>
        public LocEntry LocDesc => locDesc;
        
        /// <summary>
        /// Gets the localized display name for this data entry.
        /// </summary>
        public string Name => LocName.GetLocalizedString("no_loc_name");
        
        /// <summary>
        /// Gets the localized plural name for this data entry.
        /// </summary>
        public string Plural => LocPlural.GetLocalizedString("no_loc_plural");
        
        /// <summary>
        /// Gets the localized description text for this data entry.
        /// </summary>
        public string Desc => LocDesc.GetLocalizedString("no_loc_desc");
        
        #endregion
        
        #region Equality & Operators

        public static bool operator ==(ScriptableData<T> a, ScriptableData<T> b) => a && b && a.ID.Equals(b.ID);
        public static bool operator !=(ScriptableData<T> a, ScriptableData<T> b) => !(a == b);

        public override bool Equals(object other)
        {
            if (other is ScriptableData<T> so)
            {
                return base.Equals(other) && EqualityComparer<T>.Default.Equals(id, so.id);
            }

            return false;
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ID);

        #endregion
        
        #region Unity / Serialization

        public void OnBeforeSerialize()
        {
            name = ToString();
        }

        public void OnAfterDeserialize() { }
        
        #endregion
        
        #region Object Overrides
        
        public override string ToString()
        {
            return id.ToString();
        }
        
        #endregion
    }
}