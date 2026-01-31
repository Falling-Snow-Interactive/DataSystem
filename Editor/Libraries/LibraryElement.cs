using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Fsi.DataSystem.Libraries
{
    /// <summary>
    /// UI Toolkit element that provides a library dropdown with select/open buttons.
    /// </summary>
    public class LibraryElement<TID, TData> : VisualElement where TData : Object, ILibraryData<TID>
    {
        private const string SelectSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Highlight_Icon.png";
        private const string OpenSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Open_Icon.png";
        private const string StyleSheetPath = "Packages/com.fallingsnowinteractive.datasystem/Editor/Libraries/LibraryElement.uss";

        private const string RootClassName = "library-element";
        private const string DropdownClassName = "library-element__dropdown";
        private const string ButtonClassName = "library-element__button";
        private const string ButtonIconClassName = "library-element__button-icon";

        /// <summary>
        /// Creates a library element using the entries from the provided library.
        /// </summary>
        public LibraryElement(string label, Library<TID, TData> library, Object selected, Action<TData> onChanged)
            : this(label, library != null ? library.Entries : new List<TData>(), selected, onChanged)
        {
        }

        /// <summary>
        /// Creates a library element with explicit entries.
        /// </summary>
        public LibraryElement(string label, List<TData> entries, Object selected, Action<TData> onChanged)
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet)
            {
                styleSheets.Add(styleSheet);
            }

            List<TData> data = entries ?? new List<TData>();
            Object selectedValue = selected;

            List<string> names = data.Select(entry => entry.ID.ToString()).ToList();
            names.Insert(0, "None");

            int selectedIndex = 0;
            if (selectedValue && selectedValue is ILibraryData<TID> current)
            {
                int found = names.IndexOf(current.ID.ToString());
                selectedIndex = found >= 0 ? found : 0;
            }

            VisualElement root = new();
            root.AddToClassList(RootClassName);
            Add(root);

            DropdownField dropdown = new(names, selectedIndex) { label = label };
            dropdown.AddToClassList(DropdownClassName);

            dropdown.RegisterValueChangedCallback(_ =>
                                                  {
                                                      // Offset by 1 because index 0 is the "None" sentinel.
                                                      int index = dropdown.index - 1;
                                                      TData newValue = index < 0 ? null : data[index];
                                                      selectedValue = newValue;
                                                      onChanged?.Invoke(newValue);
                                                  });

            root.Add(dropdown);
            
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

            root.Add(selectButton);
            root.Add(openButton);
        }

        /// <summary>
        /// Creates an icon button for the element toolbar actions.
        /// </summary>
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
