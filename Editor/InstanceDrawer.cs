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
        private const string USS = "Packages/com.fallingsnowinteractive.datasystem/Editor/InstanceDrawer.uss";
        
        /// <summary>
        /// Builds a UI Toolkit field list for the instance wrapper.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            root.AddToClassList("fsi-instance-drawer");
            StyleSheet stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS);
            if (stylesheet != null)
            {
                root.styleSheets.Add(stylesheet);
            }

            // Always work on a copy
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            iterator.NextVisible(true);

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                // Each field gets its own PropertyField and is appended into a horizontal row.
                PropertyField field = new(iterator, string.Empty);
                field.AddToClassList("fsi-instance-drawer__field");
                root.Add(field);
                iterator.NextVisible(false);
            }

            return root;
        }
    }
}
