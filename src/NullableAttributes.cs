// Polyfill for nullable-reference-type attributes the compiler emits.
//
// The new Il2Cppmscorlib.dll shadows the real .NET 6 System.Runtime and its
// NullableAttribute lacks the constructor signatures the compiler expects.
// Defining these types ourselves lets the compiler find and use them.

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field |
        AttributeTargets.GenericParameter | AttributeTargets.Parameter |
        AttributeTargets.Property | AttributeTargets.ReturnValue,
        AllowMultiple = false, Inherited = false)]
    internal sealed class NullableAttribute : Attribute
    {
        public readonly byte[] NullableFlags;

        public NullableAttribute(byte value)
        {
            NullableFlags = new[] { value };
        }

        public NullableAttribute(byte[] value)
        {
            NullableFlags = value;
        }
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Interface |
        AttributeTargets.Method | AttributeTargets.Struct,
        AllowMultiple = false, Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;

        public NullableContextAttribute(byte value)
        {
            Flag = value;
        }
    }
}
