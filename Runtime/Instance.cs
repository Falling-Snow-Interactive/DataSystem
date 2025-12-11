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

        // This is the field we'll customize via UI Toolkit
        [LibraryReference]
        [SerializeField]
        private TData data;
        public TData Data => data;

        protected Instance(TData data)
        {
            this.data = data;
        }

        public void OnBeforeSerialize()
        {
            name = Data == null ? "No Data" : $"{Data.ID}";
        }

        public void OnAfterDeserialize() { }
    }
}