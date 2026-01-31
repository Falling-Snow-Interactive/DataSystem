using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Fsi.DataSystem.Libraries
{
    /// <summary>
    /// Draws a library selector for fields decorated with <see cref="LibraryAttribute"/>.
    /// </summary>
    [CustomPropertyDrawer(type: typeof(LibraryAttribute), useForChildren: true)]
    public abstract class LibraryAttributeDrawer<TID, TData> : PropertyDrawer 
        where TData : Object, ILibraryData<TID>
    {
        #region Constants
        
        private const string OpenPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Open_Icon.png";
        private const string HighlightPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Highlight_Icon.png";
        
        #endregion
        
        /// <summary>
        /// Resolves the library used to populate the selector.
        /// </summary>
        /// <returns>The library instance.</returns>
        protected abstract Library<TID,TData> GetLibrary();

        #region IMGUI
        
        /// <summary>
        /// Returns the height of the IMGUI popup row.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
        
        /// <summary>
        /// Draws the IMGUI fallback for the library selector.
        /// </summary>
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
        
        /// <summary>
        /// Builds the UI Toolkit selector for the property.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Library<TID, TData> library = GetLibrary();
            bool hideLabel = fieldInfo != null && fieldInfo.GetCustomAttributes(typeof(HideLabelAttribute), true).Length > 0;
            return new LibraryElement<TID, TData>(hideLabel ? "" : property.displayName,
                                                  library,
                                                  property.objectReferenceValue,
                                                  selected =>
                                                  {
                                                      // Push selection changes back into the serialized object.
                                                      property.objectReferenceValue = selected;
                                                      property.serializedObject.ApplyModifiedProperties();
                                                  });
        }
        
        #endregion
    }
}
