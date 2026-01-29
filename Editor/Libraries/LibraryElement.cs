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
        private const string SelectSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Highlight_Icon.png";
        private const string OpenSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Open_Icon.png";
        private const string StyleSheetPath = "Packages/com.fallingsnowinteractive.datasystem/Editor/Libraries/LibraryElement.uss";

        private const string SelectionClassName = "library-element__selection";
        private const string DropdownClassName = "library-element__dropdown";
        private const string ButtonsClassName = "library-element__buttons";
        private const string ButtonClassName = "library-element__button";
        private const string ButtonIconClassName = "library-element__button-icon";

        public LibraryElement(Library<TID, TData> library, Object selected, Action<TData> onChanged)
            : this(library != null ? library.Entries : new List<TData>(), selected, onChanged)
        {
        }

        public LibraryElement(List<TData> entries, Object selected, Action<TData> onChanged)
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            List<TData> data = entries ?? new List<TData>();
            Object selectedValue = selected;

            List<string> names = data.Select(entry => entry.ID.ToString()).ToList();
            names.Insert(0, "None");

            int selectedIndex = 0;
            if (selectedValue != null && selectedValue is ILibraryData<TID> current)
            {
                int found = names.IndexOf(current.ID.ToString());
                selectedIndex = found >= 0 ? found : 0;
            }

            VisualElement selection = new();
            selection.AddToClassList(SelectionClassName);
            Add(selection);

            DropdownField dropdown = new(names, selectedIndex);
            dropdown.AddToClassList(DropdownClassName);

            dropdown.RegisterValueChangedCallback(_ =>
                                                  {
                                                      int index = dropdown.index - 1;
                                                      TData newValue = index < 0 ? null : data[index];
                                                      selectedValue = newValue;
                                                      onChanged?.Invoke(newValue);
                                                  });

            selection.Add(dropdown);
            selection.Add(new Spacer());

            VisualElement buttons = new();
            buttons.AddToClassList(ButtonsClassName);

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
            Button button = new()
                            {
                                text = label,
                                tooltip = tooltip,
                            };
            button.AddToClassList(ButtonClassName);

            button.clicked += callback;

            Image image = new()
                          {
                              image = icon,
                              scaleMode = ScaleMode.ScaleToFit,
                              pickingMode = PickingMode.Ignore,
                          };
            image.AddToClassList(ButtonIconClassName);

            button.Add(image);

            return button;
        }
    }
}
