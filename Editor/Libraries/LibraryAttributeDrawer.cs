using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Fsi.DataSystem.Libraries
{
    [CustomPropertyDrawer(typeof(LibraryAttribute), true)]
    public abstract class LibraryAttributeDrawer<TID, TData> : PropertyDrawer 
        where TData : Object, ILibraryData<TID>
    {
        #region Constants
        
        private const string OpenPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Open_Icon.png";
        private const string HighlightPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Highlight_Icon.png";
        
        #endregion
        
        protected abstract Library<TID,TData> GetLibrary();

        #region IMGUI
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // IMGUI fallback / support for older Unity inspector UI.
            EditorGUI.BeginProperty(position, label, property);

            Library<TID, TData> library = GetLibrary();
            List<TData> data = library != null ? library.Entries : new List<TData>();

            // Build options: "None" + IDs
            List<string> names = new(data.Count + 1) { "None" };
            names.AddRange(data.Select(t => t != null ? t.ID.ToString() : "<Missing>"));

            // Current selection
            int selectedIndex = 0;
            if (property.objectReferenceValue != null && property.objectReferenceValue is ILibraryData<TID> current)
            {
                string currentId = current.ID.ToString();
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
                property.objectReferenceValue = dataIndex < 0 ? null : data[dataIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            // Buttons
            Texture2D selectIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(HighlightPath);
            Texture2D openIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenPath);

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
        
        #endregion
        
        #region UI Toolkit
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Library<TID, TData> library = GetLibrary();
            return new LibraryElement<TID, TData>(
                library,
                property.objectReferenceValue,
                selected =>
                {
                    property.objectReferenceValue = selected;
                    property.serializedObject.ApplyModifiedProperties();
                });
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
        
        private bool IsCollectionElement(SerializedProperty property)
        {
            return property.propertyPath.Contains("Array.data[");
        }
    }
}
