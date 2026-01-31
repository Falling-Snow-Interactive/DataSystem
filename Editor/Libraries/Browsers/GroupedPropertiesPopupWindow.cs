using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries.Browsers
{
    /// <summary>
    /// Utility window that displays grouped serialized properties in a popup inspector.
    /// </summary>
    internal class GroupedPropertiesPopupWindow : EditorWindow
    {
        private Object targetObject;
        private string groupTitle;
        private List<(string PropertyPath, string DisplayName)> entries;

        /// <summary>
        /// Opens a popup inspector for the specified grouped properties.
        /// </summary>
        public static void Show(Object target,
                                string groupTitle,
                                IReadOnlyList<(string PropertyPath, string DisplayName)> entries)
        {
            if (!target || entries == null || entries.Count == 0)
            {
                return;
            }

            GroupedPropertiesPopupWindow window = CreateInstance<GroupedPropertiesPopupWindow>();
            window.Initialize(target, groupTitle, entries);
            window.ShowUtility();
        }

        /// <summary>
        /// Initializes the popup with a target and group data.
        /// </summary>
        private void Initialize(Object target,
                                string title,
                                IReadOnlyList<(string PropertyPath, string DisplayName)> propertyEntries)
        {
            targetObject = target;
            groupTitle = string.IsNullOrWhiteSpace(title) ? "Grouped Properties" : title;
            entries = new List<(string PropertyPath, string DisplayName)>(propertyEntries);
            titleContent = new GUIContent(groupTitle);
            minSize = new Vector2(360f, 260f);
        }

        /// <summary>
        /// Builds the popup UI for the grouped properties.
        /// </summary>
        public void CreateGUI()
        {
            if (!targetObject)
            {
                rootVisualElement.Add(new HelpBox("Target object is missing.", HelpBoxMessageType.Warning));
                return;
            }

            if (entries == null || entries.Count == 0)
            {
                rootVisualElement.Add(new HelpBox("No grouped properties available.", HelpBoxMessageType.Warning));
                return;
            }

            var serializedObject = new SerializedObject(targetObject);
            serializedObject.Update();

            foreach ((string propertyPath, string displayName) in entries)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    rootVisualElement.Add(new HelpBox($"Missing property: {propertyPath}", HelpBoxMessageType.Warning));
                    continue;
                }

                var propertyField = new PropertyField(property)
                                    {
                                        label = string.IsNullOrWhiteSpace(displayName)
                                                    ? property.displayName
                                                    : displayName
                                    };
                propertyField.BindProperty(property);

                EventCallback<SerializedPropertyChangeEvent> callback = _ =>
                                                                        {
                                                                            serializedObject.ApplyModifiedProperties();
                                                                            MarkDirty(targetObject);
                                                                        };
                propertyField.RegisterCallback(callback);
                propertyField.userData = callback;

                rootVisualElement.Add(propertyField);
            }
        }

        /// <summary>
        /// Marks the target asset dirty and saves.
        /// </summary>
        private static void MarkDirty(Object target)
        {
            if (!target)
            {
                return;
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}
