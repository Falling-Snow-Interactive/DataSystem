using System;
using Fsi.DataSystem.Libraries;
using Fsi.Validation;
using JetBrains.Annotations;
using UnityEngine;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Wraps a library data reference for runtime use while exposing a serialized display name.
    /// </summary>
    /// <typeparam name="TID">The identifier type for the library entry.</typeparam>
    /// <typeparam name="TData">The data type stored in the library.</typeparam>
    [Serializable]
    public abstract class Instance<TID, TData> : ISerializationCallbackReceiver
        where TData : IData<TID>
    {
        [HideInInspector, UsedImplicitly]
        [SerializeField]
        private string name;
        
        /// <summary>
        /// Gets or sets the runtime library entry backing this instance.
        /// </summary>
        public abstract TData Data { get; set; }

        protected Instance(TData data)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Data = data;
        }

        /// <summary>
        /// Writes a serialized display name based on the current data ID for inspector debugging.
        /// </summary>
        public virtual void OnBeforeSerialize()
        {
            // Store a readable ID string so the wrapper is identifiable in serialized data/inspector views.
            name = Data == null ? "No Data" : $"{Data.ID}";
        }

        /// <summary>
        /// Called after Unity deserializes the instance.
        /// </summary>
        public void OnAfterDeserialize() { }

        public abstract ValidatorResult Validate();
    }
}
