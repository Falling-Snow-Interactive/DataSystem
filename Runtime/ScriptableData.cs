using System;
using System.Collections.Generic;
using Fsi.DataSystem.Selectors;
using Fsi.Localization;
using UnityEngine;

namespace Fsi.DataSystem
{
    public class ScriptableData<T> : ScriptableObject, IFsiData<T>, ISelectorData<T>, ISerializationCallbackReceiver
    {
        [HideInInspector]
        [SerializeField]
        private string name;
        
        [Header("Data")]

        [SerializeField]
        private T id;
        public T ID => id;
        
        [Header("Localization")]
        
        [SerializeField]
        private LocEntry locName;
        public LocEntry LocName => locName;
        public string Name => locName.GetLocalizedString("no_loc_name");

        [SerializeField]
        private LocEntry locDescription;
        public LocEntry LocDesc => locDescription;
        public string Desc => locDescription.GetLocalizedString("no_loc_description");
        
        #region Operators

        public static bool operator ==(ScriptableData<T> a, ScriptableData<T> b)
        {
            return a && b && a.ID.Equals(b.ID);
        }

        public static bool operator !=(ScriptableData<T> a, ScriptableData<T> b)
        {
            return !(a == b);
        }

        public override bool Equals(object other)
        {
            if (other is ScriptableData<T> so)
            {
                return base.Equals(other)
                       && EqualityComparer<T>.Default.Equals(id, so.id)
                       && Equals(locName, so.locName)
                       && Equals(locDescription, so.locDescription);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), id, locName, locDescription);
        }

        #endregion

        public override string ToString()
        {
            return id.ToString();
        }

        public void OnBeforeSerialize()
        {
            name = ToString();
        }

        public void OnAfterDeserialize() { }
    }
}