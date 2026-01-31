using System;
using System.Collections.Generic;
using Fsi.DataSystem.Libraries;
using Fsi.DataSystem.Libraries.Browsers;
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
                                              IData<T>, 
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

        [BrowserProperty(Popup = true, Width = 75f)]
        [SerializeField]
        private LocProperties loc;
        public LocProperties Loc => loc;

        [Header("Visuals")]

        [SerializeField]
        private Color color = Color.gray;

        [SerializeField]
        private Sprite sprite;

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets the unique identifier for this data entry.
        /// </summary>
        public virtual T ID => id;

        public Color Color => color;
        
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
    }
}
