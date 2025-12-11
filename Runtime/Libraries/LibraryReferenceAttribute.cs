using System;
using UnityEngine;

namespace Fsi.DataSystem.Libraries
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class LibraryReferenceAttribute : PropertyAttribute
    {
    }
}