using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Fsi.DataSystem
{
    /// <summary>
    /// Draws inline fields for <see cref="Instance{TID,TData}"/> wrappers in the inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(Instance<,>))]
    public class InstanceDrawer : PropertyDrawer
    {
        /// <summary>
        /// Builds a UI Toolkit field list for the instance wrapper.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new() { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };

            // Always work on a copy
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            iterator.NextVisible(true);

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                // Each field gets its own PropertyField and is appended into a horizontal row.
                PropertyField field = new(iterator, string.Empty){style = { flexGrow = 1}};
                root.Add(field);
                iterator.NextVisible(false);
            }

            return root;
        }
    }
}
