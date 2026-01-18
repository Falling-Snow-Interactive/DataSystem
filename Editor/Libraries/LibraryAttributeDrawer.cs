using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Spacer = Fsi.Ui.Dividers.Spacer;

namespace Fsi.DataSystem.Libraries
{
    [CustomPropertyDrawer(typeof(LibraryAttribute), true)]
    public class LibraryAttributeDrawer : PropertyDrawer
    {
        #region Constants
        private const string SelectSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Select_Sprite.png";
        private const string OpenSpritePath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Icon_Popout_Sprite.png";
        #endregion

        private static readonly Dictionary<ProviderMemberKey, MemberInfo> ProviderMemberCache = new();
        private static readonly Dictionary<Type, PropertyInfo> EntriesPropertyCache = new();
        private static readonly Dictionary<Type, PropertyInfo> IdPropertyCache = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (!TryGetLibraryEntries(property, out IList entries, out PropertyInfo idProperty))
            {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            List<string> names = BuildNameList(entries, idProperty);
            int selectedIndex = GetSelectedIndex(property.objectReferenceValue, idProperty, names);

            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            float buttonSize = EditorGUIUtility.singleLineHeight;
            const float spacing = 2f;

            Rect popupRect = contentRect;
            popupRect.width = Mathf.Max(0, contentRect.width - (buttonSize * 2f) - (spacing * 2f));

            Rect selectRect = contentRect;
            selectRect.x = popupRect.xMax + spacing;
            selectRect.width = buttonSize;

            Rect openRect = contentRect;
            openRect.x = selectRect.xMax + spacing;
            openRect.width = buttonSize;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, selectedIndex, names.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                int dataIndex = newIndex - 1;
                property.objectReferenceValue = GetEntryObject(entries, dataIndex);
                property.serializedObject.ApplyModifiedProperties();
            }

            Texture2D selectIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(SelectSpritePath);
            Texture2D openIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenSpritePath);

            GUIContent selectContent = new(selectIcon, "Select object in project.");
            GUIContent openContent = new(openIcon, "Open object window.");

            Object value = property.objectReferenceValue;

            using (new EditorGUI.DisabledScope(value == null))
            {
                if (GUI.Button(selectRect, selectContent))
                {
                    if (value != null)
                    {
                        EditorGUIUtility.PingObject(value);
                        Selection.activeObject = value;
                    }
                }

                if (GUI.Button(openRect, openContent))
                {
                    if (value != null)
                    {
                        EditorUtility.OpenPropertyEditor(value);
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!TryGetLibraryEntries(property, out IList entries, out PropertyInfo idProperty))
            {
                return new PropertyField(property);
            }

            List<string> names = BuildNameList(entries, idProperty);
            int selectedIndex = GetSelectedIndex(property.objectReferenceValue, idProperty, names);

            VisualElement root = new();
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
            root.Add(selection);

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
                                                      property.objectReferenceValue = GetEntryObject(entries, index);
                                                      property.serializedObject.ApplyModifiedProperties();
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
                                                                        Object value = property.objectReferenceValue;
                                                                        if (value != null)
                                                                        {
                                                                            EditorGUIUtility.PingObject(value);
                                                                            Selection.activeObject = value;
                                                                        }
                                                                    }, label: "", tooltip: "Select object in project.");

