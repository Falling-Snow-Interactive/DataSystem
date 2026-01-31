using System;
using JetBrains.Annotations;

namespace Fsi.DataSystem.Libraries.Browsers
{
    /// <summary>
    /// Marks a serialized class field to be shown in a popup inspector window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UsedImplicitly]
    public sealed class BrowserPopupAttribute : Attribute
    {
    }
}
