using System;
using Fsi.DataSystem.Libraries;
using JetBrains.Annotations;
using UnityEngine;

namespace Fsi.DataSystem
{
    [Serializable]
    public abstract class Instance<TID, TData> : ISerializationCallbackReceiver
        where TData : ILibraryData<TID>
    {
        [HideInInspector, UsedImplicitly]
        [SerializeField]
        private string name;
        
        public abstract TData Data { get; set; }

        protected Instance(TData data)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Data = data;
        }

        public virtual void OnBeforeSerialize()
        {
            name = Data == null ? "No Data" : $"{Data.ID}";
        }

        public void OnAfterDeserialize() { }
    }
}