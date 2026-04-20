using System;
using Fsi.Ui.Dividers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.DataBuilder
{
    public abstract class DataBuilderWindow<TID, TData> : EditorWindow where TID : Enum
                                                                       where TData : ScriptableData<TID>
    {
        protected abstract string DefaultPath { get; }
        protected abstract string DefaultLocTable { get; }
        protected abstract string DefaultName { get; }

        // Fields
        private TextField pathField;
        private EnumField idField;
        private TextField nameField;
        private TextField locTable;

        private Button browseButton;

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement fields = CreateFields();
            root.Add(fields);
            
            root.Add(new Divider());

            VisualElement buttonGroup = CreateButtonGroup();
            root.Add(buttonGroup);

        }

        protected virtual void CreateData(TID id, string name, string path, string locTable)
        {
            DataBuilder<TID,TData>.CreateData(id, name, path, locTable);
            EditorPrefs.SetInt($"{DefaultName}_ID_Prev", Convert.ToInt32(id) + 1);
            EditorPrefs.SetString($"{DefaultName}_Name_Prev", "");

            Close();
        }

        protected virtual VisualElement CreateFields()
        {
            VisualElement root = new();
            
            int idIndex = EditorPrefs.GetInt($"C{DefaultName}_ID_Prev", 0);
            TID id = (TID)Enum.ToObject(typeof(TID), idIndex);
            
            string path = EditorPrefs.GetString($"{DefaultName}_Name_Prev", DefaultPath);
            if (string.IsNullOrEmpty(path))
            {
                path = DefaultPath;
            }
            VisualElement pathGroup = new() { style = { flexDirection = FlexDirection.Row, }, };
            root.Add(pathGroup);

            pathField = new TextField("Path") { value = path, style = { flexGrow = 1 } }; 
            browseButton = new Button { text = "Browse", };
            browseButton.clicked += OnBrowseClicked;
            pathGroup.Add(pathField);
            pathGroup.Add(browseButton);
            
            idField = new EnumField("ID", id);
            root.Add(idField);
            
            nameField = new TextField("Name") { value = DefaultName, };
            root.Add(nameField);
            
            locTable = new TextField("Loc Table") { value = DefaultLocTable };
            root.Add(locTable);
            
            // Value changes
            idField.RegisterValueChangedCallback(evt => EditorPrefs.SetInt($"{DefaultName}_ID_Prev", Convert.ToInt32((TID)evt.newValue)));
            nameField.RegisterValueChangedCallback(evt => EditorPrefs.SetString($"{DefaultName}_Name_Prev", evt.newValue));

            return root;
        }

        private VisualElement CreateButtonGroup()
        {
            VisualElement root = new() { style = { flexDirection = FlexDirection.Row } };
            Button createButton = new()
                                  {
                                      text = "Create", // root.Q<Button>("create_button");
                                  };
            createButton.clicked += OnCreateClicked;
            root.Add(createButton);

            Button cancelButton = new()
                                  {
                                      text = "Cancel", //root.Q<Button>("cancel_button");
                                  };
            cancelButton.clicked += Close;
            root.Add(cancelButton);

            return root;
        }
        
        private void OnCreateClicked()
        {
            CreateData((TID)idField.value, nameField.value, pathField.value, locTable.value);
        }

        private void OnBrowseClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", // Window title
                                                                "Assets/Config/Enemies", // Default path
                                                                ""); // Default folder name

            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Optional: convert absolute path → relative Unity path
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }

                pathField.value = selectedPath;
            }
        }
    }
}