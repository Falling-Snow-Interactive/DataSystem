using System;

namespace Fsi.DataSystem.Libraries.Browsers
{
    public sealed class LibraryMapping
    {
        /// <summary>
        /// Gets a delegate that resolves the library instance.
        /// </summary>
        public Func<object> Getter { get; }
        
        /// <summary>
        /// Gets the library type.
        /// </summary>
        public Type LibraryType { get; }
        
        /// <summary>
        /// Gets the library identifier type.
        /// </summary>
        public Type IdType { get; }
        
        /// <summary>
        /// Gets the library data type.
        /// </summary>
        public Type DataType { get; }
            
        public LibraryMapping(Func<object> getter, Type libraryType)
        {
            Getter = getter;
            LibraryType = libraryType;
            if (libraryType != null && libraryType.IsGenericType)
            {
                Type[] arguments = libraryType.GetGenericArguments();
                if (arguments.Length == 2)
                {
                    IdType = arguments[0];
                    DataType = arguments[1];
                }
            }
        }
    }
}