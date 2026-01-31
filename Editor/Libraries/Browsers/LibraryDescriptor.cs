using System;
using Object = UnityEngine.Object;

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

        /// <summary>
        /// Gets a delegate that resolves the object that owns the library.
        /// </summary>
        public Func<Object> OwnerGetter { get; }
            
        public LibraryDescriptor(string displayName, string pathKey, Func<object> getter, Type libraryType, Func<Object> ownerGetter = null)
        {
            DisplayName = displayName;
            PathKey = pathKey;
            Getter = getter;
            LibraryType = libraryType;
            OwnerGetter = ownerGetter;
        }
    }
}
