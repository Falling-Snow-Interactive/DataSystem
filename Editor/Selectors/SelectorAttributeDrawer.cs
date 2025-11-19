using System;
using System.Collections.Generic;
using System.Linq;
using Fsi.Ui.Spacers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using ObjectField = UnityEditor.Search.ObjectField;

namespace Fsi.DataSystem.Selectors
{
    [CustomPropertyDrawer(typeof(SelectorAttribute))]
    public abstract class SelectorAttributeDrawer<TType, TId> : PropertyDrawer where TType : Object, ISelectorData<TId>
    {
        private const string SelectSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Select_Sprite.png";
        private const string OpenSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Popout_Sprite.png";
        
        protected abstract List<TType> GetEntries();
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new(); // {style = { flexDirection = FlexDirection.Row}};
            
            List<TType> data = GetEntries(); 
            List<string> names = data.Select(d => d.ID.ToString()).ToList();
            names.Insert(0, "None");

            ObjectField objectField = new("Data")
                                              {
                                                  objectType = typeof(TType),
                                                  value = property.objectReferenceValue
                                              };
            objectField.SetEnabled(false); // Optional: make it read-only
            
            VisualElement selection = new()
                                      {
                                          style =
                                          {
                                              flexGrow = 0,
                                              flexShrink = 0,
                                              
                                              flexDirection = FlexDirection.Row,
                                              
                                              height = EditorGUIUtility.singleLineHeight,
                                          }
                                      };
            root.Add(selection);
            
            int selectedIndex = 0;
            if (property.objectReferenceValue != null && property.objectReferenceValue is ISelectorData<TId> t)
            {
                selectedIndex = names.IndexOf(t.ID.ToString());
            }
            
            DropdownField dropdown = new(names, selectedIndex)
            {
                label = property.displayName,
                style =
                {
                    flexGrow = 1,
                },
            };
            
            selection.Add(dropdown);

            selection.Add(new Spacer(3, SpacerOrientation.Vertical, SpacerColor.Dark));

            VisualElement buttons = new()
                                    {
                                        style =
                                        {
                                            flexDirection = FlexDirection.Row,
                                            
                                            paddingTop = 0,
                                            paddingRight = 0,
                                            paddingBottom = 0,
                                            paddingLeft = 0,
                                          
                                            marginTop = 0, 
                                            marginRight = 0, 
                                            marginBottom = 0, 
                                            marginLeft = 0,
                                        },
                                    };

            selection.Add(buttons);

            Texture2D selectSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(SelectSpritePath);
            VisualElement selectButton = CreateButton(selectSprite, () =>
                                                                    {
                                                                        if (objectField.value != null)
                                                                        {
                                                                            EditorGUIUtility.PingObject(objectField
                                                                                                            .value);
                                                                        }
                                                                    }, "", "Select object in project.");

            Texture2D openSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenSpritePath);
            VisualElement openButton = CreateButton(openSprite, () =>
                                                         {
                                                             if (objectField.value)
                                                             {
                                                                 EditorUtility.OpenPropertyEditor(objectField.value);
                                                             }
                                                         }, "", "Open object window.");
            
            buttons.Add(selectButton);
            buttons.Add(openButton);
            
            return root;
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
    }
}
