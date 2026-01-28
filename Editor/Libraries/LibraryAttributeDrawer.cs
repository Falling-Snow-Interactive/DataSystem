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
        private const string SelectSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Select_Sprite.png";
        private const string OpenSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Popout_Sprite.png";
        #endregion
        
        protected abstract Library<TID,TData> GetLibrary();

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
            for (int i = 0; i < data.Count; i++)
            {
                names.Add(data[i] != null ? data[i].ID.ToString() : "<Missing>");
            }

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

    }
}
