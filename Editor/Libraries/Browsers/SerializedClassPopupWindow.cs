using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries.Browsers
{
    /// <summary>
    /// Utility window that displays a serialized property in a popup inspector.
    /// </summary>
    public class SerializedClassPopupWindow : EditorWindow
    {
        private Object targetObject;
        private string propertyPath;

        /// <summary>
        /// Opens a popup inspector for the specified serialized property.
        /// </summary>
        public static void Show(Object target, string propertyPath, string title)
        {
            if (!target || string.IsNullOrEmpty(propertyPath))
            {
                return;
            }

            SerializedClassPopupWindow window = CreateInstance<SerializedClassPopupWindow>();
            window.Initialize(target, propertyPath, title);
            window.ShowUtility();
        }

        /// <summary>
        /// Initializes the popup with a target and property path.
        /// </summary>
        private void Initialize(Object target, string path, string title)
        {
            targetObject = target;
            propertyPath = path;
            titleContent = new GUIContent(string.IsNullOrEmpty(title) ? "Serialized Property" : title);
            minSize = new Vector2(360f, 260f);
        }

        /// <summary>
        /// Builds the popup UI for the serialized property.
        /// </summary>
        public void CreateGUI()
        {
            if (!targetObject)
            {
                rootVisualElement.Add(new HelpBox("Target object is missing.", HelpBoxMessageType.Warning));
                return;
            }

            var serializedObject = new SerializedObject(targetObject);
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                rootVisualElement.Add(new HelpBox($"Missing property: {propertyPath}", HelpBoxMessageType.Warning));
                return;
            }

            var propertyField = new PropertyField(property)
                                {
                                    label = property.displayName
                                };
            propertyField.BindProperty(property);

            EventCallback<SerializedPropertyChangeEvent> callback = _ =>
                                                                    {
                                                                        serializedObject.ApplyModifiedProperties();
                                                                        MarkDirty(targetObject);
                                                                    };
            // Store the callback so it can be cleaned up if the element is rebuilt.
            propertyField.RegisterCallback(callback);
            propertyField.userData = callback;

            rootVisualElement.Add(propertyField);
        }

        /// <summary>
        /// Marks the target asset dirty and saves it.
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
