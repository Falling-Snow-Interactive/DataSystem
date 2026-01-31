using System;
using JetBrains.Annotations;

namespace Fsi.DataSystem.Libraries.Browsers
{
    /// <summary>
    /// Hides a field from the library browser list view columns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UsedImplicitly]
    public sealed class HideInBrowserAttribute : Attribute
    {
    }
}
