using UnityEngine;

namespace Fsi.DataSystem.Ui
{
    public abstract class DataObjectWidget<TData, TID> : MonoBehaviour where TData : IDataEntry<TID>
    {
        public TData Data { get; protected set; }

        public virtual void Initialize(TData data)
        {
            Data = data;
        }
    }
}
