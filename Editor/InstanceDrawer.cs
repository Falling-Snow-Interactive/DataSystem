using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Fsi.DataSystem
{
    [CustomPropertyDrawer(typeof(Instance<,>))]
    public class InstanceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new() { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };

            // Always work on a copy
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            iterator.NextVisible(true);

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                PropertyField field = new(iterator, string.Empty){style = { flexGrow = 1}};
                root.Add(field);
                iterator.NextVisible(false);
            }

            return root;
        }
    }
}