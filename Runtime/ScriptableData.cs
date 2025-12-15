using System;
using System.Collections.Generic;
using Fsi.DataSystem.Libraries;
using Fsi.Localization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Base ScriptableObject data entry that provides an ID and localized name/description for use
    /// in data-driven systems and selectors.
    /// </summary>
    /// <typeparam name="T">Type used to ID the data objects.</typeparam>
    public abstract class ScriptableData<T> : ScriptableObject, 
                                              ILibraryData<T>, 
                                              ISerializationCallbackReceiver
    {
        #region Asset Menu Constants
        
        protected const string Menu = "Falling Snow Interactive/";
        
        #endregion
        
        #region Inspector Fields
        
        [Tooltip("Unique identifier for this data entry.")]
        [SerializeField]
        private T id;
        
        [Header("Localization")]
        
        [InspectorName("Name"), Tooltip("Localization entry used to get the display name of this data entry.")]
        [SerializeField]
        private LocEntry locName;
        
        [InspectorName("Plural"), Tooltip("Localized plural name. Falls back to the singular name if unset.")]
        [SerializeField]
        private LocEntry locPlural;
        
        [InspectorName("Description"), Tooltip("Localized description text for this data entry.")]
        [SerializeField]
        private LocEntry locDesc;

        [Header("Visuals")]

        [SerializeField]
        private Color color = Color.gray;

        [FormerlySerializedAs("icon")]
        [SerializeField]
        private Sprite sprite;

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets the unique identifier for this data entry.
        /// </summary>
        public virtual T ID
        {
            get => id;
            protected set => id = value;
        }

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

        public Color Color => color;
        
        public Sprite Sprite => sprite;

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
        
        public virtual void Validate()
        {
            List<string> errors = new();

            if (ID == null)
            {
                string msg = $"ID on {name} is null.";
                Debug.LogError(msg, this);
                errors.Add(msg);
            }

            if (!LocName.IsSet)
            {
                string msg = $"LocName not set on {name}.";
                Debug.LogError(msg, this);
                errors.Add(msg);
            }

            if (errors.Count > 0)
            {
                Assert.Fail(string.Join("\n", errors));
            }
        }
    }
}