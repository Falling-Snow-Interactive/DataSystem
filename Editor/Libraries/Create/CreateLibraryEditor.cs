// Copyright Falling Snow Interactive 2025

using System;
using System.IO;
using Fsi.General.Extensions;
using Fsi.DataSystem.Libraries;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries.Create
{
    public class CreateLibraryEditor : EditorWindow
    {
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
            libraryField.value = $"{n}Library";
            
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
                                         string d = dataField.value;
                                         string l = libraryField.value;
                                         
                                         if (string.IsNullOrWhiteSpace(p) 
                                             || string.IsNullOrWhiteSpace(n)
                                             || string.IsNullOrWhiteSpace(d)
                                             || string.IsNullOrWhiteSpace(l))
                                         {
                                             EditorUtility.DisplayDialog("Invalid Input", 
                                                                         "Ensure all fields have been filled.", 
                                                                         "OK");
                                             return;
                                         }

                                         string libPath = GetLibraryRootPath(p);
                                         if (string.IsNullOrWhiteSpace(libPath))
                                         {
                                             EditorUtility.DisplayDialog("Invalid Path",
                                                                         "Libraries must be created within the project's Assets folder.",
                                                                         "OK");
                                             return;
                                         }

                                         string dataTypeName = BuildDataTypeName(d, s);
                                         if (!TryResolveDataType(dataTypeName, out Type dataType))
                                         {
                                             EditorUtility.DisplayDialog("Invalid Data Type",
                                                                         $"Unable to resolve data type '{dataTypeName}'.",
                                                                         "OK");
                                             return;
                                         }

                                         CreateLibraryAsset(libPath, l, dataType);
                                         
                                         Close();
                                     };
        }
        
        private static void CreateLibraryAsset(string path, string libraryName, Type dataType)
        {
            EnsureAssetFolder(path);

            LibraryAsset asset = CreateInstance<LibraryAsset>();
            asset.SetDataType(dataType);

            string assetPath = Path.Combine(path, $"{libraryName}.asset").Replace("\\", "/");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;

            Debug.Log($"Created {libraryName}.asset at: {assetPath}");
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

        private static bool TryResolveDataType(string fullTypeName, out Type dataType)
        {
            dataType = Type.GetType(fullTypeName);
            if (dataType != null)
            {
                return true;
            }

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                dataType = assembly.GetType(fullTypeName);
                if (dataType != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildDataTypeName(string dataType, string nspace)
        {
            if (string.IsNullOrWhiteSpace(dataType))
            {
                return string.Empty;
            }

            if (dataType.Contains('.'))
            {
                return dataType;
            }

            return string.IsNullOrWhiteSpace(nspace) ? dataType : $"{nspace}.{dataType}";
        }

        private static string GetLibraryRootPath(string locationPath)
        {
            if (string.IsNullOrWhiteSpace(locationPath))
            {
                return null;
            }

            string projectAssets = Application.dataPath.Replace("\\", "/");
            string normalizedPath = locationPath.Replace("\\", "/");

            if (normalizedPath.StartsWith(projectAssets))
            {
                normalizedPath = "Assets" + normalizedPath.Substring(projectAssets.Length);
            }

            if (File.Exists(normalizedPath))
            {
                normalizedPath = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/");
            }

            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return null;
            }

            if (!normalizedPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                return null;
            }

            return Path.Combine(normalizedPath, "Libraries").Replace("\\", "/");
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(parent))
            {
                return;
            }

            EnsureAssetFolder(parent);

            string folderName = Path.GetFileName(assetPath);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