            Texture2D openSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenSpritePath);
            VisualElement openButton = CreateButton(openSprite, () =>
                                                                   {
                                                                       Object value = property.objectReferenceValue;
                                                                       if (value != null)
                                                                       {
                                                                           EditorUtility.OpenPropertyEditor(value);
                                                                       }
                                                                   }, label: "", tooltip: "Open object window.");

            buttons.Add(selectButton);
            buttons.Add(openButton);

            return root;
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

        private bool TryGetLibraryEntries(SerializedProperty property, out IList entries, out PropertyInfo idProperty)
        {
            entries = null;
            idProperty = null;

            Type dataType = GetDataType(property);
            if (dataType == null)
            {
                return false;
            }

            idProperty = GetIdProperty(dataType);
            if (idProperty == null)
            {
                return false;
            }

            if (!TryGetLibrary(dataType, out object library))
            {
                return false;
            }

            entries = GetEntries(library);
            return entries != null;
        }

        private bool TryGetLibrary(Type dataType, out object library)
        {
            library = null;

            if (!TryGetProviderMetadata(dataType, out Type providerType, out string memberName))
            {
                return false;
            }

            MemberInfo member = GetProviderMember(providerType, memberName);
            if (member == null)
            {
                return false;
            }

            library = GetMemberValue(member);
            return library != null;
        }

        private bool TryGetProviderMetadata(Type dataType, out Type providerType, out string memberName)
        {
            providerType = null;
            memberName = null;

            if (attribute is LibraryAttribute libraryAttribute)
            {
                providerType = libraryAttribute.ProviderType;
                memberName = libraryAttribute.MemberName;
            }

            if (providerType == null)
            {
                LibraryTypeAttribute libraryTypeAttribute = dataType.GetCustomAttribute<LibraryTypeAttribute>(true);
                if (libraryTypeAttribute != null)
                {
                    providerType = libraryTypeAttribute.ProviderType;
                    memberName = libraryTypeAttribute.MemberName;
                }
            }

            if (providerType == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                memberName = LibraryAttribute.DefaultMemberName;
            }

            return true;
        }

        private Type GetDataType(SerializedProperty property)
        {
            if (fieldInfo == null)
            {
                return null;
            }

            Type fieldType = fieldInfo.FieldType;

            if (IsCollectionElement(property))
            {
                if (fieldType.IsArray)
                {
                    return fieldType.GetElementType();
                }

                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return fieldType.GetGenericArguments()[0];
                }
            }

            return fieldType;
        }

        private static IList GetEntries(object library)
        {
            if (library == null)
            {
                return null;
            }

            Type libraryType = library.GetType();
            if (!EntriesPropertyCache.TryGetValue(libraryType, out PropertyInfo entriesProperty))
            {
                entriesProperty = libraryType.GetProperty("Entries", BindingFlags.Instance | BindingFlags.Public);
                EntriesPropertyCache[libraryType] = entriesProperty;
            }

            return entriesProperty?.GetValue(library) as IList;
        }

        private static PropertyInfo GetIdProperty(Type dataType)
        {
            if (IdPropertyCache.TryGetValue(dataType, out PropertyInfo idProperty))
            {
                return idProperty;
            }

            Type interfaceType = dataType.GetInterfaces()
                                         .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILibraryData<>));

            idProperty = interfaceType?.GetProperty("ID");

            if (idProperty == null)
            {
                idProperty = dataType.GetProperty("ID", BindingFlags.Instance | BindingFlags.Public);
            }

            IdPropertyCache[dataType] = idProperty;
            return idProperty;
        }

        private static MemberInfo GetProviderMember(Type providerType, string memberName)
        {
            ProviderMemberKey key = new(providerType, memberName);
            if (ProviderMemberCache.TryGetValue(key, out MemberInfo member))
            {
                return member;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            member = providerType.GetField(memberName, flags);

            if (member == null)
            {
                member = providerType.GetProperty(memberName, flags);
            }

            ProviderMemberCache[key] = member;
            return member;
        }

        private static object GetMemberValue(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.IsStatic ? field.GetValue(null) : null;
            }

            if (member is PropertyInfo property)
            {
                MethodInfo getter = property.GetMethod;
                return getter != null && getter.IsStatic ? property.GetValue(null) : null;
            }

            return null;
        }

        private static List<string> BuildNameList(IList entries, PropertyInfo idProperty)
        {
            List<string> names = new(entries?.Count + 1 ?? 1) { "None" };

            if (entries == null)
            {
                return names;
            }

            foreach (object entry in entries)
            {
                names.Add(GetEntryId(entry, idProperty));
            }

            return names;
        }

        private static string GetEntryId(object entry, PropertyInfo idProperty)
        {
            if (entry == null || idProperty == null)
            {
                return "<Missing>";
            }

            object idValue = idProperty.GetValue(entry);
            return idValue?.ToString() ?? "<Missing>";
        }

        private static int GetSelectedIndex(Object currentValue, PropertyInfo idProperty, List<string> names)
        {
            if (currentValue == null || idProperty == null)
            {
                return 0;
            }

            string currentId = idProperty.GetValue(currentValue)?.ToString();
            if (string.IsNullOrWhiteSpace(currentId))
            {
                return 0;
            }

            int found = names.IndexOf(currentId);
            return found >= 0 ? found : 0;
        }

        private static Object GetEntryObject(IList entries, int index)
        {
            if (entries == null || index < 0 || index >= entries.Count)
            {
                return null;
            }

            return entries[index] as Object;
        }

        private static bool IsCollectionElement(SerializedProperty property)
        {
            return property.propertyPath.Contains("Array.data[");
        }

        private readonly struct ProviderMemberKey : IEquatable<ProviderMemberKey>
        {
            public ProviderMemberKey(Type providerType, string memberName)
            {
                ProviderType = providerType;
                MemberName = memberName;
            }

            public Type ProviderType { get; }
            public string MemberName { get; }

            public bool Equals(ProviderMemberKey other)
            {
                return ProviderType == other.ProviderType && MemberName == other.MemberName;
            }

            public override bool Equals(object obj)
            {
                return obj is ProviderMemberKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ProviderType, MemberName);
            }
        }
    }
}
