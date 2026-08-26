using System;
using System.Collections.Generic;
using System.Text;
using Ardalis.GuardClauses;

namespace ActDim.Emitron.Razor
{
    /// <summary>
    /// Transpiles Razor syntax templates into executable C# Roslyn script code.
    /// </summary>
    public static class RazorParser
    {
        private static readonly HashSet<string> ControlKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "if", "else", "foreach", "for", "while", "do", "switch"
        };

        /// <summary>
        /// Converts <paramref name="razorTemplate"/> into C# script statements compatible with <see cref="Emitron.Compile{T}(string, string, EmitronOptions?)"/>.
        /// </summary>
        /// <param name="razorTemplate">The input Razor template text.</param>
        /// <param name="inputParameterName">The parameter name bound to input data (defaults to <c>@params</c>).</param>
        /// <returns>The generated C# script code.</returns>
        public static string Transpile(string razorTemplate, string inputParameterName = Emitron.DefaultInputParameterName)
        {
            Guard.Against.Null(razorTemplate, nameof(razorTemplate));
            var normParam = Emitron.NormalizeInputParameterName(inputParameterName);

            var codeBuilder = new StringBuilder();
            codeBuilder.AppendLine("var __sb = new System.Text.StringBuilder();");
            codeBuilder.AppendLine($"dynamic Model = {normParam};");

            var index = 0;
            ParseBlock(razorTemplate, ref index, codeBuilder, stopAtCloseBrace: false);

            codeBuilder.AppendLine("return __sb.ToString();");
            return codeBuilder.ToString();
        }

        private static void ParseBlock(
            string template,
            ref int i,
            StringBuilder code,
            bool stopAtCloseBrace)
        {
            var textBuffer = new StringBuilder();

            void FlushTextBuffer()
            {
                if (textBuffer.Length > 0)
                {
                    var text = textBuffer.ToString();
                    textBuffer.Clear();
                    var escaped = text.Replace("\"", "\"\"");
                    code.AppendLine($"__sb.Append(@\"{escaped}\");");
                }
            }

            while (i < template.Length)
            {
                var c = template[i];

                if (stopAtCloseBrace && c == '}')
                {
                    FlushTextBuffer();
                    i++;
                    return;
                }

                if (c == '@')
                {
                    if (i + 1 < template.Length && template[i + 1] == '@')
                    {
                        textBuffer.Append('@');
                        i += 2;
                        continue;
                    }

                    if (i + 1 < template.Length && template[i + 1] == '*')
                    {
                        FlushTextBuffer();
                        i += 2;
                        var endComment = template.IndexOf("*@", i, StringComparison.Ordinal);
                        if (endComment >= 0)
                        {
                            i = endComment + 2;
                        }
                        else
                        {
                            i = template.Length;
                        }

                        continue;
                    }

                    if (i + 1 < template.Length && template[i + 1] == '{')
                    {
                        FlushTextBuffer();
                        i += 2;
                        var startCode = i;
                        var depth = 1;
                        while (i < template.Length && depth > 0)
                        {
                            if (template[i] == '{')
                            {
                                depth++;
                            }
                            else if (template[i] == '}')
                            {
                                depth--;
                                if (depth == 0)
                                {
                                    break;
                                }
                            }

                            i++;
                        }

                        var rawCode = template.Substring(startCode, i - startCode);
                        if (i < template.Length && template[i] == '}')
                        {
                            i++;
                        }

                        code.AppendLine(rawCode.Trim());
                        continue;
                    }

                    if (i + 1 < template.Length && template[i + 1] == '(')
                    {
                        FlushTextBuffer();
                        i += 2;
                        var expr = ParseParenthesizedExpression(template, ref i);
                        code.AppendLine($"__sb.Append(({expr}));");
                        continue;
                    }

                    i++;
                    var word = ReadIdentifierOrKeyword(template, ref i);

                    if (!string.IsNullOrEmpty(word) && ControlKeywords.Contains(word))
                    {
                        FlushTextBuffer();
                        ParseControlChain(template, ref i, word, code);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(word))
                    {
                        FlushTextBuffer();
                        var exprSb = new StringBuilder();
                        exprSb.Append(word);

                        while (i < template.Length)
                        {
                            var ch = template[i];
                            if (ch == '.')
                            {
                                exprSb.Append('.');
                                i++;
                                var nextPart = ReadIdentifierOrKeyword(template, ref i);
                                exprSb.Append(nextPart);
                            }
                            else if (ch == '(')
                            {
                                exprSb.Append('(');
                                i++;
                                var args = ParseParenthesizedExpression(template, ref i);
                                exprSb.Append(args);
                                exprSb.Append(')');
                            }
                            else if (ch == '[')
                            {
                                exprSb.Append('[');
                                i++;
                                var bracketContent = ParseBracketExpression(template, ref i);
                                exprSb.Append(bracketContent);
                                exprSb.Append(']');
                            }
                            else
                            {
                                break;
                            }
                        }

                        var fullExpr = exprSb.ToString();
                        code.AppendLine($"__sb.Append({fullExpr});");
                        continue;
                    }

                    textBuffer.Append('@');
                    continue;
                }

                textBuffer.Append(c);
                i++;
            }

            FlushTextBuffer();
        }

