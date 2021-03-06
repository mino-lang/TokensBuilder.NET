﻿using System.Collections.Generic;
using TokensAPI;

namespace TokensBuilder.Templates
{
    class UseTemplate : TokensTemplate
    {
        public string ns = "";

        public bool Parse(TokensReader expression, bool expression_end)
        {
            if (expression_end && expression.tokens[0] == TokenType.USING_NAMESPACE
                && expression.tokens.Count == 1)
            {
                ns = expression.string_values.Pop();
                return true;
            }
            else return false;
        }

        public List<TokensError> Run()
        {
            TokensBuilder.gen.usingNamespaces.Add(ns);
            return new List<TokensError>();
        }
    }
}
