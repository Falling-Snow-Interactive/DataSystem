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

namespace Fsi.DataSystem.Libraries.Browsers
{
    public abstract class LibraryBrowserWindow : EditorWindow
    {
        protected sealed class LibraryDescriptor
        {
            public string DisplayName { get; }
            public string PathKey { get; }
            public Func<object> Getter { get; }
            public Type LibraryType { get; }
            
            public LibraryDescriptor(string displayName, string pathKey, Func<object> getter, Type libraryType)
            {
                DisplayName = displayName;
                PathKey = pathKey;
                Getter = getter;
                LibraryType = libraryType;
            }
        }

        protected sealed class LibraryMapping
        {
            public Func<object> Getter { get; }
            public Type LibraryType { get; }
            public Type IdType { get; }
            public Type DataType { get; }
            
            public LibraryMapping(Func<object> getter, Type libraryType)
            {
                Getter = getter;
                LibraryType = libraryType;
                if (libraryType != null && libraryType.IsGenericType)
                {
                    Type[] arguments = libraryType.GetGenericArguments();
                    if (arguments.Length == 2)
                    {
                        IdType = arguments[0];
                        DataType = arguments[1];
                    }
                }
            }
        }

        private const string OpenIconPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Open_Icon.png";
        protected const string LibraryIconPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Library_Icon.png";

        private static readonly BindingFlags FieldBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly PropertyInfo IsTransientPropertyInfo = typeof(SerializedProperty).GetProperty("isTransient", BindingFlags.Instance | BindingFlags.Public);

        private readonly List<Object> entries = new();
        private readonly List<LibraryDescriptor> libraryDescriptors = new();
        private readonly List<string> libraryNames = new();

        private MultiColumnListView listView;
        private PopupField<string> libraryPopup;
        private string initialLibraryPath;
        private int selectedIndex;
        
        protected abstract IEnumerable<LibraryDescriptor> GetLibraryDescriptors();
        protected abstract Dictionary<Type, LibraryMapping> GetLibraryMappingsByAttribute();
        protected abstract Dictionary<Type, LibraryMapping> GetLibraryMappingsByDataType();

        #region UI

        public void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            BuildLibraryDescriptors();
            BuildToolbar();
            BuildListView();

            ApplyInitialSelection();
            RefreshEntries();
        }

        protected void SetInitialLibraryPath(string libraryPath)
        {
            initialLibraryPath = libraryPath;
        }

        private void BuildToolbar()
        {
            Toolbar toolbar = new();

            libraryPopup = new PopupField<string>("Library", libraryNames, selectedIndex)
                           {
                               style =
                               {
                                   flexGrow = 1,
                                   flexShrink = 0,
                                   maxWidth = new StyleLength(300f),
                               },
                           };
            
            libraryPopup.RegisterValueChangedCallback(evt =>
                                                      {
                                                          selectedIndex = libraryNames.IndexOf(evt.newValue);
                                                          RefreshEntries();
                                                      });
            toolbar.Add(libraryPopup);
            
            ToolbarSpacer s = new() { style = { flexGrow = 1, flexShrink = 0 } };
            toolbar.Add(s);
            ToolbarButton refreshButton = new(RefreshLibraries)
                                          {
                                              text = "Refresh",
                                          };
            toolbar.Add(refreshButton);

            rootVisualElement.Add(toolbar);
        }

        private void BuildListView()
        {
            listView = new MultiColumnListView
                       {
                           reorderable = false,
                           showBorder = true,
                           selectionType = SelectionType.Single,
                           sortingMode = ColumnSortingMode.Default,
                           showBoundCollectionSize = false,
                           style =
                           {
                               flexGrow = 1,
                           }
                       };

            BuildColumnsFromSerializedProperties();

            ScrollView scrollView = listView.Q<ScrollView>();
            listView.RegisterCallback<WheelEvent>(evt =>
                                                  {
                                                      if (scrollView == null)
                                                      {
                                                          return;
                                                      }

                                                      float horizontalDelta = evt.delta.x;
                                                      if (evt.shiftKey && Mathf.Abs(horizontalDelta) <= 0.01f)
                                                      {
                                                          horizontalDelta = evt.delta.y;
                                                      }

                                                      if (Mathf.Abs(horizontalDelta) > 0.01f)
                                                      {
                                                          Vector2 offset = scrollView.scrollOffset;
                                                          offset.x += horizontalDelta * 3f;
                                                          scrollView.scrollOffset = offset;
                                                          evt.StopPropagation();
                                                      }
                                                  });

            rootVisualElement.Add(listView);
        }

