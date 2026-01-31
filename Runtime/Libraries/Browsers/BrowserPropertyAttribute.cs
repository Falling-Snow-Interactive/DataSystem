using System;
using JetBrains.Annotations;

namespace Fsi.DataSystem.Libraries.Browsers
{
    /// <summary>
    /// Adds browser-specific metadata for library entry fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UsedImplicitly]
    public sealed class BrowserPropertyAttribute : Attribute
    {
        /// <summary>
        /// Overrides the column display name for the field.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// When true, hides the field from the browser columns.
        /// </summary>
        public bool HideInBrowser { get; set; } = false;

        /// <summary>
        /// When true, shows a popup button column for the field.
        /// </summary>
        public bool Popup { get; set; } = false;

        /// <summary>
        /// Optional column sort order override.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public float Width { get; set; } = 140f;

        public bool Resizable { get; set; } = true;
    }
}
