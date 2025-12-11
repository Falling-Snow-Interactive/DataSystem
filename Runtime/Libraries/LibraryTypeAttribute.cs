using System;

namespace Fsi.DataSystem.Libraries
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class LibraryTypeAttribute : Attribute
    {
        public Type LibraryType { get; }

        public LibraryTypeAttribute(Type libraryType)
        {
            LibraryType = libraryType;
        }
    }
}