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
            SerializedProperty entriesProp = property.FindPropertyRelative("entries");
            PropertyField entriesField = new(entriesProp){label = property.displayName};
            return entriesField;
        }
    }
}