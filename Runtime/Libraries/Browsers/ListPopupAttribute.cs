using System;
using JetBrains.Annotations;

namespace Fsi.DataSystem.Libraries.Browsers
{
    [AttributeUsage(AttributeTargets.Field)]
    [UsedImplicitly]
    public sealed class ListPopupAttribute : Attribute
    {
    }
}