using System;
using Fsi.Ui.Spacers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.AssetDatabase;

namespace Fsi.DataSystem
{
    [CustomEditor(typeof(ScriptableData<>), true)]
    public class ScriptableDataEditor : Editor
    {
        #region Constants
        
        private const float IconSize = 24f;
        
        private const string PopoutIcon = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Popout_Sprite.png";
        private const string SelectIcon = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Select_Sprite.png";

        private const float ImagePadding = 4f;
        
        #endregion

        private Toggle pluralToggle;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            VisualElement header = CreateHeader();
            root.Add(header);
            
            root.Add(new Spacer());
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            return root;
        }

        #region Create Header
        
        // TODO - This can probably use the toolbar stuff actually. Let's try that.
        private VisualElement CreateHeader()
        {
            VisualElement header = new()
                                   {
                                       style =
                                       {
                                           height = IconSize,

                                           flexDirection = FlexDirection.Row,
                                       }
                                   };

            // Popout
            Texture2D popoutIcon = LoadAssetAtPath<Texture2D>(PopoutIcon);
            VisualElement popoutButton = CreateHeaderButton(popoutIcon, "Popout", OnPopoutButton);
            header.Add(popoutButton);
            
            // Select
            Texture2D selectIcon = LoadAssetAtPath<Texture2D>(SelectIcon);
            VisualElement selectButton = CreateHeaderButton(selectIcon, "Select", OnSelectButton);
            header.Add(selectButton);

            return header;
        }

        private static VisualElement CreateHeaderButton(Texture2D icon, string tooltip, Action onClickedAction)
        {
            Button button = new()
                                  {
                                      tooltip = tooltip,
                                      style =
                                      {
                                          width = IconSize,
                                          height = IconSize,
                                          
                                          paddingTop = ImagePadding,
                                          paddingRight = ImagePadding,
                                          paddingBottom = ImagePadding,
                                          paddingLeft = ImagePadding,
                                          
                                          alignContent = Align.Center,
                                      }
                                  };
            
            Image image = new() { image = icon, };
            button.Add(image);
            
            button.clicked += onClickedAction;
            
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