namespace Lvlup.Build.ConfigureAwait;

internal sealed partial class AwaitRewriter
{
    private readonly ModuleDefinition ModuleDefinition;

    public AwaitRewriter(ModuleDefinition module)
    {
        ModuleDefinition = module;
    }

    public bool Execute()
    {
        var methods = ModuleDefinition.GetTypes().SelectMany(type => type.Methods).ToArray();
        var stateMachines = methods.Select(method => method.GetAsyncStateMachineType())
            .OfType<TypeDefinition>().Distinct().ToArray();
        var runtimeAsyncMethods = methods.Where(method => method.HasBody && ((int)method.ImplAttributes & 0x2000) != 0).ToArray();
        var changed = false;
        foreach (var type in stateMachines)
        {
            var moveNext = type.Methods.Single(method => method.Name == "MoveNext");
            if (!moveNext.Body.Instructions.Any(GetAwaiterSearch) ||
                !moveNext.Body.Instructions.Any(instruction => instruction.Operand is GenericInstanceMethod reference &&
                    reference.Name == "AwaitUnsafeOnCompleted"))
                continue;

            if (!changed)
                FindRuntimeTypes();
            AddAwaitConfigToAsyncMethod(type, false);
            changed = true;
        }

        foreach (var method in runtimeAsyncMethods)
        {
            if (!method.Body.Instructions.Any(instruction => instruction.Operand is MethodReference reference &&
                reference.DeclaringType.FullName == "System.Runtime.CompilerServices.AsyncHelpers" &&
                reference.Name == "Await" && reference.Parameters.Count == 1 &&
                reference.Parameters[0].ParameterType.GetElementType().FullName is
                    "System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1" or
                    "System.Threading.Tasks.ValueTask" or "System.Threading.Tasks.ValueTask`1"))
                continue;

            if (!changed)
                FindRuntimeTypes();
            AddAwaitConfigToAsyncMethod(method, false);
            changed = true;
        }

        return changed;
    }

    void AddAwaitConfigToAsyncMethod(TypeDefinition type, bool configureAwaitValue)
    {
        ProcessFields(type);

        // Modify MoveNext method
        var method = type.Methods.First(_ => _.Name == "MoveNext");
        method.Body.SimplifyMacros();

        ProcessVariables(configureAwaitValue, method);

        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is MethodReference methodRef)
            {
                TryRedirectMethodInstruction(methodRef, instruction);
                continue;
            }

            if (instruction.Operand is TypeReference typeRef)
            {
                TryRedirectTypeInstruction(typeRef, instruction);
                continue;
            }

            if (instruction.Operand is FieldReference fieldRef)
            {
                TryRedirectFieldInstruction(fieldRef);
                continue;
            }
        }

        method.Body.OptimizeMacros();
    }

    void TryRedirectMethodInstruction(MethodReference method, Instruction instruction)
    {
        // Change Task to ConfiguredTaskAwaitable
        var declaringType = method.DeclaringType;
        if (declaringType.FullName == "System.Threading.Tasks.Task")
        {
            // Only redirect GetAwaiter; constructors and other members must not be redirected
            if (method.Name != "GetAwaiter")
                return;

            var newOperand = configuredTaskAwaitableTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        if (declaringType.FullName == "System.Threading.Tasks.ValueTask")
        {
            // Only redirect GetAwaiter; constructors and other members must not be redirected
            if (method.Name != "GetAwaiter")
                return;

            var newOperand = configuredValueTaskAwaitableTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        // Change TaskAwaiter to ConfiguredTaskAwaiter
        if (declaringType.FullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            var newOperand = configuredTaskAwaiterTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        // Change TaskAwaiter to ConfiguredTaskAwaiter
        if (declaringType.FullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            var newOperand = configuredValueTaskAwaiterTypeDef.Method(method);
            if (newOperand != null)
            {
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = ModuleDefinition.ImportReference(newOperand);
            }

            return;
        }

        if (declaringType is GenericInstanceType genericType)
        {
            var genericArguments = genericType.GenericArguments;
            // Change Task`1 to ConfiguredTaskAwaitable`1
            if (declaringType.FullName.StartsWith("System.Threading.Tasks.Task`1"))
            {
                // Only redirect GetAwaiter; other members (including constructors) must not be redirected
                if (method.Name != "GetAwaiter")
                {
                    return;
                }

                var newOperand = genericConfiguredTaskAwaitableTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredTaskAwaitableTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }

            // Change Task`1 to ConfiguredTaskAwaitable`1
            if (declaringType.FullName.StartsWith("System.Threading.Tasks.ValueTask`1"))
            {
                // Only redirect GetAwaiter; other members (including constructors) must not be redirected
                if (method.Name != "GetAwaiter")
                {
                    return;
                }

                var newOperand = genericConfiguredValueTaskAwaitableTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredValueTaskAwaitableTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }

            // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
            if (declaringType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
            {
                var newOperand = genericConfiguredTaskAwaiterTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }

            // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
            if (declaringType.FullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
            {
                var newOperand = genericConfiguredValueTaskAwaiterTypeDef.Method(method);
                if (newOperand != null)
                {
                    var newOperandRef = ModuleDefinition.ImportReference(newOperand);
                    newOperandRef.DeclaringType = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newOperandRef;
                }

                return;
            }
        }

        // Change AwaitUnsafeOnCompleted<TaskAwaiter, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter, T>
        // Change AwaitUnsafeOnCompleted<TaskAwaiter`1, T> to AwaitUnsafeOnCompleted<ConfiguredTaskAwaiter`1, T>
        if (method.Name == "AwaitUnsafeOnCompleted" && method is GenericInstanceMethod awaitUnsafeOnCompleted)
        {
            var arguments = awaitUnsafeOnCompleted.GenericArguments;
            for (var index = 0; index < arguments.Count; index++)
            {
                var argument = arguments[index];
                var fullName = argument.FullName;

                if (fullName == "System.Runtime.CompilerServices.TaskAwaiter")
                {
                    arguments[index] = configuredTaskAwaiterTypeRef;
                    continue;
                }

                if (fullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
                {
                    arguments[index] = configuredValueTaskAwaiterTypeRef;
                    continue;
                }

                if (argument is GenericInstanceType genericInstanceType)
                {
                    var genericArguments = genericInstanceType.GenericArguments;
                    if (fullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
                    {
                        arguments[index] = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        continue;
                    }

                    if (fullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
                    {
                        arguments[index] = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
                        continue;
                    }
                }
            }
        }
    }

    void TryRedirectTypeInstruction(TypeReference type, Instruction instruction)
    {
        // Change TaskAwaiter to ConfiguredTaskAwaiter
        var typeFullName = type.FullName;
        if (typeFullName == "System.Runtime.CompilerServices.TaskAwaiter")
        {
            instruction.Operand = configuredTaskAwaiterTypeRef;
            return;
        }

        if (typeFullName == "System.Runtime.CompilerServices.ValueTaskAwaiter")
        {
            instruction.Operand = configuredValueTaskAwaiterTypeRef;
            return;
        }

        // Change TaskAwaiter`1 to ConfiguredTaskAwaiter`1
        if (type is not GenericInstanceType genericType)
        {
            return;
        }

        var genericArguments = genericType.GenericArguments;

        if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.TaskAwaiter`1"))
        {
            instruction.Operand = genericConfiguredTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
        else if (genericType.FullName.StartsWith("System.Runtime.CompilerServices.ValueTaskAwaiter`1"))
        {
            instruction.Operand = genericConfiguredValueTaskAwaiterTypeRef.MakeGenericInstanceType(genericArguments);
        }
    }
}
