using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Fsi.DataSystem.Libraries
{
    [CustomPropertyDrawer(typeof(LibraryReferenceAttribute))]
    public class LibraryReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Root container for this property
            VisualElement root = new VisualElement();

            // Get the runtime object this property belongs to
            Object targetObject = property.serializedObject.targetObject;
            Type targetType = targetObject.GetType();

            // Try to read [LibraryType] from the class
            LibraryTypeAttribute libraryTypeAttr = targetType.GetCustomAttribute<LibraryTypeAttribute>();

            // Get the concrete TData type (field type)
            Type dataType = fieldInfo.FieldType; // This will be the closed type for TData

            // Label (optional if you just use the ObjectField label)
            // var label = new Label(property.displayName) { name = "library-reference-label" };
            // root.Add(label);

            // Create an ObjectField bound to the property
            ObjectField objectField = new()
                                      {
                                          label = property.displayName,
                                          bindingPath = property.propertyPath,
                                          objectType = dataType, // Concrete ILibraryData<TID> type
                                      };

            // Optional: add a USS class so you can style it in a .uss
            objectField.AddToClassList("fsi-library-reference");

            // If there is a [LibraryType] on the class, you can customize behavior:
            if (libraryTypeAttr != null)
            {
                Type libType = libraryTypeAttr.LibraryType;

                // Example: add a small label or tooltip describing the library
                objectField.tooltip = $"Data from library: {libType.Name}";

                // TODO: if you have a central registry or Library asset,
                // you could use libType here to build a custom popup, search, etc.
                //
                // e.g. replace ObjectField with a custom dropdown:
                // var picker = BuildLibraryPicker(property, dataType, libType);
                // root.Add(picker);
                // return root;
            }

            // Bind the ObjectField to the property
            // In UI Toolkit property drawers, binding is done automatically via bindingPath
            root.Add(objectField);

            return root;
        }
    }
}