        #endregion

        #region Libraries

        private void RefreshLibraries()
        {
            BuildLibraryDescriptors();
            UpdateLibraryPopup();
            ApplyInitialSelection();
            RefreshEntries();
        }

        private void BuildLibraryDescriptors()
        {
            libraryDescriptors.Clear();
            libraryNames.Clear();

            foreach (LibraryDescriptor descriptor in GetLibraryDescriptors() ?? Array.Empty<LibraryDescriptor>())
            {
                if (descriptor != null)
                {
                    libraryDescriptors.Add(descriptor);
                }
            }

            libraryDescriptors.Sort((a, b) =>
                                        string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));

            foreach (var descriptor in libraryDescriptors)
            {
                libraryNames.Add(descriptor.DisplayName);
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, libraryNames.Count - 1));
        }

        private void ApplyInitialSelection()
        {
            if (string.IsNullOrWhiteSpace(initialLibraryPath) || libraryNames.Count == 0)
            {
                return;
            }

            int index = libraryDescriptors.FindIndex(descriptor =>
                                                         string.Equals(descriptor.PathKey,
                                                                       initialLibraryPath,
                                                                       StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return;
            }

            selectedIndex = index;
            UpdateLibraryPopup();
            initialLibraryPath = null;
        }

        private void UpdateLibraryPopup()
        {
            if (libraryPopup == null)
            {
                return;
            }

            libraryPopup.choices = libraryNames;
            if (libraryNames.Count == 0)
            {
                libraryPopup.SetValueWithoutNotify(string.Empty);
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, libraryNames.Count - 1);
            libraryPopup.SetValueWithoutNotify(libraryNames[selectedIndex]);
        }

        #endregion

        #region Entries

        private void RefreshEntries()
        {
            entries.Clear();

            LibraryDescriptor selected = GetSelectedDescriptor();
            if (selected == null)
            {
                listView.itemsSource = entries;
                listView.Rebuild();
                return;
            }

            object library = selected.Getter?.Invoke();
            if (library != null)
            {
                PropertyInfo entriesProperty = selected.LibraryType.GetProperty("Entries");
                if (entriesProperty?.GetValue(library) is IList entryList)
                {
                    foreach (object entry in entryList)
                    {
                        if (entry is Object unityObject)
                        {
                            entries.Add(unityObject);
                        }
                    }
                }
            }

            BuildColumnsFromSerializedProperties();
            listView.itemsSource = entries;
            listView.Rebuild();
            listView.RefreshItems();
        }

        private LibraryDescriptor GetSelectedDescriptor()
        {
            if (selectedIndex < 0 || selectedIndex >= libraryDescriptors.Count)
            {
                return null;
            }

            return libraryDescriptors[selectedIndex];
        }

        private bool TryGetEntry(int index, out Object entry)
        {
            if (index < 0 || index >= entries.Count)
            {
                entry = null;
                return false;
            }

            entry = entries[index];
            return entry != null;
        }

        private static void OpenEntry(Object entry)
        {
            if (!entry)
            {
                return;
            }

            UnityEditor.EditorUtility.OpenPropertyEditor(entry);
            EditorGUIUtility.PingObject(entry);
        }

        #endregion

        #region Columns

        private void BuildColumnsFromSerializedProperties(float defaultWidth = 140f)
        {
            listView.columns.Clear();
            listView.columns.Add(CreateOpenColumn());

            Object sampleEntry = GetFirstSerializedEntry();
            if (!sampleEntry)
            {
                return;
            }

            SerializedObject serializedObject = new(sampleEntry);
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (ShouldSkipProperty(iterator))
                {
                    continue;
                }

                Column column = CreateSerializedPropertyColumn(sampleEntry, iterator, GetLibraryMappingsByAttribute(), 
                                                               GetLibraryMappingsByDataType(), defaultWidth);
                if (column != null)
                {
                    listView.columns.Add(column);
                }
            }
        }

        private Object GetFirstSerializedEntry()
        {
            foreach (Object entry in entries)
            {
                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool ShouldSkipProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return true;
            }

            if (property.name == "m_Script")
            {
                return true;
            }

            if (IsTransientPropertyInfo != null)
            {
                bool isTransient = (bool)IsTransientPropertyInfo.GetValue(property);
                if (isTransient)
                {
                    return true;
                }
            }

            if (TryGetFieldInfoFromPath(property.serializedObject.targetObject,
                                        property.propertyPath,
                                        out FieldInfo field))
            {
                if (field.IsNotSerialized)
                {
                    return true;
                }

                if (field.GetCustomAttribute<HideInListAttribute>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private Column CreateSerializedPropertyColumn(Object sampleEntry,
                                                      SerializedProperty property,
                                                      Dictionary<Type, LibraryMapping> libraryMappingsByAttribute,
                                                      Dictionary<Type, LibraryMapping> libraryMappingsByDataType,
                                                      float width)
        {
            string propertyPath = property.propertyPath;
            string colTitle = property.displayName;
            SerializedPropertyType propertyType = property.propertyType;

            TryGetFieldType(sampleEntry, propertyPath, out Type fieldType);
            TryGetFieldInfoFromPath(sampleEntry, propertyPath, out FieldInfo fieldInfo);

            if (HasListPopupAttribute(fieldInfo)) // && IsSerializedClassProperty(property, fieldType))
            {
                return CreateSerializedClassPopupColumn(colTitle, width, propertyPath);
            }

            switch (propertyType)
            {
                case SerializedPropertyType.Enum when fieldType is { IsEnum: true }:
                    return CreateEnumPropertyColumn(colTitle, width, propertyPath, fieldType);
                case SerializedPropertyType.Integer:
                    return CreateIntegerPropertyColumn(colTitle, width, propertyPath);
                case SerializedPropertyType.Color:
                    return CreateColorPropertyColumn(colTitle, width, propertyPath);
                case SerializedPropertyType.ObjectReference:
                {
                    Type objectType = typeof(Object);
                    if (fieldType != null && typeof(Object).IsAssignableFrom(fieldType))
                    {
                        objectType = fieldType;
                    }

                    return TryGetLibraryMapping(sampleEntry, propertyPath, fieldType, libraryMappingsByAttribute, 
                                                libraryMappingsByDataType, out LibraryMapping mapping) 
                               ? CreateLibraryPropertyColumn(colTitle, width, propertyPath, mapping) 
                               : CreateObjectPropertyColumn(colTitle, width, propertyPath, objectType);
                }
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Hash128:
                case SerializedPropertyType.RenderingLayerMask:
                case SerializedPropertyType.EntityId:
                default:
                    return CreatePropertyPathColumn(colTitle, width, propertyPath);
            }
        }

        private static bool IsSerializedClassProperty(SerializedProperty property, Type fieldType)
        {
            if (property == null || fieldType == null)
            {
                return false;
            }

            if (property.isArray)
            {
                return false;
            }

            if (typeof(Object).IsAssignableFrom(fieldType))
            {
                return false;
            }

            if (!fieldType.IsClass)
            {
                return false;
            }

            if (property.propertyType != SerializedPropertyType.Generic &&
                property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return false;
            }

            return fieldType.IsSerializable || fieldType.GetCustomAttribute<SerializableAttribute>() != null;
        }

        private static bool HasListPopupAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo != null && fieldInfo.GetCustomAttribute<ListPopupAttribute>() != null;
        }

        private Column CreateSerializedClassPopupColumn(string title, float width, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       makeCell = () => new Button(),
                       bindCell = (element, index) =>
                                  {
                                      var button = (Button)element;
                                      ClearButtonCallback(button);

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          button.text = "Open";
                                          button.SetEnabled(false);
                                          return;
                                      }

                                      button.text = "Open";
                                      button.SetEnabled(true);

                                      Action callback = () =>
                                                        {
                                                            if (!data)
                                                            {
                                                                return;
                                                            }

                                                            var serializedObject = new SerializedObject(data);
                                                            serializedObject.Update();
                                                            var property = serializedObject.FindProperty(propertyPath);
                                                            if (property == null)
                                                            {
                                                                return;
                                                            }

                                                            SerializedClassPopupWindow.Show(data, propertyPath, property.displayName);
                                                        };

                                      button.clicked += callback;
                                      button.userData = callback;
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        var button = (Button)element;
                                        ClearButtonCallback(button);
                                    }
                   };
        }

        private Column CreateEnumPropertyColumn(string title, float width, string propertyPath, Type enumType)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       makeCell = () => new EnumField(),
                       bindCell = (element, index) =>
                                  {
                                      var field = (EnumField)element;
                                      ClearFieldCallback(field);

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          field.SetValueWithoutNotify((Enum)Enum.ToObject(enumType, 0));
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      var serializedObject = new SerializedObject(data);
                                      serializedObject.Update();
                                      var property = serializedObject.FindProperty(propertyPath);
                                      if (property == null)
                                      {
                                          field.SetValueWithoutNotify((Enum)Enum.ToObject(enumType, 0));
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      var enumValue = (Enum)Enum.ToObject(enumType, property.intValue);
                                      field.Init(enumValue);
                                      field.SetValueWithoutNotify(enumValue);
                                      field.SetEnabled(true);

                                      EventCallback<ChangeEvent<Enum>> callback = evt =>
                                                                                  {
                                                                                      if (!data)
                                                                                      {
                                                                                          return;
                                                                                      }

                                                                                      serializedObject.Update();
                                                                                      SerializedProperty changeProperty =
                                                                                          serializedObject.FindProperty(propertyPath);
                                                                                      if (changeProperty == null)
                                                                                      {
                                                                                          return;
                                                                                      }

                                                                                      changeProperty.intValue = Convert.ToInt32(evt.newValue);
                                                                                      serializedObject.ApplyModifiedProperties();
                                                                                      MarkDirty(data);
                                                                                  };

                                      field.RegisterValueChangedCallback(callback);
                                      field.userData = callback;
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        var field = (EnumField)element;
                                        ClearFieldCallback(field);
                                    }
                   };
        }

        private Column CreateIntegerPropertyColumn(string title, float width, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       makeCell = () => new IntegerField(),
                       bindCell = (element, index) =>
                                  {
                                      var field = (IntegerField)element;
                                      ClearFieldCallback(field);

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          field.SetValueWithoutNotify(default);
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      var serializedObject = new SerializedObject(data);
                                      serializedObject.Update();
                                      var property = serializedObject.FindProperty(propertyPath);
                                      if (property == null)
                                      {
                                          field.SetValueWithoutNotify(default);
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      field.SetValueWithoutNotify(property.intValue);
                                      field.SetEnabled(true);

                                      EventCallback<ChangeEvent<int>> callback = evt =>
                                                                                 {
                                                                                     if (!data)
                                                                                     {
                                                                                         return;
                                                                                     }

                                                                                     serializedObject.Update();
                                                                                     SerializedProperty changeProperty =
                                                                                         serializedObject.FindProperty(propertyPath);
                                                                                     if (changeProperty == null)
                                                                                     {
                                                                                         return;
                                                                                     }

                                                                                     changeProperty.intValue = evt.newValue;
                                                                                     serializedObject.ApplyModifiedProperties();
                                                                                     MarkDirty(data);
                                                                                 };

                                      field.RegisterValueChangedCallback(callback);
                                      field.userData = callback;
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        var field = (IntegerField)element;
                                        ClearFieldCallback(field);
                                    }
                   };
        }

        private Column CreateColorPropertyColumn(string title, float width, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       makeCell = () => new ColorField(),
                       bindCell = (element, index) =>
                                  {
                                      var field = (ColorField)element;
                                      ClearFieldCallback(field);

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          field.SetValueWithoutNotify(default);
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      var serializedObject = new SerializedObject(data);
                                      serializedObject.Update();
                                      var property = serializedObject.FindProperty(propertyPath);
                                      if (property == null)
                                      {
                                          field.SetValueWithoutNotify(default);
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      field.SetValueWithoutNotify(property.colorValue);
                                      field.SetEnabled(true);

                                      EventCallback<ChangeEvent<Color>> callback = evt =>
                                                                                   {
                                                                                       if (!data)
                                                                                       {
                                                                                           return;
                                                                                       }

                                                                                       serializedObject.Update();
                                                                                       SerializedProperty changeProperty =
                                                                                           serializedObject.FindProperty(propertyPath);
                                                                                       if (changeProperty == null)
                                                                                       {
                                                                                           return;
                                                                                       }

                                                                                       changeProperty.colorValue = evt.newValue;
                                                                                       serializedObject.ApplyModifiedProperties();
                                                                                       MarkDirty(data);
                                                                                   };

                                      field.RegisterValueChangedCallback(callback);
                                      field.userData = callback;
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        var field = (ColorField)element;
                                        ClearFieldCallback(field);
                                    }
                   };
        }

        private Column CreateObjectPropertyColumn(string title, float width, string propertyPath, Type objectType)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       makeCell = () => new ObjectField
                                        {
                                            objectType = objectType
                                        },
                       bindCell = (element, index) =>
                                  {
                                      var field = (ObjectField)element;
                                      ClearFieldCallback(field);

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          field.SetValueWithoutNotify(null);
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      var serializedObject = new SerializedObject(data);
                                      serializedObject.Update();
                                      var property = serializedObject.FindProperty(propertyPath);
                                      if (property == null)
                                      {
                                          field.SetValueWithoutNotify(null);
                                          field.SetEnabled(false);
                                          return;
                                      }

                                      field.SetValueWithoutNotify(property.objectReferenceValue);
                                      field.SetEnabled(true);

                                      EventCallback<ChangeEvent<Object>> callback = evt =>
                                                                                    {
                                                                                        if (!data)
                                                                                        {
                                                                                            return;
                                                                                        }

                                                                                        serializedObject.Update();
                                                                                        SerializedProperty changeProperty =
                                                                                            serializedObject.FindProperty(propertyPath);
                                                                                        if (changeProperty == null)
                                                                                        {
                                                                                            return;
                                                                                        }

                                                                                        changeProperty.objectReferenceValue = evt.newValue;
                                                                                        serializedObject.ApplyModifiedProperties();
                                                                                        MarkDirty(data);
                                                                                    };

                                      field.RegisterValueChangedCallback(callback);
                                      field.userData = callback;
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        var field = (ObjectField)element;
                                        ClearFieldCallback(field);
                                    }
                   };
        }

        private Column CreateLibraryPropertyColumn(string title, float width, string propertyPath, LibraryMapping mapping)
        {
            if (mapping?.IdType == null || mapping.DataType == null)
            {
                return CreateObjectPropertyColumn(title, width, propertyPath, typeof(Object));
            }

            MethodInfo methodInfo = GetType().GetMethod(nameof(CreateLibraryPropertyColumnInternal),
                                                        BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo genericMethod = methodInfo?.MakeGenericMethod(mapping.IdType, mapping.DataType);
            if (genericMethod == null)
            {
                return CreateObjectPropertyColumn(title, width, propertyPath, typeof(Object));
            }

            var column = genericMethod.Invoke(this, new object[] { title, width, propertyPath, mapping }) as Column;
            return column ?? CreateObjectPropertyColumn(title, width, propertyPath, typeof(Object));
        }

        private Column CreateLibraryPropertyColumnInternal<TLibraryID, TLibraryData>(string title,
                                                                                     float width,
                                                                                     string propertyPath,
                                                                                     LibraryMapping mapping)
            where TLibraryData : Object, ILibraryData<TLibraryID>
        {
            Func<Library<TLibraryID, TLibraryData>> libraryGetter =
                () => mapping?.Getter?.Invoke() as Library<TLibraryID, TLibraryData>;

            return new Column
                   {
                       title = title,
                       width = width,
                       makeCell = () => new VisualElement(),
                       bindCell = (element, index) =>
                                  {
                                      element.Clear();

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          LibraryElement<TLibraryID, TLibraryData> empty = new("",
                                                                                               libraryGetter.Invoke(),
                                                                                               null,
                                                                                               _ => { });
                                          empty.SetEnabled(false);
                                          element.Add(empty);
                                          return;
                                      }

                                      SerializedObject serializedObject = new(data);
                                      serializedObject.Update();
                                      SerializedProperty property = serializedObject.FindProperty(propertyPath);
                                      if (property == null)
                                      {
                                          LibraryElement<TLibraryID, TLibraryData> missing = new("",
                                                                                                 libraryGetter.Invoke(),
                                                                                                 null,
                                                                                                 _ => { });
                                          missing.SetEnabled(false);
                                          element.Add(missing);
                                          return;
                                      }

                                      List<TLibraryData> entries = libraryGetter.Invoke()?.Entries;
                                      TLibraryData selected = property.objectReferenceValue as TLibraryData;

                                      LibraryElement<TLibraryID, TLibraryData> libraryElement = new("",
                                                                                                    entries,
                                                                                                    selected,
                                                                                                    newValue =>
                                                                                                    {
                                                                                                        if (!data)
                                                                                                        {
                                                                                                            return;
                                                                                                        }

                                                                                                        serializedObject.Update();
                                                                                                        SerializedProperty changeProperty =
                                                                                                            serializedObject.FindProperty(propertyPath);
                                                                                                        if (changeProperty == null)
                                                                                                        {
                                                                                                            return;
                                                                                                        }

                                                                                                        changeProperty.objectReferenceValue = newValue;
                                                                                                        serializedObject.ApplyModifiedProperties();
                                                                                                        MarkDirty(data);
                                                                                                    });

                                      element.Add(libraryElement);
                                  },
                       unbindCell = (element, _) => element.Clear(),
                   };
        }

        private Column CreatePropertyPathColumn(string title, float width, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       makeCell = () => new VisualElement(),
                       bindCell = (element, index) =>
                                  {
                                      element.Clear();

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          var empty = new PropertyField
                                                      {
                                                          label = string.Empty
                                                      };
                                          empty.SetEnabled(false);
                                          element.Add(empty);
                                          return;
                                      }

                                      var serializedObject = new SerializedObject(data);
                                      serializedObject.Update();
                                      var property = serializedObject.FindProperty(propertyPath);
                                      if (property == null)
                                      {
                                          var missing = new Label($"Missing {propertyPath}");
                                          missing.SetEnabled(false);
                                          element.Add(missing);
                                          return;
                                      }

                                      var field = new PropertyField
                                                  {
                                                      label = string.Empty
                                                  };
                                      field.BindProperty(property);

                                      EventCallback<SerializedPropertyChangeEvent> callback = _ =>
                                                                                              {
                                                                                                  serializedObject.ApplyModifiedProperties();
                                                                                                  MarkDirty(data);
                                                                                              };
                                      field.RegisterCallback(callback);
                                      field.userData = callback;
                                      element.Add(field);
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        if (element.childCount > 0 && element[0] is PropertyField field)
                                        {
                                            ClearPropertyFieldCallback(field);
                                        }

                                        element.Clear();
                                    }
                   };
        }

        private Column CreateOpenColumn()
        {
            var openIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenIconPath);
            return new Column
                   {
                       title = string.Empty,
                       width = 25,
                       resizable = false,
                       makeCell = () => new Button { iconImage = openIcon },
                       bindCell = (element, index) =>
                                  {
                                      var button = (Button)element;
                                      if (!TryGetEntry(index, out var entry))
                                      {
                                          button.SetEnabled(false);
                                          button.clicked -= button.userData as Action;
                                          button.userData = null;
                                          return;
                                      }

                                      button.SetEnabled(true);
                                      if (button.userData is Action existing)
                                      {
                                          button.clicked -= existing;
                                      }

                                      Action callback = () => OpenEntry(entry);
                                      button.clicked += callback;
                                      button.userData = callback;
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        var button = (Button)element;
                                        if (button.userData is Action callback)
                                        {
                                            button.clicked -= callback;
                                            button.userData = null;
                                        }
                                    }
                   };
        }

        protected static LibraryMapping CreateLibraryMapping<TLibraryID, TLibraryData>(Func<Library<TLibraryID, TLibraryData>> getter)
            where TLibraryData : Object, ILibraryData<TLibraryID>
        {
            return new LibraryMapping(() => getter?.Invoke(), typeof(Library<TLibraryID, TLibraryData>));
        }

        private static bool TryGetLibraryMapping(Object sampleEntry,
                                                 string propertyPath,
                                                 Type fieldType,
                                                 Dictionary<Type, LibraryMapping> libraryMappingsByAttribute,
                                                 Dictionary<Type, LibraryMapping> libraryMappingsByDataType,
                                                 out LibraryMapping mapping)
        {
            mapping = null;
            FieldInfo fieldInfo = null;
            if (sampleEntry != null)
            {
                TryGetFieldInfoFromPath(sampleEntry, propertyPath, out fieldInfo);
            }

            return TryGetLibraryMapping(fieldInfo, fieldType, libraryMappingsByAttribute, libraryMappingsByDataType, out mapping);
        }

        private static bool TryGetLibraryMapping(FieldInfo fieldInfo, Type fieldType, 
                                                 Dictionary<Type, LibraryMapping> libraryMappingsByAttribute,
                                                 Dictionary<Type, LibraryMapping> libraryMappingsByDataType,
                                                 out LibraryMapping mapping)
        {
            mapping = null;

            if (fieldInfo != null)
            {
                foreach (object attribute in fieldInfo.GetCustomAttributes(true))
                {
                    if (attribute == null)
                    {
                        continue;
                    }

                    if (libraryMappingsByAttribute.TryGetValue(attribute.GetType(), out mapping))
                    {
                        return true;
                    }
                }
            }

            if (fieldType != null && ImplementsLibraryData(fieldType))
            {
                return TryGetLibraryMappingByDataType(fieldType, libraryMappingsByDataType, out mapping);
            }

            return false;
        }

        private static bool TryGetLibraryMappingByDataType(Type fieldType, Dictionary<Type, LibraryMapping> libraryMappingsByDataType, out LibraryMapping mapping)
        {
            if (libraryMappingsByDataType.TryGetValue(fieldType, out mapping))
            {
                return true;
            }

            foreach (KeyValuePair<Type, LibraryMapping> pair in libraryMappingsByDataType)
            {
                if (pair.Key.IsAssignableFrom(fieldType))
                {
                    mapping = pair.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool ImplementsLibraryData(Type type)
        {
            return type != null
                   && type.GetInterfaces().Any(interfaceType =>
                                                   interfaceType.IsGenericType
                                                   && interfaceType.GetGenericTypeDefinition() == typeof(ILibraryData<>));
        }

        private static void ClearFieldCallback<TValue>(BaseField<TValue> field)
        {
            if (field.userData is EventCallback<ChangeEvent<TValue>> callback)
            {
                field.UnregisterValueChangedCallback(callback);
            }

            field.userData = null;
        }

        private static void ClearPropertyFieldCallback(PropertyField field)
        {
            if (field.userData is EventCallback<SerializedPropertyChangeEvent> callback)
            {
                field.UnregisterCallback(callback);
            }

            field.userData = null;
        }

        private static void ClearButtonCallback(Button button)
        {
            if (button.userData is Action callback)
            {
                button.clicked -= callback;
            }

            button.userData = null;
        }

        private static void MarkDirty(Object target)
        {
            if (!target)
            {
                return;
            }

            UnityEditor.EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private static bool TryGetFieldType(Object target, string propertyPath, out Type fieldType)
        {
            fieldType = null;

            if (target == null || string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            Type currentType = target.GetType();
            string[] elements = propertyPath.Split('.');

            foreach (string element in elements)
            {
                if (element == "Array")
                {
                    continue;
                }

                if (element.StartsWith("data[", StringComparison.Ordinal))
                {
                    currentType = GetElementType(currentType);
                    continue;
                }

                FieldInfo field = GetFieldInfo(currentType, element);
                if (field == null)
                {
                    return false;
                }

                currentType = field.FieldType;
            }

            fieldType = currentType;
            return fieldType != null;
        }

        private static bool TryGetFieldInfoFromPath(Object target, string propertyPath, out FieldInfo fieldInfo)
        {
            fieldInfo = null;

            if (target == null || string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            Type currentType = target.GetType();
            string[] elements = propertyPath.Split('.');

            foreach (string element in elements)
            {
                if (element == "Array")
                {
                    continue;
                }

                if (element.StartsWith("data[", StringComparison.Ordinal))
                {
                    currentType = GetElementType(currentType);
                    continue;
                }

                FieldInfo field = GetFieldInfo(currentType, element);
                if (field == null)
                {
                    return false;
                }

                fieldInfo = field;
                currentType = field.FieldType;
            }

            return fieldInfo != null;
        }

        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, FieldBindingFlags);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static Type GetElementType(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType)
            {
                Type[] args = type.GetGenericArguments();
                if (args.Length == 1)
                {
                    return args[0];
                }
            }

            return type;
        }

        #endregion
        
        protected static void AddLibrariesFromType(List<LibraryDescriptor> descriptors,
                                                   Type type,
                                                   Func<object> parentGetter,
                                                   string parentName)
        {
            foreach (FieldInfo field in type.GetFields(FieldBindingFlags))
            {
                if (IsLibraryField(field))
                {
                    string displayName = BuildDisplayName(parentName, field.Name);
                    FieldInfo localField = field;
                    descriptors.Add(new LibraryDescriptor(displayName,
                                                          displayName,
                                                          () =>
                                                          {
                                                              var parent = parentGetter();
                                                              return parent == null ? null : localField.GetValue(parent);
                                                          },
                                                          localField.FieldType));
                    continue;
                }

                if (!IsNestedContainer(field.FieldType))
                {
                    continue;
                }

                string nestedName = BuildDisplayName(parentName, field.Name);
                FieldInfo localNestedField = field;
                AddLibrariesFromType(descriptors,
                                     field.FieldType,
                                     () =>
                                     {
                                         var parent = parentGetter();
                                         return parent == null ? null : localNestedField.GetValue(parent);
                                     },
                                     nestedName);
            }
        }
        
        private static bool IsLibraryField(FieldInfo field)
        {
            if (!field.FieldType.IsGenericType)
            {
                return false;
            }

            return field.FieldType.GetGenericTypeDefinition() == typeof(Library<,>);
        }
        
        private static string BuildDisplayName(string parent, string fieldName)
        {
            string nicified = ObjectNames.NicifyVariableName(fieldName);
            return string.IsNullOrWhiteSpace(parent) ? nicified : $"{parent}/{nicified}";
        }
        
        private static bool IsNestedContainer(Type fieldType)
        {
            if (!fieldType.IsValueType || fieldType.IsPrimitive || fieldType.IsEnum)
            {
                return false;
            }

            return fieldType.GetFields(FieldBindingFlags).Any(IsLibraryField);
        }
        
                private static void StripDecoratorDrawers(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            // Unity can inject decorator visuals for attributes like [Header]/[Space].
            // There is no public flag to disable them for UI Toolkit PropertyField, so we remove them post-bind.
            RemoveDecoratorElementsRecursive(root);
        }

        private static void RemoveDecoratorElementsRecursive(VisualElement element)
        {
            for (int i = element.childCount - 1; i >= 0; i--)
            {
                VisualElement child = element[i];

                if (IsDecoratorElement(child))
                {
                    element.RemoveAt(i);
                    continue;
                }

                RemoveDecoratorElementsRecursive(child);
            }
        }

        private static bool IsDecoratorElement(VisualElement element)
        {
            if (element == null)
            {
                return false;
            }

            // Known Unity USS classes/names used for decorator drawers (may vary by Unity version).
            if (element.ClassListContains("unity-decorator-drawers") ||
                element.ClassListContains("unity-decorator-drawer") ||
                string.Equals(element.name, "unity-decorator-drawers", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback heuristics: decorators often include "decorator" in the name/class.
            if (!string.IsNullOrEmpty(element.name) &&
                element.name.IndexOf("decorator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return element.GetClasses().Any(className => !string.IsNullOrEmpty(className) 
                                                         && className.IndexOf("decorator", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
