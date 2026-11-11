namespace Lvlup.Build.ConfigureAwait;

internal static class CecilExtensions
{
    public static MethodDefinition Method(this TypeDefinition type, MethodReference reference)
    {
        return type.Methods.FirstOrDefault(item => item.Name == reference.Name);
    }

    public static void InsertBefore(this ILProcessor processor, Instruction target, params Instruction[] instructions)
    {
        foreach (var instruction in instructions)
        {
            processor.InsertBefore(target, instruction);
        }
    }

    // Async iterators (async IAsyncEnumerable<T> with yield) are lowered to a state machine
    // just like normal async methods, but the generated method carries
    // AsyncIteratorStateMachineAttribute rather than AsyncStateMachineAttribute.
    static bool IsAsyncStateMachineAttribute(this CustomAttribute attribute)
    {
        var fullName = attribute.AttributeType.FullName;
        return fullName is
            "System.Runtime.CompilerServices.AsyncStateMachineAttribute" or
            "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute";
    }

    public static TypeDefinition GetAsyncStateMachineType(this ICustomAttributeProvider provider)
    {
        var attribute = provider.CustomAttributes
            .FirstOrDefault(IsAsyncStateMachineAttribute);

        return (TypeDefinition)attribute?.ConstructorArguments[0].Value;
    }

    public static GenericInstanceType MakeGenericInstanceType(this TypeReference self, IEnumerable<TypeReference> arguments)
    {
        var instance = new GenericInstanceType(self);
        foreach (var arg in arguments)
        {
            instance.GenericArguments.Add(arg);
        }

        return instance;
    }
}
