﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using TokensAPI;
using TokensBuilder.Errors;
using TokensStandard;
using System.Linq;

namespace TokensBuilder
{
    public static class Context
    {
        #region Propeties and fields
        public static AssemblyBuilder assemblyBuilder = null;
        public static AssemblyName assemblyName = new AssemblyName();
        public static ModuleBuilder moduleBuilder = null;
        private static ClassBuilder _classBuilder = null;
        public static ClassBuilder mainClass = null;
        public static ClassBuilder classBuilder
        {
            get => _classBuilder ?? mainClass;
            set => _classBuilder = value;
        }
        public static FunctionBuilder functionBuilder => classBuilder.methodBuilder;
        public static bool isFuncBody => functionBuilder != null && !functionBuilder.IsEmpty;
        private static Generator gen => TokensBuilder.gen;
        private static ILGenerator ilg => classBuilder.methodBuilder.generator;
        public static readonly CustomAttributeBuilder entrypointAttr = new CustomAttributeBuilder(
                        typeof(EntrypointAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
            scriptAttr = new CustomAttributeBuilder(
                        typeof(ScriptAttribute).GetConstructor(Type.EmptyTypes), new object[] { }),
            typeAliasAttr = new CustomAttributeBuilder(
                typeof(TypeAliasAttributte).GetConstructor(Type.EmptyTypes), new object[] { });
        public static Dictionary<string, object> constants = new Dictionary<string, object>();
        public static List<MethodInfo> scriptFunctions = new List<MethodInfo>();
        #endregion

        #region 'Find' methods
        public static MethodInfo FindScriptFunction(string name)
        {
            foreach (MethodInfo func in scriptFunctions)
            {
                if (func.Name == name)
                    return func;
            }
            return null;
        }

        public static CustomAttributeBuilder FindAttribute(IEnumerable<string> namespaces)
        {
            string attributeName = gen.reader.string_values.Pop();
            Type[] ctorTypes = Type.EmptyTypes;
            object[] args = new object[] { };
            if (gen.reader.tokens[0] == TokenType.STATEMENT)
            {
                gen.reader.tokens.RemoveAt(0);
                if (gen.reader.bool_values.Pop())
                {
                    //pass
                }
                else
                {
                    TokensBuilder.Error(new NeedEndError(gen.line, "Extra closing bracket in attribute"));
                }
            }
            return new CustomAttributeBuilder(
                GetTypeByName(attributeName, namespaces).GetConstructor(ctorTypes), args); //it`s a pass
        }
        #endregion

        #region 'Get' methods
        public static Type GetTypeByName(string name) => GetTypeByName(name, gen.usingNamespaces);

        public static Type GetTypeByName(string name, IEnumerable<string> namespaces)
        {
            Type type = null;
            type = Type.GetType(name);
            if (type != null) return type;
            foreach (string nameSpace in namespaces)
            {
                type = Type.GetType(nameSpace + '.' + name);
                if (type != null) return type;
            }
            return null;
        }

        public static Type GetInterfaceByName(string name, IEnumerable<string> namespaces)
        {
            Type iface;
            foreach (string nameSpace in namespaces)
            {
                iface = Type.GetType(nameSpace + name);
                if (iface != null)
                {
                    if (!iface.IsInterface) iface = null;
                    else return iface;
                }
            }
            return null;
        }

        public static FieldInfo GetVarByName(string caller, string name, IEnumerable<string> namespaces)
        {
            return GetTypeByName(caller, namespaces).GetField(name);
        }

        public static FieldInfo GetVarByName(string name, IEnumerable<string> namespaces)
        {
            List<string> literals = name.Split('.').ToList();
            name = literals.Last();
            literals.RemoveAt(literals.Count - 1);
            return GetVarByName(string.Join(".", literals), name, namespaces);
        }

        public static FieldInfo GetVarByName(string caller, string name) => GetVarByName(caller, name, gen.usingNamespaces);

        public static FieldInfo GetVarByName(string name) => GetVarByName(name, gen.usingNamespaces);
        #endregion

        #region 'Create' methods
        public static void CreateAssembly(bool autoAssemblyName = false)
        {
            if (Config.header == HeaderType.LIBRARY) Config.outputType = PEFileKinds.Dll;
            else if (Config.header == HeaderType.CONSOLE) Config.outputType = PEFileKinds.ConsoleApplication;
            else if (Config.header == HeaderType.GUI) Config.outputType = PEFileKinds.WindowApplication;
            //else don't change output type in config
            if (autoAssemblyName)
            {
                assemblyName = new AssemblyName
                {
                    Version = Config.version,
                    Name = Config.appName
                };
            }
            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(Config.FileName);
            if (Config.header == HeaderType.CLASS || Config.header == HeaderType.SCRIPT)
            {
                mainClass = new ClassBuilder(Config.MainClassName, "", ClassType.STATIC, SecurityDegree.PRIVATE);
                if (Config.header == HeaderType.SCRIPT)
                {
                    mainClass.SetAttribute(scriptAttr);
                    mainClass.CreateMethod("Main", "void", FuncType.STATIC, SecurityDegree.PRIVATE);
                    functionBuilder.SetAttribute(scriptAttr);
                    functionBuilder.SetAttribute(entrypointAttr);
                    assemblyBuilder.SetEntryPoint(functionBuilder.methodBuilder.GetBaseDefinition());
                }
            }
        }
        #endregion

        #region Methods for manipulations with ILGenerator
        public static void LoadObject(object value)
        {
            if (value == null)
                ilg.Emit(OpCodes.Ldnull);
            else if (value is sbyte b)
                ilg.Emit(OpCodes.Ldc_I4_S, b);
            else if (value is short s)
                ilg.Emit(OpCodes.Ldc_I4, s);
            else if (value is int i)
                ilg.Emit(OpCodes.Ldc_I4, i);
            else if (value is float f)
                ilg.Emit(OpCodes.Ldc_R4, f);
            else if (value is long l)
                ilg.Emit(OpCodes.Ldc_I8, l);
            else if (value is double d)
                ilg.Emit(OpCodes.Ldc_R8, d);
            else if (value is bool bl)
            {
                if (bl) ilg.Emit(OpCodes.Ldc_I4_1);
                else ilg.Emit(OpCodes.Ldc_I4_0);
            }
            else if (value is char c)
                ilg.Emit(OpCodes.Ldc_I4, c);
            else if (value is string str)
                ilg.Emit(OpCodes.Ldstr, str);
        }

        public static void CallMethod(MethodInfo method, bool dontPop = true)
        {
            if (method.IsVirtual)
                ilg.Emit(OpCodes.Callvirt, method);
            else 
                ilg.Emit(OpCodes.Call, method);
            if (!dontPop)
            {
                if (method.ReturnType == typeof(void))
                    ilg.Emit(OpCodes.Nop);
                else
                    ilg.Emit(OpCodes.Pop);
            }
        }

        public static void LoadField(FieldInfo field)
        {
            if (field != null)
            {
                if (field.IsStatic)
                    ilg.Emit(OpCodes.Ldsfld, field);
                else
                    ilg.Emit(OpCodes.Ldfld, field);
            }
            else
                TokensBuilder.Error(new VarNotFoundError(gen.line, "Incorrect field given for load"));
        }

        public static void SetField(FieldInfo field)
        {
            if (field != null)
            {
                if (field.IsStatic)
                    ilg.Emit(OpCodes.Stsfld, field);
                else
                    ilg.Emit(OpCodes.Stfld, field);
            }
            else
                TokensBuilder.Error(new VarNotFoundError(gen.line, "Incorrect field given for assign"));
        }

        public static void LoadLocal(LocalBuilder local)
        {
            if (local != null)
                ilg.Emit(OpCodes.Ldloc, local);
            else
                TokensBuilder.Error(new VarNotFoundError(gen.line, "Incorrect local given for load"));
        }

        public static void SetLocal(LocalBuilder local)
        {
            if (local != null)
                ilg.Emit(OpCodes.Stloc, local);
            else
                TokensBuilder.Error(new VarNotFoundError(gen.line, "Incorrect local given for assign"));
        }

        public static void LoadOperator(Type callerType, OperatorType op)
        {
            switch (op)
            {
                case OperatorType.ADD:
                    if (callerType.IsNumber())
                        ilg.Emit(OpCodes.Add);
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_Addition"));
                    break;
                case OperatorType.SUB:
                    if (callerType.IsNumber())
                        ilg.Emit(OpCodes.Sub);
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_Substraction"));
                    break;
                case OperatorType.MUL:
                    if (callerType.IsNumber())
                        ilg.Emit(OpCodes.Mul);
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_Multiply"));
                    break;
                case OperatorType.DIV:
                    if (callerType.IsNumber())
                        ilg.Emit(OpCodes.Div);
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_Division"));
                    break;
                case OperatorType.MOD:
                    if (callerType.IsNumber())
                        ilg.Emit(OpCodes.Rem);
                    else
                        TokensBuilder.Error(new InvalidOperatorError(
                            gen.line, "Operator MOD cannot using in not-number types"));
                    break;
                case OperatorType.EQ:
                    if (callerType.IsNumber() || callerType == typeof(bool))
                        ilg.Emit(OpCodes.Ceq);
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_Equality"));
                    break;
                case OperatorType.NOTEQ:
                    if (callerType.IsNumber() || callerType == typeof(bool))
                    {
                        ilg.Emit(OpCodes.Ceq);
                        ilg.Emit(OpCodes.Not);
                    }
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_Inequality"));
                    break;
                case OperatorType.AND:
                    if (callerType == typeof(bool))
                        ilg.Emit(OpCodes.And);
                    else
                        TokensBuilder.Error(new InvalidOperatorError(
                            gen.line, "Operator AND cannot using with not-boolean types"));
                    break;
                case OperatorType.OR:
                    if (callerType == typeof(bool))
                        ilg.Emit(OpCodes.Or);
                    else
                        TokensBuilder.Error(new InvalidOperatorError(
                            gen.line, "Operator OR cannot using with not-boolean types"));
                    break;
                case OperatorType.XOR:
                    ilg.Emit(OpCodes.Xor);
                    break;
                case OperatorType.GT:
                    if (callerType.IsNumber())
                        ilg.Emit(OpCodes.Cgt);
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_GreaterThan"));
                    break;
                case OperatorType.LT:
                    if (callerType.IsNumber())
                        ilg.Emit(OpCodes.Clt);
                    else
                        ilg.Emit(OpCodes.Call, callerType.GetMethod("op_LessThan"));
                    break;
                case OperatorType.IN:
                    break;
                case OperatorType.GORE:
                    //if (callerType.IsNumber())
                    //ilg.Emit(OpCodes.Cgt);
                    //else
                    ilg.Emit(OpCodes.Call, callerType.GetMethod("op_GreaterThanOrEqual"));
                    break;
                case OperatorType.LORE:
                    //if (callerType.IsNumber())
                    //ilg.Emit(OpCodes.Clt);
                    //else
                    ilg.Emit(OpCodes.Call, callerType.GetMethod("op_LessThanOrEqual"));
                    break;
                case OperatorType.RANGE:
                    break;
                case OperatorType.POW:
                    ilg.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow"));
                    break;
                default:
                    TokensBuilder.Error(new InvalidOperatorError(
                        gen.line, $"Operator {op} cannot using between two values"));
                    break;
            }
        }

        public static void NewObject(ConstructorInfo constructor)
        {
            if (constructor != null)
                ilg.Emit(OpCodes.Newobj, constructor);
            else
                TokensBuilder.Error(new TypeNotFoundError(gen.line, "Constructor not found given with operator 'new'"));
        }
        #endregion

        public static void Finish()
        {
            if (mainClass.IsEmpty)
            {
                MethodInfo method = null;
                foreach (Type type in moduleBuilder.GetTypes())
                {
                    foreach (MethodInfo methodInfo in type.GetMethods())
                    {
                        if (methodInfo.GetCustomAttribute<EntrypointAttribute>() != null)
                        {
                            method = methodInfo;
                            break;
                        }
                    }
                    if (method != null) break;
                }
                try
                {
                    assemblyBuilder.SetEntryPoint(method, Config.outputType);
                }
                catch
                {
                    //error
                }
            }
            else
            {
                mainClass.methodBuilder.generator.Emit(OpCodes.Ret);
                mainClass.End();
            }
            if (Config.header != (HeaderType.BUILDSCRIPT | HeaderType.TOKENSLIBRARY)) assemblyBuilder.Save(Config.FileName);
        }
    }
}