        private static void ParseControlChain(string template, ref int i, string word, StringBuilder code)
        {
            if (word == "else" && PeekNextWord(template, i) == "if")
            {
                i = SkipWhitespace(template, i);
                i += 2;
                word = "else if";
            }

            var headerSb = new StringBuilder();
            headerSb.Append(word);

            while (i < template.Length && template[i] != '{')
            {
                headerSb.Append(template[i]);
                i++;
            }

            if (i < template.Length && template[i] == '{')
            {
                i++;
                code.AppendLine(headerSb.ToString().Trim());
                code.AppendLine("{");

                ParseBlock(template, ref i, code, stopAtCloseBrace: true);

                code.AppendLine("}");

                var nextIdx = SkipWhitespace(template, i);
                if (nextIdx < template.Length && StartsWithWord(template, nextIdx, "else"))
                {
                    i = nextIdx + 4;
                    ParseControlChain(template, ref i, "else", code);
                }
            }
            else
            {
                code.AppendLine(headerSb.ToString().Trim() + ";");
            }
        }

        private static bool StartsWithWord(string template, int i, string word)
        {
            if (i + word.Length > template.Length)
            {
                return false;
            }

            for (var j = 0; j < word.Length; j++)
            {
                if (template[i + j] != word[j])
                {
                    return false;
                }
            }

            var nextCharIndex = i + word.Length;
            if (nextCharIndex < template.Length && (char.IsLetterOrDigit(template[nextCharIndex]) || template[nextCharIndex] == '_'))
            {
                return false;
            }

            return true;
        }

        private static string ReadIdentifierOrKeyword(string template, ref int i)
        {
            var start = i;
            while (i < template.Length && (char.IsLetterOrDigit(template[i]) || template[i] == '_'))
            {
                i++;
            }

            return template.Substring(start, i - start);
        }

        private static string PeekNextWord(string template, int i)
        {
            i = SkipWhitespace(template, i);
            var start = i;
            while (i < template.Length && (char.IsLetterOrDigit(template[i]) || template[i] == '_'))
            {
                i++;
            }

            return template.Substring(start, i - start);
        }

        private static int SkipWhitespace(string template, int i)
        {
            while (i < template.Length && char.IsWhiteSpace(template[i]))
            {
                i++;
            }

            return i;
        }

        private static string ParseParenthesizedExpression(string template, ref int i)
        {
            var sb = new StringBuilder();
            var depth = 1;
            while (i < template.Length && depth > 0)
            {
                var c = template[i];
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++;
                        break;
                    }
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        private static string ParseBracketExpression(string template, ref int i)
        {
            var sb = new StringBuilder();
            var depth = 1;
            while (i < template.Length && depth > 0)
            {
                var c = template[i];
                if (c == '[')
                {
                    depth++;
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++;
                        break;
                    }
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }
    }
}

