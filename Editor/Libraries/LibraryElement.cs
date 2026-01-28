using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Spacer = Fsi.Ui.Dividers.Spacer;

namespace Fsi.DataSystem.Libraries
{
    public class LibraryElement<TID, TData> : VisualElement where TData : Object, ILibraryData<TID>
    {
        private const string SelectSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Select_Sprite.png";
        private const string OpenSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Popout_Sprite.png";

        private readonly List<TData> data;
        private readonly Action<TData> changeCallback;

        private Object selectedValue;

        public LibraryElement(Library<TID, TData> library, Object selected, Action<TData> onChanged)
            : this(library != null ? library.Entries : new List<TData>(), selected, onChanged)
        {
        }

        public LibraryElement(List<TData> entries, Object selected, Action<TData> onChanged)
        {
            data = entries ?? new List<TData>();
            selectedValue = selected;
            changeCallback = onChanged;

            List<string> names = data.Select(entry => entry.ID.ToString()).ToList();
            names.Insert(0, "None");

            int selectedIndex = 0;
            if (selectedValue != null && selectedValue is ILibraryData<TID> current)
            {
                int found = names.IndexOf(current.ID.ToString());
                selectedIndex = found >= 0 ? found : 0;
            }

            VisualElement selection = new()
                                      {
                                          style =
                                          {
                                              flexGrow = 1,
                                              flexShrink = 0,
                                              flexDirection = FlexDirection.Row,
                                              height = EditorGUIUtility.singleLineHeight,
                                          }
                                      };
            Add(selection);

            DropdownField dropdown = new(names, selectedIndex)
                                     {
                                         style =
                                         {
                                             flexGrow = 1,
                                         },
                                     };

            dropdown.RegisterValueChangedCallback(_ =>
                                                  {
                                                      int index = dropdown.index - 1;
                                                      TData newValue = index < 0 ? null : data[index];
                                                      selectedValue = newValue;
                                                      changeCallback?.Invoke(newValue);
                                                  });

            selection.Add(dropdown);
            selection.Add(new Spacer());

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
                                                                        if (selectedValue != null)
                                                                        {
                                                                            EditorGUIUtility.PingObject(selectedValue);
                                                                        }
                                                                    }, label: "", tooltip: "Select object in project.");

            Texture2D openSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenSpritePath);
            VisualElement openButton = CreateButton(openSprite, () =>
                                                         {
                                                             if (selectedValue != null)
                                                             {
                                                                 EditorUtility.OpenPropertyEditor(selectedValue);
                                                             }
                                                         }, label: "", tooltip: "Open object window.");

            buttons.Add(selectButton);
            buttons.Add(openButton);
        }

        private static VisualElement CreateButton(Texture2D icon, Action callback, string label = "", string tooltip = "")
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
                                tooltip = tooltip,
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
