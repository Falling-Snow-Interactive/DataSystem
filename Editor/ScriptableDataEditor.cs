using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Adds quick action buttons for ScriptableData assets (popout and select).
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptableData<>), true)]
    public class ScriptableDataEditor : Editor
    {
        private const string OpenPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Open_Icon.png";
        private const string HighlightPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Highlight_Icon.png";
        private const string StylesheetPath = "Packages/com.fallingsnowinteractive.datasystem/Editor/ScriptableDataEditor.uss";
        
        [SerializeField]
        private Texture2D openTexture;

        [SerializeField]
        private Texture2D highlightTexture;
        
        // Toolbar
        private Toolbar toolbar;
        
        /// <summary>
        /// Builds the custom inspector UI with a toolbar and default fields.
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath);
            if (styleSheet)
            {
                root.styleSheets.Add(styleSheet);
            }

            toolbar = new Toolbar();
            root.Add(toolbar);

            openTexture ??= AssetDatabase.LoadAssetAtPath<Texture2D>(OpenPath);
            highlightTexture ??= AssetDatabase.LoadAssetAtPath<Texture2D>(HighlightPath);
            
            AddToolbarButton(openTexture, "Popout", OnPopoutButton);
            AddToolbarButton(highlightTexture, "Select", OnSelectButton);
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            return root;
        }

        #region Toolbar

        /// <summary>
        /// Adds a toolbar button to the inspector toolbar.
        /// </summary>
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

        /// <summary>
        /// Creates a toolbar button configured with icon, tooltip, and click handler.
        /// </summary>
        private static VisualElement CreateToolbarButton(Texture2D icon, string tooltip, Action onClickedAction)
        {
            ToolbarButton button = new(onClickedAction)
                                   {
                                       tooltip = tooltip,
                                       iconImage = icon,
                                   };
            button.AddToClassList("scriptable-data-editor__toolbar-button");
            return button;
        }
        
        #endregion

        #region Buttons Clicked Calls
        
        /// <summary>
        /// Opens the current target in a separate property editor window.
        /// </summary>
        private void OnPopoutButton()
        {
            if (target == null)
            {
                return;
            }
            
            EditorUtility.OpenPropertyEditor(target);
        }

        /// <summary>
        /// Pings the current target in the project window.
        /// </summary>
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
    }
}
