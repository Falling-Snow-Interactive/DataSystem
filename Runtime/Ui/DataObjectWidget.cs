using Fsi.DataSystem.Libraries;
using UnityEngine;

namespace Fsi.DataSystem.Ui
{
    /// <summary>
    /// Base UI widget for displaying or editing a library data object.
    /// </summary>
    /// <typeparam name="TData">The data type displayed by this widget.</typeparam>
    /// <typeparam name="TID">The identifier type for the data.</typeparam>
    public abstract class DataObjectWidget<TData, TID> : MonoBehaviour where TData : IData<TID>
    {
        /// <summary>
        /// Gets the data currently bound to this widget.
        /// </summary>
        public TData Data { get; protected set; }

        /// <summary>
        /// Initializes the widget with a new data object.
        /// </summary>
        /// <param name="data">The data object to bind.</param>
        public virtual void Initialize(TData data)
        {
            Data = data;
        }
    }
}
