using System;
using Fsi.DataSystem.Libraries;
using Fsi.Localization;
using UnityEngine;

namespace Fsi.DataSystem
{
    [Serializable]
    public class SerializableDataEntry<T> : ILibraryData<T>
    {
        [Header("Data")]

        [SerializeField]
        private T id;
        public T ID => id;

        [Header("Localization")]
        
        [SerializeField]
        private LocEntry locName;
        public LocEntry LocName => locName;

        [SerializeField]
        private LocEntry locDescription;
        public LocEntry LocDesc => locDescription;

        public override string ToString()
        {
            string s = id.ToString();
            return s;
        }
    }
}