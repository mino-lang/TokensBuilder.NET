﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TokensAPI;
using TokensBuilder.Errors;

namespace TokensBuilder.Templates
{
    public static class PartTemplate
    {
        static uint line => TokensBuilder.gen.line;
        public static List<TokensError> errors = new List<TokensError>();

        public static FieldInfo ParseVar(ref TokensReader expression)
        {
            errors = new List<TokensError>();
            TokenType token = expression.tokens.Peek();
            if (token == TokenType.LITERAL)
            {
                bool mustLiteral = true;
                StringBuilder varName = new StringBuilder();
                while (token == TokenType.LITERAL || token == TokenType.SEPARATOR)
                {
                    if (mustLiteral && token == TokenType.LITERAL)
                    {
                        varName.Append(expression.string_values.Peek());
                        mustLiteral = false;
                    }
                    else if (!mustLiteral && token == TokenType.SEPARATOR)
                    {
                        mustLiteral = true;
                        if (expression.bool_values.Peek())
                            varName.Append(".");
                        else
                        {
                            expression.bool_values.Insert(0, false);
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                    token = expression.tokens.Peek();
                }
                if (mustLiteral)
                {
                    errors.Add(new InvalidTokenError(line, "After separator must be literal"));
                    return null;
                }
                else
                {
                    expression.tokens.Insert(0, token);
                    FieldInfo field = Context.GetVarByName(varName.ToString());
                    if (field == null)
                    {
                        errors.Add(new VarNotFoundError(line, $"Field with name {varName} not found"));
                    }
                    return field;
                }
            }
            else
            {
                expression.tokens.Insert(0, token);
            }
            return null;
        }

        public static MethodInfo ParseCallMethod(ref TokensReader expression)
        {
            errors = new List<TokensError>();
            List<object> parameters = new List<object>();
            List<Type> paramTypes = new List<Type>();
            TokenType token = expression.tokens.Peek();
            if (token == TokenType.LITERAL)
            {
                bool mustLiteral = true;
                StringBuilder parentName = new StringBuilder();
                string lastLiteral = "";
                while (token == TokenType.LITERAL || token == TokenType.SEPARATOR)
                {
                    if (mustLiteral && token == TokenType.LITERAL)
                        lastLiteral = expression.string_values.Peek();
                    else if (!mustLiteral && token == TokenType.SEPARATOR)
                    {
                        if (expression.bool_values.Peek())
                            parentName.Append(lastLiteral + ".");
                        else
                            break;
                    }
                    else
                        break;
                    token = expression.tokens.Peek();
                }
                if (mustLiteral)
                    return null;
                else
                {
                    parentName.Length--; // delete last character - '.'
                    string typename = parentName.ToString();
                    string methname = lastLiteral;
                    if (token == TokenType.STATEMENT && expression.bool_values.Peek())
                    {
                        token = expression.tokens.Peek();
                        if (token == TokenType.STATEMENT && !expression.bool_values.Peek())
                        {
                            CallMethodTemplate callMethod = new CallMethodTemplate();
                            callMethod.methname = methname;
                            callMethod.parameters = parameters;
                            callMethod.paramTypes = paramTypes;
                            callMethod.typename = typename;
                            callMethod.dontPop = true;
                            callMethod.Run(expression);
                            return callMethod.method;
                        }
                        else
                        {
                            parse_param:
                            Type paramType = ParseValue(ref expression, out object val);
                            if (paramType == null)
                                return null;
                            else
                            {
                                paramTypes.Add(paramType);
                                parameters.Add(val);
                                token = expression.tokens.Peek();
                                if (token == TokenType.STATEMENT)
                                {
                                    if (!expression.bool_values.Peek())
                                    {
                                        CallMethodTemplate callMethod = new CallMethodTemplate();
                                        callMethod.methname = methname;
                                        callMethod.parameters = parameters;
                                        callMethod.paramTypes = paramTypes;
                                        callMethod.typename = typename;
                                        callMethod.dontPop = true;
                                        callMethod.Run(expression);
                                        return callMethod.method;
                                    }
                                    else
                                        expression.bool_values.Insert(0, true);
                                }
                                else if (token == TokenType.SEPARATOR && expression.bool_values.Peek())
                                    goto parse_param;
                                else
                                    return null;
                            }
                            return null;
                        }
                    }
                    else
                        return null;
                }
            }
            else
                return null;
        }

        public static Type ParseValue(ref TokensReader expression, out object value)
        {
            Type type = null;
            value = null;
            errors = new List<TokensError>();
            TokenType token = expression.tokens.Peek();
            if (token == TokenType.VALUE)
            {
                byte valtype = expression.byte_values.Peek();
                if (valtype == 0) type = typeof(object);
                else if (valtype == 1) type = typeof(int);
                else if (valtype == 2) type = typeof(string);
                else if (valtype == 3) type = typeof(sbyte);
                else if (valtype == 4) type = typeof(bool);
                else if (valtype == 5) type = typeof(char);
                else if (valtype == 6) type = typeof(float);
                else if (valtype == 7) type = typeof(short);
                else if (valtype == 8) type = typeof(long);
                else if (valtype == 9) type = typeof(double);
                value = expression.values.Peek();
            }
            else if (token == TokenType.LITERAL)
            {
                expression.tokens.Insert(0, TokenType.LITERAL);
                TokensReader backup = new TokensReader();
                backup.Add(expression);
                MethodInfo method = ParseCallMethod(ref expression);
                if (method == null)
                {
                    value = ParseVar(ref backup);
                    expression = backup;
                    if (value is FieldInfo fld)
                        type = fld.FieldType;
                }
                else
                {
                    type = method.ReturnType;
                }
                TokensBuilder.gen.errors.AddRange(errors);
            }
            return type;
        }
    }
}