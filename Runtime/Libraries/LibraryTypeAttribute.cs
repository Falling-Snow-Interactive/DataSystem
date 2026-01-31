using System;

namespace Fsi.DataSystem.Libraries
{
    /// <summary>
    /// Declares the library type associated with a data class for editor mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class LibraryTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the library type associated with the annotated class.
        /// </summary>
        public Type LibraryType { get; }

        /// <summary>
        /// Creates a new attribute pointing at a library type.
        /// </summary>
        /// <param name="libraryType">The library type to associate.</param>
        public LibraryTypeAttribute(Type libraryType)
        {
            LibraryType = libraryType;
        }
    }
}
