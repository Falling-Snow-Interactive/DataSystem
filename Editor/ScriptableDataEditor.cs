using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptableData<>), true)]
    public class ScriptableDataEditor : Editor
    {
        [SerializeField]
        private Texture2D openTexture;

        [SerializeField]
        private Texture2D highlightTexture;
        
        // Toolbar
        private Toolbar toolbar;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            toolbar = new Toolbar();
            root.Add(toolbar);
            
            AddToolbarButton(openTexture, "Popout", OnPopoutButton);
            AddToolbarButton(highlightTexture, "Select", OnSelectButton);
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            return root;
        }

        #region Toolbar

        private void AddToolbarButton(Texture2D icon, string tooltip, Action onClickedAction)
        {
            if (toolbar == null)
            {
                Debug.LogWarning($"Toolbar does not exist on {name}.", target);
                return;
            }

            VisualElement b = CreateToolbarButton(icon, tooltip, onClickedAction);
            toolbar.Add(b);
        }

        private static VisualElement CreateToolbarButton(Texture2D icon, string tooltip, Action onClickedAction)
        {
            ToolbarButton button = new(onClickedAction)
                                   {
                                       tooltip = tooltip,
                                       iconImage = icon,
                                       
                                       style =
                                       {
                                           height = new StyleLength(StyleKeyword.Auto),
                                       }
                                   };
            return button;
        }
        
        #endregion

        #region Buttons Clicked Calls
        
        private void OnPopoutButton()
        {
            if (target == null)
            {
                return;
            }
            
            EditorUtility.OpenPropertyEditor(target);
        }

        private void OnSelectButton()
        {
            if (target == null)
            {
                return;
            }
            
            if (serializedObject.targetObject)
            {
                EditorGUIUtility.PingObject(serializedObject.targetObject);
            }
        }
        
        #endregion
        
        #region Helpers
        
        private static void ShowPropertyByPath(bool show, VisualElement root, string propertyPath)
        {
            if (TryGetPropertyByPath(root, propertyPath, out PropertyField field))
            {
                field.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        private static bool TryGetPropertyByPath(VisualElement root, string propertyPath, out PropertyField field)
        {
            field = root
                    .Query<PropertyField>()
                    .Where(p => p.bindingPath == propertyPath)
                    .First();

            return field != null;
        }
        
        #endregion
    }
}