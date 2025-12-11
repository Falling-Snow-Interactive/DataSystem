using System;
using Fsi.DataSystem.Libraries;
using UnityEngine;

namespace Fsi.DataSystem
{
    [Serializable]
    public abstract class Instance<TID, TData> : ISerializationCallbackReceiver
        where TData : ILibraryData<TID>
    {
        [HideInInspector]
        [SerializeField]
        private string name;

        // [SerializeField]
        // private TData data;
        // public TData Data => data;
        
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