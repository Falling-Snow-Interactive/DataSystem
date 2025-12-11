using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries
{
    public static class LibraryUtility
    {
        public static DropdownField BuildLibraryDropdown<TId, TData>(string label,
                                                                     Library<TId, TData> library,
                                                                     TData currentValue,
                                                                     Func<TData, string> getLabel,
                                                                     Action<TData> onChanged) 
            where TData : ILibraryData<TId>
        {
            List<TData> entries = library.Entries;
            List<string> names = entries.Select(getLabel).ToList();
            names.Insert(0, "None");

            int index = 0;
            if (currentValue != null)
            {
                TId id = currentValue.ID;
                index = entries.FindIndex(e => Equals(e.ID, id));
                if (index >= 0) index += 1; // +1 for "None"
            }

            DropdownField dropdown = new(names, index)
                                     {
                                         label = label,
                                         style = { flexGrow = 1 },
                                     };

            dropdown.RegisterValueChangedCallback(_ =>
                                                  {
                                                      int newIndex = dropdown.index - 1;
                                                      TData newValue = newIndex < 0 ? default : entries[newIndex];
                                                      onChanged?.Invoke(newValue);
                                                  });

            return dropdown;
        }
    }
}