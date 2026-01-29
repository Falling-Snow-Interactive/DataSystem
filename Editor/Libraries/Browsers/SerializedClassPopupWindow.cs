using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries.Browsers
{
    public class SerializedClassPopupWindow : EditorWindow
    {
        private Object targetObject;
        private string propertyPath;

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

        private void Initialize(Object target, string path, string title)
        {
            targetObject = target;
            propertyPath = path;
            titleContent = new GUIContent(string.IsNullOrEmpty(title) ? "Serialized Property" : title);
            minSize = new Vector2(360f, 260f);
        }

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
            propertyField.RegisterCallback(callback);
            propertyField.userData = callback;

            rootVisualElement.Add(propertyField);
        }

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
