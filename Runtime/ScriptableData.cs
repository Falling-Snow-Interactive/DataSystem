using Fsi.DataSystem.Selectors;
using Fsi.Localization;
using UnityEngine;

namespace Fsi.DataSystem
{
    public class ScriptableData<T> : ScriptableObject, IFsiData<T>, ISelectorData<T>
    {
        [Header("Data")]

        [SerializeField]
        private T id;
        public T Id => id;
        
        [Header("Localization")]
        
        [SerializeField]
        private LocEntry locName;
        public LocEntry LocName => locName;

        [SerializeField]
        private LocEntry locDescription;
        public LocEntry LocDesc => locDescription;
    }
}