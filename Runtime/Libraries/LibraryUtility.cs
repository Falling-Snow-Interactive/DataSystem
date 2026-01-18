// Copyright Falling Snow Interactive 2025
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fsi.DataSystem.Libraries
{
    public static class LibraryUtility
    {
        private static readonly Dictionary<Type, object> LibrariesByDataType = new();
        private static readonly Dictionary<Type, object> LibrariesByLibraryType = new();

        public static void RegisterLibrary<TID, TData>(Library<TID, TData> library)
            where TData : ILibraryData<TID>
        {
            RegisterLibrary(typeof(TData), library);
        }

        public static void RegisterLibrary(Type dataType, object library)
        {
            if (dataType == null)
            {
                return;
            }

            if (library == null)
            {
                LibrariesByDataType.Remove(dataType);
                return;
            }

            LibrariesByDataType[dataType] = library;
            LibrariesByLibraryType[library.GetType()] = library;
        }

        public static bool TryGetLibrary<TID, TData>(out Library<TID, TData> library)
            where TData : ILibraryData<TID>
        {
            if (TryGetLibrary(typeof(TData), out object libraryObj) && libraryObj is Library<TID, TData> typedLibrary)
            {
                library = typedLibrary;
                return true;
            }

            library = null;
            return false;
        }

        public static bool TryGetLibrary(Type dataType, out object library)
        {
            if (dataType == null)
            {
                library = null;
                return false;
            }

            if (LibrariesByDataType.TryGetValue(dataType, out library) && library != null)
            {
                return true;
            }

            Type libraryType = GetLibraryType(dataType);
            if (libraryType == null)
            {
                #if UNITY_EDITOR
                if (TryResolveLibraryAsset(dataType, out library))
                {
                    LibrariesByDataType[dataType] = library;
                    return true;
                }
                #endif

                library = null;
                return false;
            }

            if (TryGetLibraryByType(libraryType, out library))
            {
                LibrariesByDataType[dataType] = library;
                return true;
            }

            library = null;
            return false;
        }

        private static Type GetLibraryType(Type dataType)
        {
            LibraryTypeAttribute attribute = dataType.GetCustomAttribute<LibraryTypeAttribute>(inherit: true);
            return attribute?.LibraryType;
        }

        private static bool TryGetLibraryByType(Type libraryType, out object library)
        {
            if (libraryType == null)
            {
                library = null;
                return false;
            }

            if (LibrariesByLibraryType.TryGetValue(libraryType, out library) && library != null)
            {
                return true;
            }

            foreach (KeyValuePair<Type, object> pair in LibrariesByLibraryType)
            {
                if (libraryType.IsAssignableFrom(pair.Key) && pair.Value != null)
                {
                    library = pair.Value;
                    return true;
                }
            }

            #if UNITY_EDITOR
            if (TryResolveLibraryFromAssets(libraryType, out library))
            {
                LibrariesByLibraryType[libraryType] = library;
                return true;
            }
            #endif

            library = null;
            return false;
        }

#if UNITY_EDITOR
        private static bool TryResolveLibraryFromAssets(Type libraryType, out object library)
        {
            if (typeof(ScriptableObject).IsAssignableFrom(libraryType))
            {
                Object asset = FindScriptableObjectAsset(libraryType);
                if (asset != null)
                {
                    library = asset;
                    return true;
                }
            }

            return TryFindLibraryInScriptableObjects(libraryType, out library);
        }

        private static bool TryResolveLibraryAsset(Type dataType, out object library)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LibraryAsset)}");
            LibraryAsset found = null;
            string foundPath = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LibraryAsset asset = AssetDatabase.LoadAssetAtPath<LibraryAsset>(path);
                if (!asset)
                {
                    continue;
                }

                Type assetDataType = asset.DataType;
                if (assetDataType == null)
                {
                    continue;
                }

                if (assetDataType != dataType && !dataType.IsAssignableFrom(assetDataType))
                {
                    continue;
                }

                if (found != null)
                {
                    Debug.LogWarning($"LibraryUtility | Multiple assets found for data type {dataType.Name}. Using {foundPath}.");
                    continue;
                }

                found = asset;
                foundPath = path;
            }

            if (found != null)
            {
                library = found;
                return true;
            }

            library = null;
            return false;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private static Object FindScriptableObjectAsset(Type assetType)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{assetType.Name}");
            switch (guids.Length)
            {
                case 0:
                    return null;
                case > 1:
                    Debug.LogWarning($"LibraryUtility | Multiple assets found for {assetType.Name}. Using the first match.");
                    break;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath(path, assetType);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private static bool TryFindLibraryInScriptableObjects(Type libraryType, out Object library)
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            object found = null;
            string foundPath = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (!asset)
                {
                    continue;
                }

                if (TryGetLibraryFromAsset(asset, libraryType, out object candidate))
                {
                    if (found != null)
                    {
                        Debug.LogWarning($"LibraryUtility | Multiple libraries of type {libraryType.Name} found. Using {foundPath}.");
                        continue;
                    }

                    found = candidate;
                    foundPath = path;
                }
            }

            if (found != null)
            {
                library = found;
                return true;
            }

            library = null;
            return false;
        }

        private static bool TryGetLibraryFromAsset(ScriptableObject asset, Type libraryType, out object library)
        {
            Type assetType = asset.GetType();
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (FieldInfo field in assetType.GetFields(bindingFlags))
            {
                if (libraryType.IsAssignableFrom(field.FieldType))
                {
                    library = field.GetValue(asset);
                    if (library != null)
                    {
                        return true;
                    }
                }
            }

            foreach (PropertyInfo property in assetType.GetProperties(bindingFlags))
            {
                if (!property.CanRead)
                {
                    continue;
                }

                if (libraryType.IsAssignableFrom(property.PropertyType))
                {
                    library = property.GetValue(asset);
                    if (library != null)
                    {
                        return true;
                    }
                }
            }

            library = null;
            return false;
        }
#endif
    }
}
