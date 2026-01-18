using System;

namespace Fsi.DataSystem.Libraries
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class LibraryTypeAttribute : Attribute
    {
        public const string DefaultMemberName = "Library";

        public Type ProviderType { get; }
        public string MemberName { get; }

        public Type LibraryType => ProviderType;

        public LibraryTypeAttribute(Type providerType) : this(providerType, DefaultMemberName)
        {
        }

        public LibraryTypeAttribute(Type providerType, string memberName)
        {
            ProviderType = providerType;
            MemberName = memberName;
        }
    }
}
