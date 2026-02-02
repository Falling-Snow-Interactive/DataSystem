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
    /// <summary>
    /// Base editor window that lists library entries in a multi-column table.
    /// </summary>
    public abstract class LibraryBrowserWindow : EditorWindow
    {
        private const string OpenIconPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Open_Icon.png";
        protected const string LibraryIconPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Library_Icon.png";
        private const string AddIconPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Add_Icon.png";
        private const string RefreshIconPath = "Packages/com.fallingsnowinteractive.datasystem/Assets/Icons/Refresh_Icon.png";

        private static readonly BindingFlags FieldBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly PropertyInfo IsTransientPropertyInfo = typeof(SerializedProperty).GetProperty("isTransient", BindingFlags.Instance | BindingFlags.Public);

        private readonly List<Object> entries = new();
        private readonly List<LibraryDescriptor> libraryDescriptors = new();
        private readonly List<string> libraryNames = new();

        private MultiColumnListView listView;
        private PopupField<string> libraryPopup;
        private ToolbarButton addEntryButton;
        private ToolbarButton editScriptButton;
        private string initialLibraryPath;
        private int selectedIndex;
        
        /// <summary>
        /// Gets the library descriptors shown in the popup.
        /// </summary>
        protected abstract IEnumerable<LibraryDescriptor> GetLibraryDescriptors();
        
        /// <summary>
        /// Maps attribute types to libraries for object reference fields.
        /// </summary>
        protected abstract Dictionary<Type, LibraryMapping> GetLibraryMappingsByAttribute();
        
        /// <summary>
        /// Maps data types to libraries for object reference fields.
        /// </summary>
        protected abstract Dictionary<Type, LibraryMapping> GetLibraryMappingsByDataType();

        #region UI

        /// <summary>
        /// Builds the editor window UI.
        /// </summary>
        public void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            BuildLibraryDescriptors();
            BuildToolbar();
            BuildListView();

            ApplyInitialSelection();
            RefreshEntries();
        }

        /// <summary>
        /// Sets the initial selection using a library path key.
        /// </summary>
        protected void SetInitialLibraryPath(string libraryPath)
        {
            initialLibraryPath = libraryPath;
        }

        private void BuildToolbar()
        {
            Toolbar toolbar = new() { style = { height = 30f } };

            selectedIndex = EditorPrefs.GetInt("Library_Index", 0);

            libraryPopup = new PopupField<string>("", libraryNames, selectedIndex)
                           {
                               style =
                               {
                                   flexGrow = 1,
                                   flexShrink = 0,
                                   maxWidth = new StyleLength(225f),
                               },
                           };
            
            libraryPopup.RegisterValueChangedCallback(evt =>
                                                      {
                                                          selectedIndex = libraryNames.IndexOf(evt.newValue);
                                                          EditorPrefs.SetInt("Library_Index", selectedIndex);
                                                          RefreshEntries();
                                                          UpdateToolbarButtonStates();
                                                      });
            toolbar.Add(libraryPopup);
            
            ToolbarSpacer s = new() { style = { flexGrow = 1, flexShrink = 0, flexBasis = 1} };
            toolbar.Add(s);
            
            Texture2D refreshIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(RefreshIconPath);
            ToolbarButton refreshButton = new(RefreshLibraries)
                                          {
                                              text = "",
                                              iconImage = refreshIcon,
                                              style =
                                              {
                                                  flexGrow = 0,
                                                  flexShrink = 1,
                                     
                                                  width = 30f,
                                              },
                                          };
            toolbar.Add(refreshButton);
            
            Texture2D addIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AddIconPath);
            addEntryButton = new ToolbarButton(CreateNewEntry)
                             {
                                 text = "",
                                 iconImage = addIcon,
                                 style =
                                 {
                                     flexGrow = 0,
                                     flexShrink = 1,
                                     
                                     width = 30f,
                                 },
                             };
            toolbar.Add(addEntryButton);
            
            UpdateToolbarButtonStates();

            rootVisualElement.Add(toolbar);
        }

        private void BuildListView()
        {
            listView = new MultiColumnListView
                       {
                           reorderable = true,
                           showBorder = true,
                           selectionType = SelectionType.Single,
                           sortingMode = ColumnSortingMode.Default,
                           showBoundCollectionSize = false,
                           style =
                           {
                               flexGrow = 1,
                           }
                       };
            listView.itemIndexChanged += OnListItemIndexChanged;

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
                                                          // Allow shift+wheel to scroll horizontally like a spreadsheet.
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

        /// <summary>
        /// Reloads library descriptors and refreshes the entries list.
        /// </summary>
        private void RefreshLibraries()
        {
            BuildLibraryDescriptors();
            UpdateLibraryPopup();
            ApplyInitialSelection();
            RefreshEntries();
            UpdateToolbarButtonStates();
        }

        /// <summary>
        /// Populates descriptors and display names from the provided sources.
        /// </summary>
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

        /// <summary>
        /// Applies the initial library selection if a path is provided.
        /// </summary>
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

        /// <summary>
        /// Refreshes the library popup choices and selected value.
        /// </summary>
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

        /// <summary>
        /// Rebuilds the entries list for the selected library.
        /// </summary>
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
            UpdateToolbarButtonStates();
        }

        private void OnListItemIndexChanged(int from, int to)
        {
            LibraryDescriptor descriptor = GetSelectedDescriptor();
            if (descriptor == null)
            {
                return;
            }

            object library = descriptor.Getter?.Invoke();
            if (library == null)
            {
                return;
            }

            PropertyInfo entriesProperty = descriptor.LibraryType.GetProperty("Entries");
            if (entriesProperty?.GetValue(library) is not IList entryList)
            {
                return;
            }

            entryList.Clear();
            foreach (Object entry in entries)
            {
                entryList.Add(entry);
            }

            Object owner = descriptor.OwnerGetter?.Invoke();
            if (owner != null)
            {
                MarkDirty(owner);
            }
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

            EditorUtility.OpenPropertyEditor(entry);
            EditorGUIUtility.PingObject(entry);
        }

        private static MonoScript GetScriptForLibrary(object library, out string warning)
        {
            warning = null;

            switch (library)
            {
                case null:
                    return null;
                case ScriptableObject scriptableObject:
                {
                    MonoScript script = MonoScript.FromScriptableObject(scriptableObject);
                    if (script == null)
                    {
                        warning = $"Unable to resolve script for library {scriptableObject.GetType().Name}.";
                    }

                    return script;
                }
                case MonoBehaviour monoBehaviour:
                {
                    MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
                    if (script == null)
                    {
                        warning = $"Unable to resolve script for library {monoBehaviour.GetType().Name}.";
                    }

                    return script;
                }
                default:
                    warning = $"Unable to resolve script for library type {library.GetType().Name}.";
                    return null;
            }
        }

        private void UpdateToolbarButtonStates()
        {
            if (editScriptButton == null)
            {
                return;
            }

            LibraryDescriptor descriptor = GetSelectedDescriptor();
            if (descriptor == null)
            {
                if (addEntryButton != null)
                {
                    addEntryButton.SetEnabled(false);
                }

                editScriptButton.SetEnabled(false);
                return;
            }

            object library = descriptor.Getter?.Invoke();
            MonoScript script = GetScriptForLibrary(library, out _);
            editScriptButton.SetEnabled(script != null);

            if (addEntryButton != null)
            {
                Type entryType = GetLibraryEntryType(descriptor.LibraryType);
                bool canCreateEntry = entryType != null && typeof(ScriptableObject).IsAssignableFrom(entryType);
                addEntryButton.SetEnabled(canCreateEntry);
            }
        }

        #endregion

        #region Columns

        /// <summary>
        /// Builds columns using serialized properties from the first entry.
        /// </summary>
        private void BuildColumnsFromSerializedProperties()
        {
            listView.columns.Clear();
            listView.columns.Add(CreateOpenColumn());
            listView.columns.Add(CreateFileColumn());

            Object sampleEntry = GetFirstSerializedEntry();
            if (!sampleEntry)
            {
                return;
            }

            SerializedObject serializedObject = new(sampleEntry);
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            Dictionary<string, List<GroupedPropertyData>> groupedProperties = new(StringComparer.Ordinal);

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                TryGetFieldInfoFromPath(sampleEntry, iterator.propertyPath, out FieldInfo fieldInfo);
                BrowserPropertyAttribute browserPropertyAttribute = fieldInfo?.GetCustomAttribute<BrowserPropertyAttribute>();
                if (!string.IsNullOrWhiteSpace(browserPropertyAttribute?.Group))
                {
                    AddGroupedProperty(groupedProperties, browserPropertyAttribute.Group, iterator.Copy(), fieldInfo, browserPropertyAttribute);
                    continue;
                }

                if (ShouldSkipProperty(iterator))
                {
                    continue;
                }

                Column column = CreateSerializedPropertyColumn(sampleEntry, iterator, GetLibraryMappingsByAttribute(),
                                                               GetLibraryMappingsByDataType());
                if (column != null)
                {
                    listView.columns.Add(column);
                }
            }

            if (groupedProperties.Count == 0)
            {
                return;
            }

            List<GroupedColumnData> groupedColumns = new();
            foreach ((string groupName, List<GroupedPropertyData> properties) in groupedProperties)
            {
                groupedColumns.Add(CreateGroupedColumnData(groupName, properties));
            }

            foreach (GroupedColumnData groupedColumn in groupedColumns
                                                        .OrderBy(data => data.SortOrder)
                                                        .ThenBy(data => data.GroupName, StringComparer.Ordinal))
            {
                listView.columns.Add(groupedColumn.Column);
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

        /// <summary>
        /// Determines whether a serialized property should be hidden from the list view.
        /// </summary>
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

                BrowserPropertyAttribute browserPropertyAttribute = field.GetCustomAttribute<BrowserPropertyAttribute>();
                if (browserPropertyAttribute?.HideInBrowser == true)
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(browserPropertyAttribute?.Group))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a column appropriate for the serialized property type.
        /// </summary>
        private Column CreateSerializedPropertyColumn(Object sampleEntry,
                                                      SerializedProperty property,
                                                      Dictionary<Type, LibraryMapping> libraryMappingsByAttribute,
                                                      Dictionary<Type, LibraryMapping> libraryMappingsByDataType)
        {
            string propertyPath = property.propertyPath;
            string colTitle = property.displayName;
            SerializedPropertyType propertyType = property.propertyType;

            TryGetFieldType(sampleEntry, propertyPath, out Type fieldType);
            TryGetFieldInfoFromPath(sampleEntry, propertyPath, out FieldInfo fieldInfo);
            BrowserPropertyAttribute browserPropertyAttribute = fieldInfo?.GetCustomAttribute<BrowserPropertyAttribute>();

            if (!string.IsNullOrWhiteSpace(browserPropertyAttribute?.DisplayName))
            {
                colTitle = browserPropertyAttribute.DisplayName;
            }

            float width = 140f;
            bool resizable = true;
            if (browserPropertyAttribute != null)
            {
                width = browserPropertyAttribute.Width;
                resizable = browserPropertyAttribute.Resizable;
            }

            if (HasListPopupAttribute(fieldInfo)) // && IsSerializedClassProperty(property, fieldType))
            {
                return CreateSerializedClassPopupColumn(colTitle, width, resizable, propertyPath);
            }

            switch (propertyType)
            {
                case SerializedPropertyType.Enum when fieldType is { IsEnum: true }:
                    return CreateEnumPropertyColumn(colTitle, width, resizable, propertyPath, fieldType);
                case SerializedPropertyType.Integer:
                    return CreateIntegerPropertyColumn(colTitle, width, resizable, propertyPath);
                case SerializedPropertyType.Color:
                    return CreateColorPropertyColumn(colTitle, width, resizable, propertyPath);
                case SerializedPropertyType.ObjectReference:
                {
                    Type objectType = typeof(Object);
                    if (fieldType != null && typeof(Object).IsAssignableFrom(fieldType))
                    {
                        objectType = fieldType;
                    }

                    return TryGetLibraryMapping(sampleEntry, propertyPath, fieldType, libraryMappingsByAttribute, 
                                                libraryMappingsByDataType, out LibraryMapping mapping) 
                               ? CreateLibraryPropertyColumn(colTitle, width, resizable, propertyPath, mapping) 
                               : CreateObjectPropertyColumn(colTitle, width, resizable, propertyPath, objectType);
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
                    return CreatePropertyPathColumn(colTitle, width, resizable, propertyPath);
            }
        }

        private static void AddGroupedProperty(Dictionary<string, List<GroupedPropertyData>> groupedProperties,
                                               string groupName,
                                               SerializedProperty property,
                                               FieldInfo fieldInfo,
                                               BrowserPropertyAttribute attribute)
        {
            if (!groupedProperties.TryGetValue(groupName, out List<GroupedPropertyData> properties))
            {
                properties = new List<GroupedPropertyData>();
                groupedProperties[groupName] = properties;
            }

            properties.Add(new GroupedPropertyData(property, fieldInfo, attribute));
        }

        private GroupedColumnData CreateGroupedColumnData(string groupName, List<GroupedPropertyData> properties)
        {
            int sortOrder = int.MaxValue;
            float? width = null;
            bool? resizable = null;
            const float defaultWidth = 140f;

            foreach (GroupedPropertyData property in properties)
            {
                if (property.Attribute == null)
                {
                    continue;
                }

                sortOrder = Math.Min(sortOrder, property.Attribute.SortOrder);

                if (width == null)
                {
                    width = property.Attribute.Width;
                }
                else if (!Mathf.Approximately(width.Value, property.Attribute.Width))
                {
                    width = defaultWidth;
                }

                if (resizable == null)
                {
                    resizable = property.Attribute.Resizable;
                }
                else if (resizable.Value != property.Attribute.Resizable)
                {
                    resizable = true;
                }
            }

            Column column = CreateGroupedPopupColumn(groupName,
                                                     width ?? defaultWidth,
                                                     resizable ?? true,
                                                     properties);
            return new GroupedColumnData(groupName, sortOrder == int.MaxValue ? 0 : sortOrder, column);
        }

        private Column CreateGroupedPopupColumn(string groupName,
                                                float width,
                                                bool resizable,
                                                IReadOnlyList<GroupedPropertyData> properties)
        {
            List<GroupedPropertyDefinition> definitions = properties
                                                          .Select(property => new GroupedPropertyDefinition(property.Property.propertyPath, property.DisplayName))
                                                          .ToList();

            return new Column
                   {
                       title = groupName,
                       width = width,
                       resizable = resizable,
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

                                      Action callback = () => GroupedPropertyPopupWindow.Show(data, groupName, definitions);
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

        /// <summary>
        /// Checks for the list popup attribute on a field.
        /// </summary>
        private static bool HasListPopupAttribute(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                return false;
            }

            BrowserPropertyAttribute browserPropertyAttribute = fieldInfo.GetCustomAttribute<BrowserPropertyAttribute>();
            return browserPropertyAttribute?.Popup == true;
        }

        /// <summary>
        /// Creates a column that opens a serialized class in a popup window.
        /// </summary>
        private Column CreateSerializedClassPopupColumn(string title, float width, bool resizable, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       resizable = resizable,
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

        /// <summary>
        /// Creates a column for enum fields with inline editing.
        /// </summary>
        private Column CreateEnumPropertyColumn(string title, float width, bool resizable, string propertyPath, Type enumType)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       resizable = resizable,
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

        /// <summary>
        /// Creates a column for integer fields with inline editing.
        /// </summary>
        private Column CreateIntegerPropertyColumn(string title, float width, bool resizable, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       resizable = resizable,
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

        /// <summary>
        /// Creates a column for color fields with inline editing.
        /// </summary>
        private Column CreateColorPropertyColumn(string title, float width, bool resizable, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       resizable = resizable,
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

        /// <summary>
        /// Creates a column for generic object reference fields.
        /// </summary>
        private Column CreateObjectPropertyColumn(string title, float width, bool resizable, string propertyPath, Type objectType)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       resizable = resizable,
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

        /// <summary>
        /// Creates a column for library-backed object references.
        /// </summary>
        private Column CreateLibraryPropertyColumn(string title, float width, bool resizable, string propertyPath, LibraryMapping mapping)
        {
            if (mapping?.IdType == null || mapping.DataType == null)
            {
                return CreateObjectPropertyColumn(title, width, resizable, propertyPath, typeof(Object));
            }

            MethodInfo methodInfo = GetType().GetMethod(nameof(CreateLibraryPropertyColumnInternal),
                                                        BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo genericMethod = methodInfo?.MakeGenericMethod(mapping.IdType, mapping.DataType);
            if (genericMethod == null)
            {
                return CreateObjectPropertyColumn(title, width, resizable, propertyPath, typeof(Object));
            }

            Column column = genericMethod.Invoke(this, new object[] { title, width, propertyPath, mapping }) as Column;
            return column ?? CreateObjectPropertyColumn(title, width, resizable, propertyPath, typeof(Object));
        }

        /// <summary>
        /// Creates a library selector column for a specific library type.
        /// </summary>
        private Column CreateLibraryPropertyColumnInternal<TLibraryID, TLibraryData>(string title,
                                                                                     float width,
                                                                                     string propertyPath,
                                                                                     LibraryMapping mapping)
            where TLibraryData : Object, IData<TLibraryID>
        {
            Func<Library<TLibraryID, TLibraryData>> libraryGetter = () => mapping?.Getter?.Invoke() as Library<TLibraryID, TLibraryData>;

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

                                      List<TLibraryData> datas = libraryGetter.Invoke()?.Entries;
                                      TLibraryData selected = property.objectReferenceValue as TLibraryData;

                                      LibraryElement<TLibraryID, TLibraryData> libraryElement = new("",
                                                                                                    datas,
                                                                                                    selected,
                                                                                                    newValue =>
                                                                                                    {
                                                                                                        if (!data)
                                                                                                        {
                                                                                                            return;
                                                                                                        }

                                                                                                        // Keep serialized state in sync when changing the dropdown.
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

        /// <summary>
        /// Creates a column that binds to a generic property field.
        /// </summary>
        private Column CreatePropertyPathColumn(string title, float width, bool resizable, string propertyPath)
        {
            return new Column
                   {
                       title = title,
                       width = width,
                       resizable = resizable,
                       makeCell = () => new VisualElement(),
                       bindCell = (element, index) =>
                                  {
                                      element.Clear();

                                      if (!TryGetEntry(index, out Object data))
                                      {
                                          PropertyField empty = new()
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

                                      PropertyField field = new()
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

        /// <summary>
        /// Creates the leading column that opens an entry asset.
        /// </summary>
        private Column CreateOpenColumn()
        {
            Texture2D openIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(OpenIconPath);
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
        
        /// <summary>
        /// Creates the leading column that opens an entry asset.
        /// </summary>
        private Column CreateFileColumn()
        {
            return new Column
                   {
                       title = "Filename",
                       width = 150f,
                       resizable = true,
                       makeCell = () => new Label(""),
                       bindCell = (element, index) =>
                                  {
                                      Label label = (Label)element;
                                      if (!TryGetEntry(index, out Object entry))
                                      {
                                          label.SetEnabled(false);
                                          label.text = "";
                                          return;
                                      }

                                      label.SetEnabled(true);
                                      label.text = $"{entry.name}";
                                  },
                       unbindCell = (element, _) =>
                                    {
                                        Label label = (Label)element;
                                        label.text = "";
                                    }
                   };
        }

        /// <summary>
        /// Creates a mapping wrapper for a library getter.
        /// </summary>
        protected static LibraryMapping CreateLibraryMapping<TLibraryID, TLibraryData>(Func<Library<TLibraryID, TLibraryData>> getter)
            where TLibraryData : Object, IData<TLibraryID>
        {
            return new LibraryMapping(() => getter?.Invoke(), typeof(Library<TLibraryID, TLibraryData>));
        }

        /// <summary>
        /// Attempts to resolve a library mapping for a serialized field.
        /// </summary>
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

        /// <summary>
        /// Attempts to resolve a library mapping based on attribute or data type.
        /// </summary>
        private static bool TryGetLibraryMapping(FieldInfo fieldInfo, Type fieldType, 
                                                 Dictionary<Type, LibraryMapping> libraryMappingsByAttribute,
                                                 Dictionary<Type, LibraryMapping> libraryMappingsByDataType,
                                                 out LibraryMapping mapping)
        {
            mapping = null;

            if (fieldInfo != null)
            {
                // Attribute-based mappings take priority over data-type mappings.
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

        /// <summary>
        /// Attempts to find a mapping that matches the provided data type.
        /// </summary>
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

        /// <summary>
        /// Checks if a type implements <see cref="IData{T}"/>.
        /// </summary>
        private static bool ImplementsLibraryData(Type type)
        {
            return type != null
                   && type.GetInterfaces().Any(interfaceType =>
                                                   interfaceType.IsGenericType
                                                   && interfaceType.GetGenericTypeDefinition() == typeof(IData<>));
        }

        /// <summary>
        /// Removes a UI Toolkit value-changed callback stored in userData.
        /// </summary>
        private static void ClearFieldCallback<TValue>(BaseField<TValue> field)
        {
            if (field.userData is EventCallback<ChangeEvent<TValue>> callback)
            {
                field.UnregisterValueChangedCallback(callback);
            }

            field.userData = null;
        }

        /// <summary>
        /// Removes a serialized property change callback from a PropertyField.
        /// </summary>
        private static void ClearPropertyFieldCallback(PropertyField field)
        {
            if (field.userData is EventCallback<SerializedPropertyChangeEvent> callback)
            {
                field.UnregisterCallback(callback);
            }

            field.userData = null;
        }

        /// <summary>
        /// Removes a stored click handler from a button.
        /// </summary>
        private static void ClearButtonCallback(Button button)
        {
            if (button.userData is Action callback)
            {
                button.clicked -= callback;
            }

            button.userData = null;
        }

        /// <summary>
        /// Marks the target asset dirty and saves.
        /// </summary>
        private static void MarkDirty(Object target)
        {
            if (!target)
            {
                return;
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private void CreateNewEntry()
        {
            LibraryDescriptor descriptor = GetSelectedDescriptor();
            if (descriptor == null)
            {
                return;
            }

            Type entryType = GetLibraryEntryType(descriptor.LibraryType);
            if (entryType == null || !typeof(ScriptableObject).IsAssignableFrom(entryType))
            {
                EditorUtility.DisplayDialog("Unsupported Entry Type",
                                            "This library does not support creating ScriptableObject entries.",
                                            "OK");
                return;
            }

            string defaultName = $"New {ObjectNames.NicifyVariableName(entryType.Name)}";
            string lastPath = EditorPrefs.GetString($"{entryType}_entry_path", "Assets");
            string path = EditorUtility.SaveFilePanelInProject("Create Library Entry",
                                                               defaultName,
                                                               "asset",
                                                               "Choose a save location for the new library entry.",
                                                               lastPath);
            
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            EditorPrefs.SetString($"{entryType}_entry_path", path);

            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                EditorUtility.DisplayDialog("Asset Already Exists",
                                            $"An asset already exists at {path}.",
                                            "OK");
                return;
            }

            ScriptableObject newEntry = ScriptableObject.CreateInstance(entryType);
            AssetDatabase.CreateAsset(newEntry, path);

            if (!TryAddEntryToLibrary(descriptor, newEntry))
            {
                AssetDatabase.DeleteAsset(path);
                EditorUtility.DisplayDialog("Unable to Add Entry",
                                            "The library entries list could not be updated.",
                                            "OK");
                return;
            }

            MarkDirty(newEntry);
            Object owner = descriptor.OwnerGetter?.Invoke();
            if (owner != null)
            {
                MarkDirty(owner);
            }

            RefreshEntries();
            SelectEntry(newEntry);
            EditorGUIUtility.PingObject(newEntry);
        }

        private void SelectEntry(Object entry)
        {
            if (entry == null || listView == null)
            {
                return;
            }

            int index = entries.IndexOf(entry);
            if (index < 0)
            {
                return;
            }

            listView.SetSelection(index);
        }

        private bool TryAddEntryToLibrary(LibraryDescriptor descriptor, Object entry)
        {
            if (descriptor == null || entry == null)
            {
                return false;
            }

            object library = descriptor.Getter?.Invoke();
            if (library == null)
            {
                return false;
            }

            PropertyInfo entriesProperty = descriptor.LibraryType?.GetProperty("Entries");
            if (entriesProperty?.GetValue(library) is not IList entryList)
            {
                return false;
            }

            entryList.Add(entry);
            return true;
        }

        private static Type GetLibraryEntryType(Type libraryType)
        {
            if (libraryType == null)
            {
                return null;
            }

            Type current = libraryType;
            while (current != null)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Library<,>))
                {
                    Type[] args = current.GetGenericArguments();
                    return args.Length > 1 ? args[1] : null;
                }

                current = current.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Attempts to resolve the field type for a serialized property path.
        /// </summary>
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

        /// <summary>
        /// Attempts to resolve the field info for a serialized property path.
        /// </summary>
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

        /// <summary>
        /// Walks the type hierarchy to resolve a field.
        /// </summary>
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

        /// <summary>
        /// Resolves the element type for array or single-generic collections.
        /// </summary>
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
        
        /// <summary>
        /// Recursively collects library fields from a container type.
        /// </summary>
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
                                                          localField.FieldType,
                                                          () => parentGetter() as Object));
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
        
        /// <summary>
        /// Checks whether a field is a library container type.
        /// </summary>
        private static bool IsLibraryField(FieldInfo field)
        {
            if (!field.FieldType.IsGenericType)
            {
                return false;
            }

            return field.FieldType.GetGenericTypeDefinition() == typeof(Library<,>);
        }
        
        /// <summary>
        /// Builds a hierarchical display name for nested libraries.
        /// </summary>
        private static string BuildDisplayName(string parent, string fieldName)
        {
            string nicified = ObjectNames.NicifyVariableName(fieldName);
            return string.IsNullOrWhiteSpace(parent) ? nicified : $"{parent}/{nicified}";
        }
        
        /// <summary>
        /// Determines if a nested container type should be traversed for libraries.
        /// </summary>
        private static bool IsNestedContainer(Type fieldType)
        {
            if (!fieldType.IsValueType || fieldType.IsPrimitive || fieldType.IsEnum)
            {
                return false;
            }

            return fieldType.GetFields(FieldBindingFlags).Any(IsLibraryField);
        }

        private sealed class GroupedPropertyData
        {
            public GroupedPropertyData(SerializedProperty property, FieldInfo fieldInfo, BrowserPropertyAttribute attribute)
            {
                Property = property;
                FieldInfo = fieldInfo;
                Attribute = attribute;
                DisplayName = ResolveDisplayName(property, attribute);
            }

            public SerializedProperty Property { get; }
            public FieldInfo FieldInfo { get; }
            public BrowserPropertyAttribute Attribute { get; }
            public string DisplayName { get; }
        }

        private readonly struct GroupedColumnData
        {
            public GroupedColumnData(string groupName, int sortOrder, Column column)
            {
                GroupName = groupName;
                SortOrder = sortOrder;
                Column = column;
            }

            public string GroupName { get; }
            public int SortOrder { get; }
            public Column Column { get; }
        }

        private static string ResolveDisplayName(SerializedProperty property, BrowserPropertyAttribute attribute)
        {
            if (!string.IsNullOrWhiteSpace(attribute?.DisplayName))
            {
                return attribute.DisplayName;
            }

            return property?.displayName ?? string.Empty;
        }
    }
}
