﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace FunctionalExtentions
{
    internal static class FactoryObjectCreator
    {
        private const string FactoryDelegateName = "CreateInstanceFactory";
        private static readonly Type DefaultDelegateResult = typeof(object);
        private static readonly Type[] ConcreteDelegateDefaultArgType = { typeof(object[]) };
        private static readonly Type DefaultInstanceDelegateType = typeof(CreateDefaultInstance);
        private static readonly Type InstanceDelegateType = typeof(CreateInstanceDelegate);

        public static FactoryObject GenerateInstanceFactory<T>()
        {
            var defInstanceDelegate = ConstructDefaultInstanceDelegate(typeof(T));
            var factory = new FactoryObject(defInstanceDelegate);
            return factory;
        }

        public static FactoryObject GenerateInstanceFactory(Type instanceType)
        {
            var defInstanceDelegate = ConstructDefaultInstanceDelegate(instanceType);
            var factory = new FactoryObject(defInstanceDelegate);
            return factory;
        }

        public static FactoryObject GenerateInstanceFactory(Type instanceType, object[] args)
        {
            FactoryObject factory;
            if (args == null || args.Length == 0)
            {
                var defInstanceDelegate = ConstructDefaultInstanceDelegate(instanceType);
                factory = new FactoryObject(defInstanceDelegate);
            }
            else
            {
                var instanceDelegate = ConstructCreateInstanceDelegate(instanceType, args);
                factory = new FactoryObject(instanceDelegate);
            }
            return factory;
        }

        private static CreateDefaultInstance ConstructDefaultInstanceDelegate(Type instanceType)
        {
            return (CreateDefaultInstance)CreateDynamicFactory(instanceType, Type.EmptyTypes, DefaultInstanceDelegateType);
        }

        private static CreateInstanceDelegate ConstructCreateInstanceDelegate(Type instanceType, object[] args)
        {
            var argTypes = GetTypeArrayFromArgs(args);
            var @delegate =
                (CreateInstanceDelegate)CreateDynamicFactory(instanceType,
                                                                ConcreteDelegateDefaultArgType,
                                                                InstanceDelegateType,
                                                                argTypes);
            return @delegate;
        }

        private static Delegate CreateDynamicFactory(Type instanceType, Type[] parameterTypes,
            Type delegateType, Type[] argTypes = null)
        {
            var timer = Stopwatch.StartNew();
            if (argTypes == null)
                argTypes = Type.EmptyTypes;
            timer.Stop();
            Console.WriteLine($"Typename conversion taked {timer.Elapsed}");

            timer.Restart();
            ConstructorInfo constructor = instanceType.GetConstructor(argTypes);

            timer.Stop();
            Console.WriteLine($"Constructor extracting taked {timer.Elapsed}");

            timer.Restart();
            var retType = instanceType.IsValueType ? DefaultDelegateResult : instanceType;
            timer.Stop();
            Console.WriteLine($"RetType choosing taked {timer.Elapsed}");
            timer.Restart();

            var method = new DynamicMethod(
                name: FactoryDelegateName,
                returnType: retType,
                parameterTypes: parameterTypes,
                m: typeof(FactoryObjectCreator).Module,
                skipVisibility: true);

            timer.Stop();
            Console.WriteLine($"Dynamic method creation taked {timer.Elapsed}");

            timer.Restart();
            ILGenerator ilGen = method.GetILGenerator();
            // Constructor for value types could be null
            if (constructor != null)
            {
                for (int i = 0; i < argTypes.Length; i++)
                {
                    var argType = argTypes[i];
                    ilGen.Emit(OpCodes.Ldarg, i);
                    ilGen.Emit(OpCodes.Ldc_I4, i);
                    ilGen.Emit(OpCodes.Ldelem_Ref);
                    if (argType.IsValueType)
                        ilGen.Emit(OpCodes.Unbox_Any, argType);
                    else
                        ilGen.Emit(OpCodes.Castclass, argType);
                }
                ilGen.Emit(OpCodes.Newobj, constructor);
            }
            else
            {
                LocalBuilder temp = ilGen.DeclareLocal(instanceType);
                ilGen.Emit(OpCodes.Ldloca, temp);
                ilGen.Emit(OpCodes.Initobj, instanceType);
                ilGen.Emit(OpCodes.Ldloc, temp);
                ilGen.Emit(OpCodes.Box, instanceType);
            }

            ilGen.Emit(OpCodes.Ret);
            timer.Stop();
            Console.WriteLine($"Il code generation taked {timer.Elapsed}");
            timer.Restart();
            var @delegate = method.CreateDelegate(delegateType);
            timer.Stop();
            Console.WriteLine($"Delegate creation taked {timer.Elapsed} \n");

            return @delegate;
        }

        private static Type[] GetTypeArrayFromArgs(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return Type.EmptyTypes;
            }

            return args.Select(x => x.GetType()).ToArray();
        }
    }
}