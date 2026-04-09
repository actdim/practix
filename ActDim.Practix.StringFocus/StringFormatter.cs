using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using ActDim.Practix.TypeAccess.Linq.Dynamic;
using Ardalis.GuardClauses;

namespace ActDim.Practix.StringFocus
{
    public static class StringFormatter
    {
        // TODO: format with parameters: specified by object or Dictionary<string, object>
        /// <summary>
        ///
        /// </summary>
        /// <param name="format"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Format(this string format, object source)
        {
            var @params = new ExtendedArrayList();

            var evalStrings = FormatHelper(format, source, out string newFormat).Select((expression, index) => expression + " as _" + index.ToString()).ToList();

            if (evalStrings.Count == 0 && format == newFormat)
            {
                return format; // format string does not contain parameters (expressions)
            }

            var args = DynamicHelper.EvalGet(source, "new (" + string.Join(", ", [.. evalStrings]) + ")"); // newSource

            DynamicHelper.EvalGet(string.Format("params.AddRange({0})", string.Join(", ", Enumerable.Range(0, evalStrings.Count).Select(index => "this._" + index.ToString()).ToArray())),
                new
                {
                    @this = args,
                    @params
                },
                typeof(void)
            );

            return string.Format(newFormat, [.. @params]);
        }

        public static string Format(this string format, IDictionary<string, object> source)
        {
            var dynamicObj = DynamicTypeFactory.Instance.CreateObject(source);
            return Format(format, dynamicObj);
        }

        private static List<string> FormatHelper(this string format, object source, out string newFormat)
        {
            return FormatHelper(format, source, true, out newFormat);
        }

        private static List<string> FormatHelper(this string format, object source, bool allowCompositeFormat, out string newFormat)
        {
            Guard.Against.Null(format, nameof(format));

            var sb = new StringBuilder(format.Length);

            var expressions = new List<string>();

            using (var reader = new StringReader(format))
            {
                var expressionBuilder = new StringBuilder();
                var @char = -1;

                State state = State.OutsideExpression;
                do
                {
                    switch (state)
                    {
                        case State.OutsideExpression:
                            @char = reader.Read();
                            switch (@char)
                            {
                                case -1:
                                    state = State.End;
                                    break;
                                case '{':
                                    state = State.OnOpenBracket;
                                    break;
                                case '}':
                                    state = State.OnCloseBracket;
                                    break;
                                default:
                                    sb.Append((char)@char);
                                    break;
                            }
                            break;
                        case State.OnOpenBracket:
                            @char = reader.Read();
                            switch (@char)
                            {
                                case -1:
                                    throw new FormatException();
                                case '{':
                                    sb.Append("{{");
                                    state = State.OutsideExpression;
                                    break;
                                default:
                                    expressionBuilder.Append((char)@char);
                                    state = State.InsideExpression;
                                    break;
                            }
                            break;
                        case State.InsideExpression:
                            @char = reader.Read();
                            switch (@char)
                            {
                                case -1:
                                    throw new FormatException();
                                case '}':
                                    if (allowCompositeFormat)
                                    {
                                        var expressionsCountString = expressions.Count.ToString();
                                        var expression = ParseCompositeFormat(expressionBuilder.ToString(), out string alignmentAndFormatString);
                                        sb.Append("{" +
                                                             (alignmentAndFormatString == null
                                                                ? expressionsCountString
                                                                : expressionsCountString + alignmentAndFormatString) + "}");
                                        expressions.Add(expression);
                                    }
                                    else
                                    {
                                        sb.Append("{" + expressions.Count + "}");
                                        expressions.Add(expressionBuilder.ToString());
                                    }
                                    expressionBuilder.Length = 0;
                                    state = State.OutsideExpression;
                                    break;
                                default:
                                    expressionBuilder.Append((char)@char);
                                    break;
                            }
                            break;
                        case State.OnCloseBracket:
                            @char = reader.Read();
                            switch (@char)
                            {
                                case '}':
                                    sb.Append("}}");
                                    state = State.OutsideExpression;
                                    break;
                                default:
                                    throw new FormatException();
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Invalid state.");
                    }
                } while (state != State.End);
            }

            newFormat = sb.ToString();
            return expressions;
        }

        private static string ParseCompositeFormat(string expression, out string alignmentAndFormatString)
        {
            var parts = Regex.Split(expression, "(?<=^([^\"]|\"[^\"]*\")*):");
            var formatString = parts.Length > 1 ? expression.Substring(parts[0].Length + 1) : null;
            expression = parts[0];
            parts = Regex.Split(expression, "(?<=^([^\"]|\"[^\"]*\")*)(?<![(][^)]*),(?![^(]*[)])");
            var alignment = parts.Length > 1 ? expression.Substring(parts[0].Length + 1) : null;

            if (formatString == null)
            {
                if (alignment == null)
                {
                    alignmentAndFormatString = null;
                }
                else
                {
                    alignmentAndFormatString = "," + alignment;
                }
            }
            else
            {
                if (alignment == null)
                {
                    alignmentAndFormatString = ":" + formatString;
                }
                else
                {
                    alignmentAndFormatString = "," + alignment + ":" + formatString;
                }
            }
            return parts[0];
        }

        public class ExtendedArrayList : List<object>
        {
            public ExtendedArrayList()
                : base() { }
            public void AddRange(params object[] items) // AddMany
            {
                base.AddRange(items);
            }
        }

        private enum State
        {
            OutsideExpression,
            OnOpenBracket,
            InsideExpression,
            OnCloseBracket,
            End
        }
    }
}
