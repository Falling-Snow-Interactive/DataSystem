using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries
{
    // [CustomPropertyDrawer(typeof(Library<,>), true)]
    /// <summary>
    /// Renders the entries list for a library container.
    /// </summary>
    public class LibraryPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Builds a UI Toolkit field for the entries list.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            
            SerializedProperty entriesProp = property.FindPropertyRelative("entries");
            PropertyField entriesField = new(entriesProp){label = property.displayName};
            root.Add(entriesField);

            return root;
        }
    }
}
