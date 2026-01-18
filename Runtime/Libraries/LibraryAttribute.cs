using System;
using UnityEngine;

namespace Fsi.DataSystem.Libraries
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class LibraryAttribute : PropertyAttribute
    {
        public const string DefaultMemberName = "Library";

        public Type ProviderType { get; }
        public string MemberName { get; }

        public LibraryAttribute()
        {
        }

        public LibraryAttribute(Type providerType) : this(providerType, DefaultMemberName)
        {
        }

        public LibraryAttribute(Type providerType, string memberName)
        {
            ProviderType = providerType;
            MemberName = memberName;
        }
    }
}
