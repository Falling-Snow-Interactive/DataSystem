using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using ObjectField = UnityEditor.Search.ObjectField;
using Spacer = Fsi.Ui.Dividers.Spacer;

namespace Fsi.DataSystem.Libraries
{
    [CustomPropertyDrawer(typeof(LibraryAttribute), true)]
    public class LibraryAttributeDrawer : PropertyDrawer
    {
        #region Constants
        private const string SelectSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Select_Sprite.png";
        private const string OpenSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Popout_Sprite.png";
        #endregion

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // IMGUI fallback / support for older Unity inspector UI.
            EditorGUI.BeginProperty(position, label, property);

            List<Object> data = GetLibraryEntries();

            // Build options: "None" + IDs
            List<string> names = new(data.Count + 1) { "None" };
            for (int i = 0; i < data.Count; i++)
            {
                names.Add(GetEntryId(data[i]));
            }

            // Current selection
            int selectedIndex = 0;
            if (property.objectReferenceValue != null)
            {
                string currentId = GetEntryId(property.objectReferenceValue);
                int found = names.IndexOf(currentId);
                selectedIndex = found >= 0 ? found : 0;
            }

            // Layout: label + popup + two icon buttons
            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            float buttonSize = EditorGUIUtility.singleLineHeight;
            const float spacing = 2f;

            Rect popupRect = contentRect;
            popupRect.width = Mathf.Max(0, contentRect.width - (buttonSize * 2f) - (spacing * 2f));

            Rect selectRect = contentRect;
            selectRect.x = popupRect.xMax + spacing;
            selectRect.width = buttonSize;

            Rect openRect = contentRect;
            openRect.x = selectRect.xMax + spacing;
            openRect.width = buttonSize;

            // Draw popup
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, selectedIndex, names.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                int dataIndex = newIndex - 1;
                property.objectReferenceValue = dataIndex < 0 || dataIndex >= data.Count ? null : data[dataIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            // Buttons
            Texture2D selectIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(SelectSpritePath);
            Texture2D openIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenSpritePath);

            GUIContent selectContent = new(selectIcon, "Select object in project.");
            GUIContent openContent = new(openIcon, "Open object window.");

            Object value = property.objectReferenceValue;

            using (new EditorGUI.DisabledScope(value == null))
            {
                if (GUI.Button(selectRect, selectContent))
                {
                    if (value != null)
                    {
                        EditorGUIUtility.PingObject(value);
                        Selection.activeObject = value;
                    }
                }

                if (GUI.Button(openRect, openContent))
                {
                    if (value != null)
                    {
                        EditorUtility.OpenPropertyEditor(value);
                    }
                }
            }

            EditorGUI.EndProperty();
        }
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            List<Object> data = GetLibraryEntries();
            List<string> names = new(data.Count + 1) { "None" };
            for (int i = 0; i < data.Count; i++)
            {
                names.Add(GetEntryId(data[i]));
            }
            
