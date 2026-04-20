using System;
using System.IO;
using System.Linq;
using Fsi.Localization;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace Fsi.DataSystem.DataBuilder
{
    public static class DataBuilder<TID, TData> where TID : Enum
                                                where TData : ScriptableData<TID>
    {
        public static TData CreateData(TID id, string name, string path, string locTable)
        {
            Debug.Log("Creating data...");
            
            string folder = CreateFolder(path, id, name);

            // Create scriptable object instance for enemy data
            TData data = ScriptableObject.CreateInstance<TData>();

            // Set enemy id;
            data.ID = id;
            data.Internal = name;
            
            // Localization attempt?
            string key = id.ToString().ToLowerInvariant();
            
            FsiLocalizationUtilityEditor.EnsureStringEntryExists(locTable, $"{key}_name", $"{id}");
            FsiLocalizationUtilityEditor.EnsureStringEntryExists(locTable, $"{key}_desc", $"{id} needs a description.");
            
            data.LocName = new LocEntry(locTable, $"{key}_name");
            data.LocDesc = new LocEntry(locTable, $"{key}_desc");

            // Create and save scriptable object assets
            string dataPath = $"{folder}/{id}_Config.asset";
            AssetDatabase.CreateAsset(data, dataPath);

            // Save + refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Focus project window and ping the main asset
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;

            return data;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string CreateFolder(string path, TID id, string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = $"_{SanitizeName(name)}";
            }
            
            string folder = $"{id}{name}";
            string pathToFolder = GetFolderPath(path, id, name);
            
            AssetDatabase.CreateFolder(path, $"{folder}");
            AssetDatabase.Refresh();
            Debug.Log($"Created folder ({pathToFolder})");
            
            return pathToFolder;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string SanitizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Remove whitespace
            input = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());

            // Remove invalid file name characters
            return Path.GetInvalidFileNameChars()
                       .Aggregate(input, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetFolderPath(string path, TID id, string name)
        {
            string folder = $"{id}{name}";
            string pathToFolder = Path.Join(path, folder);
            
            int index = 0;
            while (AssetDatabase.IsValidFolder(pathToFolder)) // 500 attempts
            {
                Debug.Log($"Folder ({pathToFolder}) already exists.");
                string testFolder = $"{folder}_{index}";
                pathToFolder = Path.Join(path, testFolder);
                index++;

                if (index > 500)
                {
                    throw new InvalidOperationException($"Failed to create a unique folder for enemy '{folder}' " +
                                                        $"after 500 attempts.");
                }
            }

            return pathToFolder;
        }

        public static T CreateFromPlaceholder<T>(string placeholderPath, string copyPath) 
            where T : UnityEngine.Object
        {
            if (AssetDatabase.CopyAsset(placeholderPath, copyPath))
            {
                Debug.Log("Copied asset:\n" +
                          $"\tFrom: {placeholderPath}\n" +
                          $"\tTo: {copyPath}");
                AssetDatabase.Refresh();

                T asset = AssetDatabase.LoadAssetAtPath<T>(copyPath);
                return asset;
            }
            
            return null;
        }
    }
}