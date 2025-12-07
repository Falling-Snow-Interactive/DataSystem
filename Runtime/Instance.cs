using System;
using UnityEngine;

namespace Fsi.DataSystem
{
    [Serializable]
    public abstract class Instance<TID, TData> : ISerializationCallbackReceiver
        where TData : IDataEntry<TID>
    {
        [HideInInspector]
        [SerializeField]
        private string name;
        
        public abstract TData Data { get; set; }

        protected Instance(TData data)
        {
            Data = data;
        }

        public void OnBeforeSerialize()
        {
            name = Data == null ? "No Data" : $"{Data.ID}";
        }

        public void OnAfterDeserialize() { }
    }
}