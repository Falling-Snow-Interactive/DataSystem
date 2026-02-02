using System.Collections.Generic;
using Fsi.DataSystem.Libraries;
using Fsi.DataSystem.Libraries.Browsers;
using Fsi.Localization;
using Fsi.Validation;
using UnityEngine;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Base ScriptableObject data entry that provides an ID and localized name/description for use
    /// in data-driven systems and selectors.
    /// </summary>
    /// <typeparam name="T">Type used to ID the data objects.</typeparam>
    public abstract class ScriptableData<T> : ScriptableObject, IData<T>, ISerializationCallbackReceiver
    {
        #region Inspector Fields
        
        [Tooltip("Unique identifier for this data entry.")]
        [SerializeField]
        private T id;
        
        /// <summary>
        /// Gets the unique identifier for this data entry.
        /// </summary>
        public virtual T ID => id;

        [Header("Localization")]

        [BrowserProperty(Group = "Localization", DisplayName = "Name")]
        [InspectorName("Name"), Tooltip("Localization entry used to get the display name of this data entry.")]
        [SerializeField]
        private LocEntry locName;
        
        /// <summary>
        /// Gets the localization entry for the display name.
        /// </summary>
        public LocEntry LocName => locName;
        
        /// <summary>
        /// Gets the localized display name or a fallback string if missing.
        /// </summary>
        public string Name => LocName == null ? "no_loc_name" : LocName.GetLocalizedString("no_loc_name");
        
        [BrowserProperty(Group = "Localization", DisplayName = "Description")]
        [InspectorName("Description"), Tooltip("Localized description text for this data entry.")]
        [SerializeField]
        private LocEntry locDesc;
        
        /// <summary>
        /// Gets the localization entry for the description.
        /// </summary>
        public LocEntry LocDesc => locDesc;

        /// <summary>
        /// Gets the localized description text or a fallback string if missing.
        /// </summary>
        public string Desc => LocDesc == null ? "no_loc_desc" : LocDesc.GetLocalizedString("no_loc_desc");

        [Header("Visuals")]

        [BrowserProperty(Width = 75f)]
        [SerializeField]
        private Color color = Color.gray;
        public Color Color => color;

        [SerializeField]
        private Sprite sprite;
        public Sprite Sprite => sprite;

        #endregion
        
        #region Equality & Operators

        public static bool operator ==(ScriptableData<T> a, ScriptableData<T> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return EqualityComparer<T>.Default.Equals(a.ID, b.ID);
        }
        public static bool operator !=(ScriptableData<T> a, ScriptableData<T> b) => !(a == b);

        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is ScriptableData<T> so
                   && EqualityComparer<T>.Default.Equals(ID, so.ID);
        }

        public override int GetHashCode()
            => ID is null ? base.GetHashCode() : EqualityComparer<T>.Default.GetHashCode(ID);

        #endregion
        
        #region Unity / Serialization

        public void OnBeforeSerialize()
        {
            // name = ToString();
        }

        public void OnAfterDeserialize() { }
        
        #endregion
        
        #region Object Overrides
        
        public override string ToString()
        {
            return ID == null ? "None" :  ID.ToString();
        }
        
        #endregion

        public abstract ValidatorResult Validate();
    }
}
