using System;

namespace Fsi.DataSystem.Libraries
{
    /// <summary>
    /// Hides the label for a library field in custom property drawers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HideLabelAttribute : Attribute
    {
        
    }
}
