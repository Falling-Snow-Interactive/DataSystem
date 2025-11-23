using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Fsi.DataSystem.Libraries
{
    [CustomPropertyDrawer(typeof(Library<,>), true)]
    public class LibraryPropertyDrawer : PropertyDrawer
    {
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