            int selectedIndex = 0;
            if (property.objectReferenceValue != null)
            {
                selectedIndex = names.IndexOf(GetEntryId(property.objectReferenceValue));
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }
            }

            ObjectField objectField = new(property.displayName)
                                      {
                                          objectType = fieldInfo.FieldType,
                                          value = property.objectReferenceValue
                                      };
            objectField.SetEnabled(false);

            VisualElement selection = new()
                                      {
                                          style =
                                          {
                                              flexGrow = 1,
                                              flexShrink = 0,

                                              flexDirection = FlexDirection.Row,

                                              height = EditorGUIUtility.singleLineHeight,
                                          }
                                      };
            root.Add(selection);

            DropdownField dropdown = new(names, selectedIndex)
            {
                style =
                {
                    flexGrow = 1,
                },
            };

            dropdown.RegisterValueChangedCallback(evt =>
                                                  {
                                                      int index = dropdown.index;
                                                      index -= 1;

                                                      property.objectReferenceValue = index == -1 || index >= data.Count
                                                                                          ? null
                                                                                          : data[index];
                                                      property.serializedObject.ApplyModifiedProperties();
                                                  });

            selection.Add(dropdown);

            selection.Add(new Spacer());

            VisualElement buttons = new()
                                    {
                                        style =
                                        {
                                            flexDirection = FlexDirection.Row,

                                            paddingTop = 0,
                                            paddingRight = 0,
                                            paddingBottom = 0,
                                            paddingLeft = 0,

                                            marginTop = 0,
                                            marginRight = 0,
                                            marginBottom = 0,
                                            marginLeft = 0,
                                        },
                                    };

            selection.Add(buttons);

            Texture2D selectSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(SelectSpritePath);
            VisualElement selectButton = CreateButton(selectSprite, () =>
                                                                    {
                                                                        if (objectField.value != null)
                                                                        {
                                                                            EditorGUIUtility.PingObject(objectField
                                                                                                            .value);
                                                                        }
                                                                    }, label: "", tooltip: "Select object in project.");

            Texture2D openSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenSpritePath);
            VisualElement openButton = CreateButton(openSprite, () =>
                                                         {
                                                             if (objectField.value)
                                                             {
                                                                 EditorUtility.OpenPropertyEditor(objectField.value);
                                                             }
                                                         }, label: "", tooltip: "Open object window.");

            buttons.Add(selectButton);
            buttons.Add(openButton);

            return root;
        }

        private VisualElement CreateButton(Texture2D icon, Action callback, string label = "", string tooltip = "")
        {
            const float margin = 1;
            const float padding = 0;
            
            Button button = new()
                                  {
                                      text = label,
                                      style =
                                      {
                                          flexGrow = 0,
                                          flexShrink = 0,
                                          
                                          width = EditorGUIUtility.singleLineHeight,
                                          
                                          paddingTop = padding,
                                          paddingRight = padding,
                                          paddingBottom = padding,
                                          paddingLeft = padding,
                                          
                                          marginTop = margin, 
                                          marginRight = margin, 
                                          marginBottom = margin, 
                                          marginLeft = margin,
                                      },
                                      tooltip = tooltip,
                                  };
            
            button.clicked += callback;

            Image image = new()
                          {
                              image = icon,
                              scaleMode = ScaleMode.ScaleToFit,
                              pickingMode = PickingMode.Ignore,
                              
                              style =
                              {
                                  flexShrink = 1,
                                  flexGrow = 1,
                                  
                                  width = Length.Auto(),
                                  height = Length.Auto(),
                                  
                                  paddingTop = 0,
                                  paddingRight = 0,
                                  paddingBottom = 0,
                                  paddingLeft = 0,
                                  
                                  marginTop = 0,
                                  marginRight = 0,
                                  marginBottom = 0,
                                  marginLeft = 0,
                              }
                          };

            button.Add(image);

            return button;
        }

        private List<Object> GetLibraryEntries()
        {
            List<Object> entries = new();

            if (fieldInfo == null)
            {
                return entries;
            }

            if (!LibraryUtility.TryGetLibrary(fieldInfo.FieldType, out object library) || library == null)
            {
                return entries;
            }

            if (GetEntriesEnumerable(library) is not IEnumerable enumerable)
            {
                return entries;
            }

            foreach (object entry in enumerable)
            {
                if (entry is Object unityObject)
                {
                    entries.Add(unityObject);
                }
                else
                {
                    entries.Add(null);
                }
            }

            return entries;
        }

        private static IEnumerable GetEntriesEnumerable(object library)
        {
            if (library == null)
            {
                return null;
            }

            PropertyInfo entriesProperty = library.GetType()
                                                  .GetProperty("Entries", BindingFlags.Instance | BindingFlags.Public);
            return entriesProperty?.GetValue(library) as IEnumerable;
        }

        private static string GetEntryId(Object entry)
        {
            if (entry == null)
            {
                return "<Missing>";
            }

            PropertyInfo idProperty = entry.GetType()
                                           .GetProperty("ID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object idValue = idProperty?.GetValue(entry);
            return idValue?.ToString() ?? "<Missing>";
        }
        
        private bool IsCollectionElement(SerializedProperty property)
        {
            return property.propertyPath.Contains("Array.data[");
        }
    }

    public abstract class LibraryAttributeDrawer<TID, TData> : PropertyDrawer
        where TData : Object, ILibraryData<TID>
    {
        protected virtual Library<TID, TData> GetLibrary()
        {
            LibraryUtility.TryGetLibrary(out Library<TID, TData> library);
            return library;
        }
    }
}
