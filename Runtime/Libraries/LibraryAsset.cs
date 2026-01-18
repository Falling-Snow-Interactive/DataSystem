using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fsi.DataSystem.Libraries
{
    [CreateAssetMenu(menuName = "Falling Snow Interactive/Data System/Library Asset", fileName = "NewLibrary")]
    public class LibraryAsset : ScriptableObject
    {
        [SerializeField]
        private string dataTypeName;

        [SerializeField]
        private List<Object> entries = new();

        public List<Object> Entries => entries;

        public string DataTypeName => dataTypeName;

        public Type DataType => string.IsNullOrWhiteSpace(dataTypeName) ? null : Type.GetType(dataTypeName);

        public void SetDataType(Type dataType)
        {
            dataTypeName = dataType?.AssemblyQualifiedName;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (DataType == null)
            {
                return;
            }

            entries.RemoveAll(entry => entry != null && !DataType.IsInstanceOfType(entry));
        }
#endif
    }
}
