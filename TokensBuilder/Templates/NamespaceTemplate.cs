﻿using System.Collections.Generic;
using TokensAPI;

namespace TokensBuilder.Templates
{
    class NamespaceTemplate : TokensTemplate
    {
        public bool Parse(TokensReader expression, bool expression_end)
        {
            return expression_end && expression.tokens[0] == TokenType.NAMESPACE
                && expression.tokens.Count == 1;
        }

        public List<TokensError> Run(TokensReader expression)
        {
            TokensBuilder.gen.currentNamespace = expression.string_values.Pop();
            return new List<TokensError>();
        }
    }
}
