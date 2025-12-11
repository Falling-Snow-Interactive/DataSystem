// Copyright Falling Snow Interactive 2025

using System;
using System.IO;
using Fsi.General.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries.Create
{
    public class CreateLibraryEditor : EditorWindow
    {
        private const string TemplatesFolder = "Packages/com.fallingsnowinteractive.datasystem/Templates";
        private const string TemplateExtension = "template";
        
        private const string LibraryTemplate = "Library";
        private const string AttributeTemplate = "Attribute";
        private const string DrawerTemplate = "Drawer";
        
        [SerializeField]
        private VisualTreeAsset treeAsset = null;
        
        // References
        private Label locationPath;
        private Button locationButton;
        
        private TextField nameField;
        private TextField namespaceField;
        private TextField idField;
        private TextField dataField; 
        private TextField libraryField; 

        [MenuItem("Assets/Create/Falling Snow Interactive/Create Library")]
        public static void OpenWindow()
        {
            CreateLibraryEditor wnd = GetWindow<CreateLibraryEditor>();
            wnd.titleContent = new GUIContent("Create Library");
        }
        
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            treeAsset.CloneTree(root);

            locationPath = root.Q<Label>("location_path");
            locationButton = root.Q<Button>("location_button");
            
            nameField = root.Q<TextField>("name_field");
            namespaceField = root.Q<TextField>("namespace_field");
            idField = root.Q<TextField>("id_field");
            dataField = root.Q<TextField>("data_field");
            libraryField = root.Q<TextField>("library_field");
            
            // Initial values
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string n = Selection.activeObject.name.ToSingular();
            
            locationPath.text = path;
            nameField.value = n;
            namespaceField.value = PathToNamespace(path);
            idField.value = $"{n}ID";
            dataField.value = $"{n}Data";
            libraryField.value = $"{n}Settings.Library";
            
            locationButton.clicked += () =>
                                      {
                                          string p = EditorUtility.OpenFolderPanel("Choose Location", 
                                                                                   "", "");
                                          locationPath.text = p;
                                      };

            Button confirmButton = root.Q<Button>("confirm_button");
            confirmButton.clicked += () =>
                                     {
                                         string p = locationPath.text;
                                         string n = nameField.value;
                                         string s = namespaceField.value;
                                         string i = idField.value;
                                         string d = dataField.value;
                                         string l = libraryField.value;
                                         
                                         if (string.IsNullOrWhiteSpace(p) 
                                             || string.IsNullOrWhiteSpace(n)
                                             || string.IsNullOrWhiteSpace(s)
                                             || string.IsNullOrWhiteSpace(i)
                                             || string.IsNullOrWhiteSpace(d))
                                         {
                                             EditorUtility.DisplayDialog("Invalid Input", 
                                                                         "Ensure all fields have been filled.", 
                                                                         "OK");
                                             return;
                                         }
                                         
                                         string libPath = Path.Combine(p, "Libraries");
                                         if (!Directory.Exists(libPath))
                                         {
                                             Directory.CreateDirectory(libPath);
                                         }
                                         
                                         string edPath = Path.Combine(libPath, "Editor");
                                         if (!Directory.Exists(edPath))
                                         {
                                             Directory.CreateDirectory(edPath);
                                         }

                                         CreateLibrary(libPath, n, s, i ,d, l);
                                         CreateLibraryAttribute(libPath, n, s,i,d,l);
                                         CreateLibraryAttributeDrawer(edPath, n, s,i,d,l);
                                         
                                         Close();
                                     };
        }
        
        private void CreateLibrary(string path, string name, string nspace, string id, string data, string library)
        {
            string output = FillTemplate(LibraryTemplate, name, nspace, id, data, library);

            // Ensure target directory exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Write new file
            string fileName = $"{name}Library.cs";
            string absoluteOutPath = Path.Combine(path, fileName);
            File.WriteAllText(absoluteOutPath, output);

            // If saved under Assets/, refresh so Unity imports it
            string projectAssets = Application.dataPath.Replace("\\", "/");
            if (absoluteOutPath.Replace("\\", "/").StartsWith(projectAssets))
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"Created {fileName} at: {absoluteOutPath}");
        }

        private void CreateLibraryAttribute(string path, string name, string nspace, string id, string data, string library)
        {
            string output = FillTemplate(AttributeTemplate, name, nspace, id, data, library);

            // Write Attribute file
            string filename = $"{name}LibraryAttribute.cs";
            string absolutePath = Path.Combine(path, filename);
            File.WriteAllText(absolutePath, output);

            // If saved under Assets/, refresh so Unity imports it
            string projectAssets = Application.dataPath.Replace("\\", "/");
            if (absolutePath.Replace("\\", "/").StartsWith(projectAssets))
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"Created {filename} at: {absolutePath}");
        }
        
        private void CreateLibraryAttributeDrawer(string path, string name, string nspace, string id, string data, string library)
        {
            string output = FillTemplate(DrawerTemplate, name, nspace, id, data, library);

            // Write Drawer file
            string filename = $"{name}LibraryAttributeDrawer.cs";
            string absolutePath = Path.Combine(path, filename);
            File.WriteAllText(absolutePath, output);

            // If saved under Assets/, refresh so Unity imports it
            string projectAssets = Application.dataPath.Replace("\\", "/");
            if (absolutePath.Replace("\\", "/").StartsWith(projectAssets))
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"Created {filename} at: {absolutePath}");
        }

        private static string GetTemplateFullPath(string filename)
        {
            return $"{TemplatesFolder}/{filename}.{TemplateExtension}";
        }
        
        private static string PathToNamespace(string path)
        {
            string root = EditorSettings.projectGenerationRootNamespace;
            
            if (string.IsNullOrEmpty(path))
            {
                return root;
            }

            // Get path relative to the project Assets folder
            string assetsPath = Application.dataPath.Replace("\\", "/");
            path = path.Replace("\\", "/");

            // If absolute path starts with Assets folder, trim it
            if (path.StartsWith(assetsPath))
            {
                path = "Assets" + path.Substring(assetsPath.Length);
            }
            
            // Remove file name if included
            if (!Directory.Exists(path))
            {
                path = Path.GetDirectoryName(path)?.Replace("\\", "/");
            }

            // Ensure it starts at Assets/
            if (path != null)
            {
                int index = path.IndexOf("Assets", StringComparison.Ordinal);
                if (index > -1)
                {
                    path = path[index..];
                }
            }

            // Split into folders
            if (path != null)
            {
                string[] parts = path.Split('/');

                // Build final namespace
                string ns = root;

                for (int i = 1; i < parts.Length; i++)
                {
                    string part = parts[i];
                    if (string.IsNullOrWhiteSpace(part))
                    {
                        continue;
                    }
                    
                    // Skip Scripts folder
                    if (string.Equals(part, "Scripts", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }


                    // Remove spaces, illegal chars, etc.
                    part = CleanNamespacePart(part);
                    ns += "." + part;
                }

                return ns;
            }

            return root;
        }

        private static string CleanNamespacePart(string s)
        {
            // Only letters, digits, and underscore allowed for namespace parts
            System.Text.StringBuilder sb = new();

            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
        
        private static string FillTemplate(string template, string name, string nspace, string id, string data, string library)
        {
            string path = GetTemplateFullPath(template);
            
            string assetPath = path;
            string fullPath = Path.GetFullPath(assetPath);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Template file not found at: {fullPath}");
                return "";
            }
            
            string fileText = File.ReadAllText(fullPath);
            
            // Replace placeholder
            string output = fileText.Replace("[Name]", name)
                                    .Replace("[Namespace]", nspace)
                                    .Replace("[ID]", id)
                                    .Replace("[Data]", data)
                                    .Replace("[Library]", library);

            return output;
        }
    }
}