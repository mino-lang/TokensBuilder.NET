﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TokensAPI;
using TokensBuilder.Errors;

namespace TokensBuilder
{
    public sealed class FunctionBuilder
    {
        public bool IsEmpty => methodBuilder == null && constructorBuilder == null;
        public MethodBuilder methodBuilder;
        public ConstructorBuilder constructorBuilder;
        public FuncType type;
        public Dictionary<string, LocalBuilder> localVariables, localFinals;
        public ParameterAttributes parameterAttributes;
        public ILGenerator generator => constructorBuilder == null ? methodBuilder.GetILGenerator() : constructorBuilder.GetILGenerator();
        private Generator gen => TokensBuilder.gen;

        public LocalBuilder DeclareLocal(string typeName)
        {
            Type type = Context.GetTypeByName(typeName);
            if (type == null)
            {
                gen.errors.Add(new TypeNotFoundError(gen.line, $"Type with name '{typeName}' for local variable not found"));
                return null;
            }
            return generator.DeclareLocal(type);
        }
        public LocalBuilder GetLocal(string name)
        {
            try
            {
                return localFinals[name];
            }
            catch (KeyNotFoundException)
            {
                try
                {
                    return localVariables[name];
                }
                catch (KeyNotFoundException)
                {
                    return null;
                }
            }
        }

        public FunctionBuilder(MethodBuilder methodBuilder) : this()
        {
            this.methodBuilder = methodBuilder;
        }

        public FunctionBuilder(ConstructorBuilder constructorBuilder) : this()
        {
            this.constructorBuilder = constructorBuilder;
        }

        public FunctionBuilder()
        {
            localVariables = new Dictionary<string, LocalBuilder>();
            localFinals = new Dictionary<string, LocalBuilder>();
            methodBuilder = null;
            constructorBuilder = null;
            type = FuncType.DEFAULT;
            parameterAttributes = ParameterAttributes.None;
        }

        public void SetAttribute(CustomAttributeBuilder attribute)
        {
            if (constructorBuilder == null)
                methodBuilder.SetCustomAttribute(attribute);
            else
                constructorBuilder.SetCustomAttribute(attribute);
        }
    }
}
