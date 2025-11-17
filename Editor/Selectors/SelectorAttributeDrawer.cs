using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.Search.ObjectField;

namespace Fsi.DataSystem.Selectors
{
    [CustomPropertyDrawer(typeof(SelectorAttribute))]
    public abstract class SelectorAttributeDrawer<TType, TId> : PropertyDrawer where TType : Object, ISelectorData<TId>
    {
        protected abstract List<TType> GetData();
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new(); // {style = { flexDirection = FlexDirection.Row}};
            
            List<TType> data = GetData(); 
            List<string> names = data.Select(d => d.ID.ToString()).ToList();
            names.Insert(0, "None");

            ObjectField objectField = new("Data")
                                              {
                                                  objectType = typeof(TType),
                                                  value = property.objectReferenceValue
                                              };
            objectField.SetEnabled(false); // Optional: make it read-only
            
            VisualElement selection = new() { style = { flexDirection = FlexDirection.Row } };
            root.Add(selection);
            
            int selectedIndex = 0;
            if (property.objectReferenceValue != null && property.objectReferenceValue is ISelectorData<TId> t)
            {
                selectedIndex = names.IndexOf(t.ID.ToString());
            }
            
            DropdownField dropdown = new(names, selectedIndex)
            {
                label = property.displayName,
                style =
                {
                    flexGrow = 1,
                },
            };
            
            Button selectButton = new()
            {
                text = "Select",
                style =
                {
                    flexGrow = 0,
                    flexShrink = 0,
                    width = 50f,
                },
                enabledSelf = objectField.value,
            };
            
            selectButton.clicked += () =>
            {
                if (objectField.value)
                {
                    EditorGUIUtility.PingObject(objectField.value);
                    // Selection.activeObject = objectField.value;
                }
            };
            
            // Set initial?
            dropdown.RegisterValueChangedCallback(evt =>
                                                  {
                                                      int index = names.IndexOf(evt.newValue) - 1;
                                                      if (index < 0)
                                                      {
                                                          property.objectReferenceValue = null;
                                                          selectButton.enabledSelf = false;
                                                          property.serializedObject.ApplyModifiedProperties();
                                                      }
                                                      else
                                                      {
                                                          TType newSelected = data[index];
                                                          selectButton.enabledSelf = true;
                                                          property.objectReferenceValue = newSelected;
                                                      }
                                                      property.serializedObject.ApplyModifiedProperties();
                                                  });
            
            selection.Add(dropdown);
            selection.Add(selectButton);
            
            return root;
        }
    }
}
