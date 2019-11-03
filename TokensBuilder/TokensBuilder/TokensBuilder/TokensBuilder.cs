﻿using System;
using System.Reflection.Emit;
using System.Reflection;
using System.IO;
using TokensAPI;
using System.Collections.Generic;
using System.CodeDom.Compiler;

namespace TokensBuilder
{
    public enum OutputType
    {
        ConsoleApp,
        ConsoleLibrary
    }

    public static class TokensBuilder
    {
        public static AppDomain cd;
        public static AssemblyBuilder ab;
        public static MethodBuilder main_method;
        public static CompilerParameters cp = new CompilerParameters();

        public static string info
        {
            get => "TokensBuilder by snaulX\n" +
                $"Version - {Assembly.GetExecutingAssembly().GetName().Version}\n" +
                "For get info write \"TokensBuilder -info\" in your command line";
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(info);
                Console.ReadKey();
            }
            else
            {
                switch (args[0])
                {
                    case "-o":
                        using (StreamReader file = File.OpenText(Path.GetFullPath(args[1])))
                        {
                            Build(args[2], file.ReadToEnd());
                        }
                        break;
                    case "-info":
                        Console.WriteLine(info);
                        break;
                    default:
                        string filename = args[0];
                        using (StreamReader file = File.OpenText(Path.GetFullPath(filename)))
                        {
                            Build(filename.Remove(filename.LastIndexOf('.')), file.ReadToEnd());
                        }
                        break;
                }
            }
        }

        public static void Build(string assembly_name, string code)
        {
            List<Expression> expressions = new List<Expression>();
            string[] lines = code.Split('\n', '\r');
            for (int i = 0; i < lines.Length; i++)
            {
                string[] ids = lines[i].Split(' ', '\t');
                Expression expr = new Expression();
                expr.token = TokensAPI.Main.GetToken(ids[0]);
                for (int j = 1; j < ids.Length; j++)
                {
                    expr.arguments.Add(Identifer.GetIdentifer(ids[j]));
                }
            }
            Build(assembly_name, expressions);
        }

        public static void Build(string assembly_name, List<Expression> expressions)
        {
            AppDomainSetup ads = new AppDomainSetup();
            ads.ApplicationName = assembly_name + "TokensApplication";
            cd = AppDomain.CreateDomain("CompilerDomain");
            AssemblyName aName = new AssemblyName(assembly_name);
            ab = cd.DefineDynamicAssembly(
                    aName,
                    AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder main_module = ab.DefineDynamicModule("main");
            TypeBuilder main_type = main_module.DefineType("Main");
            main_type.CreateType();
            main_method = main_type.DefineMethod("Main", MethodAttributes.Static, CallingConventions.Any);
            for (int i = 0; i < expressions.Count; i++)
            {
                main_method = expressions[i].Parse(main_method);
            }
        }
    }

    public class Expression
    {
        public Token token;
        public List<Identifer> arguments;

        public Expression()
        {
            token = Token.NULL;
            arguments = new List<Identifer>();
        }

        public MethodBuilder Parse(MethodBuilder current)
        {
            switch (token)
            {
                case Token.NULL:
                    //do nothing
                    break;
                case Token.USE:
                    Identifer id = arguments[0];
                    if (id is TokensAPI.identifers.Array)
                    {
                        foreach (Identifer ide in ((TokensAPI.identifers.Array) id).elements)
                        {
                            //while nothing
                        }
                    }
                    else
                    {
                        //while nothing
                    }
                    break;
                case Token.WRITEVAR:
                    break;
                case Token.NEWCLASS:
                    break;
                case Token.NEWVAR:
                    break;
                case Token.NEWFUNC:
                    break;
                case Token.END:
                    break;
                case Token.GETCLASS:
                    break;
                case Token.GETVAR:
                    break;
                case Token.GETFUNC:
                    break;
                case Token.RUNFUNC:
                    break;
                case Token.WHILE:
                    break;
                case Token.FOR:
                    break;
                case Token.FOREACH:
                    break;
                case Token.BREAK:
                    break;
                case Token.CONTINUE:
                    break;
                case Token.RETURN:
                    break;
                case Token.IF:
                    break;
                case Token.ELSE:
                    break;
                case Token.ELIF:
                    break;
                case Token.GOTO:
                    break;
                case Token.LABEL:
                    break;
                case Token.YIELD:
                    break;
                case Token.GETLINK:
                    break;
                case Token.WRITEINPOINTER:
                    break;
                case Token.NEWSTRUCT:
                    break;
                case Token.NEWINTERFACE:
                    break;
                case Token.NEWENUM:
                    break;
                case Token.NEWMODULE:
                    TokensBuilder.ab.DefineDynamicModule(arguments[0].GetValue());
                    break;
                case Token.NEWCONSTRUCTOR:
                    break;
                case Token.NEWATTRIBUTE:
                    break;
                case Token.GETATTRIBUTE:
                    break;
                case Token.GETCONSTRUCTOR:
                    break;
                case Token.OPCODEADD:
                    break;
                case Token.NEWEVENT:
                    break;
                case Token.GETEVENT:
                    break;
                case Token.TRY:
                    break;
                case Token.CATCH:
                    break;
                case Token.IMPLEMENTS:
                    break;
                case Token.THROW:
                    break;
                case Token.CALLCONSTRUCTOR:
                    break;
                case Token.ABSTRACT:
                    break;
                case Token.STATIC:
                    break;
                case Token.OVERRIDE:
                    break;
                case Token.GET:
                    break;
                case Token.SET:
                    break;
                case Token.TYPEOF:
                    break;
                case Token.CONST:
                    break;
                case Token.INTERNAL:
                    break;
                case Token.SEALED:
                    break;
                case Token.EXTERNAL:
                    break;
                case Token.PUBLIC:
                    break;
                case Token.PRIVATE:
                    break;
                case Token.PROTECTED:
                    break;
                case Token.OPERATOR:
                    break;
                case Token.ASYNC:
                    break;
                case Token.AWAIT:
                    break;
                case Token.SWITCH:
                    break;
                case Token.CASE:
                    break;
                case Token.DEFAULT:
                    break;
                case Token.NEWPOINTER:
                    break;
            }
            return TokensBuilder.main_method;
        }
    }
}
