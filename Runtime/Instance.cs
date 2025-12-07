using System;
using UnityEngine;

namespace Fsi.DataSystem
{
    [Serializable]
    public class Instance<TID, TData> : ISerializationCallbackReceiver
        where TData : IDataEntry<TID>
    {
        [HideInInspector]
        [SerializeField]
        private string name;
        
        [SerializeField]
        private TData data;
        public TData Data => data;
        
        public Instance(TData data)
        {
            this.data = data;
        }

        public void OnBeforeSerialize()
        {
            name = data == null ? "No Data" : $"{Data.ID}";
        }

        public void OnAfterDeserialize() { }
    }
}