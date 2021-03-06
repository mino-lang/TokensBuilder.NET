﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TokensAPI;
using TokensBuilder.Errors;

namespace TokensBuilder.Templates
{
    class IncludeTemplate : TokensTemplate
    {
        public string libPath = "";

        public bool Parse(TokensReader expression, bool expression_end)
        {
            if (expression_end && expression.tokens[0] == TokenType.INCLUDE
                && expression.tokens.Count == 1)
            {
                libPath = expression.string_values.Pop();
                return true;
            }
            else
                return false;
        }

        public List<TokensError> Run()
        {
            List<TokensError> errors = new List<TokensError>();
            uint line = TokensBuilder.gen.line;
            try
            {
                Assembly.LoadFrom(libPath);
            }
            catch (FileNotFoundException)
            {
                errors.Add(new IncludeError(line, $"The {libPath} was not found, or the module" +
                    " you are trying to load does not indicate a file name extension."));
            }
            catch (FileLoadException)
            {
                errors.Add(new IncludeError(line, "Failed to load the file that was found." +
                    " or The ability to execute code in remote assemblies is disabled."));
            }
            catch (BadImageFormatException)
            {
                errors.Add(new IncludeError(line, $"{Path.GetFileName(libPath)} is not valid assembly"));
            }
            catch (ArgumentException)
            {
                errors.Add(new IncludeError(line, $"Name of assembly is empty or not valid"));
            }
            catch (PathTooLongException)
            {
                errors.Add(new IncludeError(line, "The assembly name is longer than the maximum length" +
                    " defined in the system."));
            }
            return errors;
        }
    }
}
