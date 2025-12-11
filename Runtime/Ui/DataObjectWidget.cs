using Fsi.DataSystem.Libraries;
using UnityEngine;

namespace Fsi.DataSystem.Ui
{
    public abstract class DataObjectWidget<TData, TID> : MonoBehaviour where TData : ILibraryData<TID>
    {
        public TData Data { get; protected set; }

        public virtual void Initialize(TData data)
        {
            Data = data;
        }
    }
}
