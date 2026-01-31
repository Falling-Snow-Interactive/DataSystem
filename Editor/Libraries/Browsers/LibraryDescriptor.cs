using System;

namespace Fsi.DataSystem.Libraries.Browsers
{
    public sealed class LibraryDescriptor
    {
        /// <summary>
        /// Gets the display name shown in the library popup.
        /// </summary>
        public string DisplayName { get; }
        
        /// <summary>
        /// Gets the serialized path used for initial selection lookup.
        /// </summary>
        public string PathKey { get; }
        
        /// <summary>
        /// Gets a delegate that resolves the library instance.
        /// </summary>
        public Func<object> Getter { get; }
        
        /// <summary>
        /// Gets the library type for reflection lookups.
        /// </summary>
        public Type LibraryType { get; }
            
        public LibraryDescriptor(string displayName, string pathKey, Func<object> getter, Type libraryType)
        {
            DisplayName = displayName;
            PathKey = pathKey;
            Getter = getter;
            LibraryType = libraryType;
        }
    }
}