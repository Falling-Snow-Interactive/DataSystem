using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Selectors
{
    [CustomPropertyDrawer(typeof(SelectorAttribute))]
    public abstract class SelectorAttributeDrawer<TType, TId> : PropertyDrawer where TType : Object, ISelectorData<TId>
    {
        protected abstract List<TType> GetData();
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            List<TType> data = GetData(); 
            List<string> names = data.Select(d => d.Id.ToString()).ToList();
            names.Insert(0, "None");

            int selectedIndex = 0;
            if (property.objectReferenceValue != null
                && property.objectReferenceValue is ISelectorData<TId> t)
            {
                selectedIndex = names.IndexOf(t.Id.ToString());
            }

            ObjectField objectField = new("Data")
                                              {
                                                  objectType = typeof(TType),
                                                  value = property.objectReferenceValue
                                              };
            objectField.SetEnabled(false); // Optional: make it read-only
			
            DropdownField dropdown = new(names, selectedIndex){label = property.displayName};
            dropdown.RegisterValueChangedCallback(evt =>
                                                  {
                                                      int index = names.IndexOf(evt.newValue) - 1;
                                                      if (index < 0)
                                                      {
                                                          property.objectReferenceValue = null;
                                                          property.serializedObject.ApplyModifiedProperties();
                                                      }
                                                      else
                                                      {
                                                          TType newSelected = data[index];
                                                          property.objectReferenceValue = newSelected;
                                                      }
                                                      property.serializedObject.ApplyModifiedProperties();
                                                  });
            root.Add(dropdown);
            return root;
        }
    }
}
