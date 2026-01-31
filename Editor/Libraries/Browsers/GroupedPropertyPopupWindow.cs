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
    internal class GroupedPropertyPopupWindow : EditorWindow
    {
        private Object targetObject;
        private string groupTitle;
        private List<GroupedPropertyDefinition> properties;

        /// <summary>
        /// Opens a popup inspector for the specified grouped properties.
        /// </summary>
        public static void Show(Object target, string groupTitle, IReadOnlyList<GroupedPropertyDefinition> properties)
        {
            if (!target || properties == null || properties.Count == 0)
            {
                return;
            }

            GroupedPropertyPopupWindow window = CreateInstance<GroupedPropertyPopupWindow>();
            window.Initialize(target, groupTitle, properties);
            window.ShowUtility();
        }

        /// <summary>
        /// Initializes the popup with a target and group data.
        /// </summary>
        private void Initialize(Object target, string title, IReadOnlyList<GroupedPropertyDefinition> propertyDefinitions)
        {
            targetObject = target;
            groupTitle = string.IsNullOrWhiteSpace(title) ? "Grouped Properties" : title;
            properties = new List<GroupedPropertyDefinition>(propertyDefinitions);
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

            if (properties == null || properties.Count == 0)
            {
                rootVisualElement.Add(new HelpBox("No grouped properties available.", HelpBoxMessageType.Warning));
                return;
            }

            var serializedObject = new SerializedObject(targetObject);
            serializedObject.Update();

            foreach (GroupedPropertyDefinition definition in properties)
            {
                SerializedProperty property = serializedObject.FindProperty(definition.PropertyPath);
                if (property == null)
                {
                    rootVisualElement.Add(new HelpBox($"Missing property: {definition.PropertyPath}", HelpBoxMessageType.Warning));
                    continue;
                }

                var propertyField = new PropertyField(property)
                                    {
                                        label = string.IsNullOrWhiteSpace(definition.DisplayName)
                                                    ? property.displayName
                                                    : definition.DisplayName
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

    internal readonly struct GroupedPropertyDefinition
    {
        public GroupedPropertyDefinition(string propertyPath, string displayName)
        {
            PropertyPath = propertyPath;
            DisplayName = displayName;
        }

        public string PropertyPath { get; }
        public string DisplayName { get; }
    }
}
