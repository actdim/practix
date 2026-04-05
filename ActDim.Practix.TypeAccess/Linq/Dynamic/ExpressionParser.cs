using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ActDim.Practix.TypeAccess.Reflection;

//http://www.codeproject.com/Articles/110065/Quickly-Generate-and-Use-Dynamic-Class

//http://dblinq2007.googlecode.com/svn/trunk/lib/DynamicLinq.cs

//http://docs.go-mono.com/index.aspx?link=N%3aMono.CSharp
//http://www.mono-project.com/CsharpRepl
//http://jint.codeplex.com/releases

//http://www.wintellect.com/CS/blogs/jlikness/archive/2011/04/28/dynamic-types-to-simplify-property-change-notification-in-silverlight-4-and-5.aspx
//http://dotnetslackers.com/articles/net/Using-the-DLR-to-build-Expression-Trees.aspx
//http://blogs.msdn.com/b/csharpfaq/archive/2009/09/14/generating-dynamic-methods-with-expression-trees-in-visual-studio-2010.aspx
//http://blogs.msdn.com/b/vbteam/archive/2007/08/29/implementing-dynamic-searching-using-linq.aspx

//http://forums.silverlight.net/forums/t/146541.aspx
//http://elegantcode.com/2010/05/22/silverlight-databind-to-an-anonymous-targetType-who-knew/
//http://grahammurray.wordpress.com/2010/05/30/binding-to-anonymous-types-in-silverlight/

//http://stackoverflow.com/questions/307512/how-do-i-apply-orderby-on-an-iqueryable-using-a-string-column-name-within-a-gene

//http://kamimucode.com/Home.aspx/C-sharp-Eval/1
//http://static.springsource.org/spring/docs/3.0.5.RELEASE/reference/expressions.html

//http://netmatze.wordpress.com/2011/06/05/implementieren-eines-anonymen-interfaces-in-c/

namespace ActDim.Practix.TypeAccess.Linq.Dynamic
{
    internal class ExpressionParser
    {
        struct Token
        {
            public TokenId Id;
            public string Text;
            public int Pos;
        }

        enum TokenId
        {
            Unknown,
            End,
            Identifier,
            StringLiteral,
            IntegerLiteral,
            RealLiteral,
            Exclamation,
            Percent,
            Amphersand,
            OpenParen,
            CloseParen,
            Asterisk,
            Plus,
            Comma,
            Minus,
            Dot,
            Slash,
            Colon,
            LessThan,
            Equal,
            GreaterThan,
            Question,
            OpenBracket,
            CloseBracket,
            Bar,
            ExclamationEqual,
            DoubleAmphersand,
            LessThanEqual,
            LessGreater,
            DoubleEqual,
            GreaterThanEqual,
            DoubleBar
        }

        interface ILogicalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        interface IArithmeticSignatures
        {
            void F(int x, int y);
            void F(uint x, uint y);
            void F(long x, long y);
            void F(ulong x, ulong y);
            void F(float x, float y);
            void F(double x, double y);
            void F(decimal x, decimal y);
            void F(int? x, int? y);
            void F(uint? x, uint? y);
            void F(long? x, long? y);
            void F(ulong? x, ulong? y);
            void F(float? x, float? y);
            void F(double? x, double? y);
            void F(decimal? x, decimal? y);
        }

        interface IRelationalSignatures : IArithmeticSignatures
        {
            void F(string x, string y);
            void F(char x, char y);
            void F(DateTime x, DateTime y);
            void F(TimeSpan x, TimeSpan y);
            void F(char? x, char? y);
            void F(DateTime? x, DateTime? y);
            void F(TimeSpan? x, TimeSpan? y);
        }

        interface IEqualitySignatures : IRelationalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        interface IAddSignatures : IArithmeticSignatures
        {
            void F(DateTime x, TimeSpan y);
            void F(TimeSpan x, TimeSpan y);
            void F(DateTime? x, TimeSpan? y);
            void F(TimeSpan? x, TimeSpan? y);
        }

        interface ISubtractSignatures : IAddSignatures
        {
            void F(DateTime x, DateTime y);
            void F(DateTime? x, DateTime? y);
        }

        interface INegationSignatures
        {
            void F(int x);
            void F(long x);
            void F(float x);
            void F(double x);
            void F(decimal x);
            void F(int? x);
            void F(long? x);
            void F(float? x);
            void F(double? x);
            void F(decimal? x);
        }

        interface INotSignatures
        {
            void F(bool x);
            void F(bool? x);
        }

        interface IEnumerableSignatures
        {
            void Where(bool predicate);
            void Any();
            void Any(bool predicate);
            void All(bool predicate);
            void Count();
            void Count(bool predicate);
            void Min(object selector);
            void Max(object selector);
            void Sum(int selector);
            void Sum(int? selector);
            void Sum(long selector);
            void Sum(long? selector);
            void Sum(float selector);
            void Sum(float? selector);
            void Sum(double selector);
            void Sum(double? selector);
            void Sum(decimal selector);
            void Sum(decimal? selector);
            void Average(int selector);
            void Average(int? selector);
            void Average(long selector);
            void Average(long? selector);
            void Average(float selector);
            void Average(float? selector);
            void Average(double selector);
            void Average(double? selector);
            void Average(decimal selector);
            void Average(decimal? selector);
        }

        static readonly Type[] PredefinedTypes = {
            typeof(Object),
            typeof(Boolean),
            typeof(Char),
            typeof(String),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(Decimal),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(System.Math),
            typeof(Convert)
        };

        static readonly Expression TrueLiteral = Expression.Constant(true);
        static readonly Expression FalseLiteral = Expression.Constant(false);
        static readonly Expression NullLiteral = Expression.Constant(null);

        private const string KeywordIt = "it";
        private const string KeywordIif = "iif";
        private const string KeywordNew = "new";
        private const string KeywordNull = "null";

        static Dictionary<string, object> _keywords;

        Dictionary<string, object> _symbols;
        IDictionary<string, object> _externals;
        Dictionary<Expression, string> _literals;
        ParameterExpression[] _parameters;

        ParameterExpression _it; //_this/_me/_that
        readonly string _text;
        int _textPos;
        readonly int _textLen;
        char _ch;
        Token _token;

        public ExpressionParser(ParameterExpression[] parameters, string expression, object[] values)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (_keywords == null) _keywords = CreateKeywords();
            _symbols = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _literals = new Dictionary<Expression, string>();
            _parameters = parameters;
            //_parameters = new ParameterExpression[parameters.Length];
            //parameters.CopyTo(_parameters, parameters.GetLowerBound(0));
            //_parameters = parameters.ToArray();
            //Array.Copy
            //Array.ConstrainedCopy  
            //Buffer.BlockCopy
            if (parameters != null) ProcessParameters(parameters);
            if (values != null) ProcessValues(values);
            _text = expression;
            _textLen = _text.Length;
            SetTextPos(0);
            NextToken();
        }

        void ProcessParameters(ParameterExpression[] parameters)
        {
            foreach (ParameterExpression pe in parameters)
                if (!String.IsNullOrEmpty(pe.Name))
                    AddSymbol(pe.Name, pe);
            //if (parameters.Length == 1 && String.IsNullOrEmpty(parameters[0].Name))
            if (String.IsNullOrEmpty(parameters[0].Name))
                _it = parameters[0];
        }

        void ProcessValues(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                object value = values[i];
                if (i == values.Length - 1 && value is IDictionary<string, object>)
                {
                    _externals = (IDictionary<string, object>)value;
                }
                else
                {
                    AddSymbol("@" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), value);
                }
            }
        }

        void AddSymbol(string name, object value)
        {
            if (_symbols.ContainsKey(name))
                throw ParseError(Res.DuplicateIdentifier, name);
            _symbols.Add(name, value);
        }

        public Expression Parse(Type resultType)
        {
            int exprPos = _token.Pos;
            Expression expr = ParseExpression();
            if (resultType != null)
                if ((expr = PromoteExpression(expr, resultType, true)) == null)
                    throw ParseError(exprPos, Res.ExpressionTypeMismatch, GetTypeName(resultType));
            ValidateToken(TokenId.End, Res.SyntaxError);
            return expr;
        }

#pragma warning disable 0219
        public IEnumerable<DynamicOrdering> ParseOrdering()
        {
            List<DynamicOrdering> orderings = new List<DynamicOrdering>();
            while (true)
            {
                Expression expr = ParseExpression();
                bool ascending = true;
                if (TokenIdentifierIs("asc") || TokenIdentifierIs("ascending"))
                {
                    NextToken();
                }
                else if (TokenIdentifierIs("desc") || TokenIdentifierIs("descending"))
                {
                    NextToken();
                    ascending = false;
                }
                orderings.Add(new DynamicOrdering { Selector = expr, Ascending = ascending });
                if (_token.Id != TokenId.Comma) break;
                NextToken();
            }
            ValidateToken(TokenId.End, Res.SyntaxError);
            return orderings;
        }
#pragma warning restore 0219

        // ?: operator
        Expression ParseExpression()
        {
            int errorPos = _token.Pos;
            Expression expr = ParseLogicalOr();
            if (_token.Id == TokenId.Question)
            {
                NextToken();
                Expression expr1 = ParseExpression();
                ValidateToken(TokenId.Colon, Res.ColonExpected);
                NextToken();
                Expression expr2 = ParseExpression();
                expr = GenerateConditional(expr, expr1, expr2, errorPos);
            }
            return expr;
        }

        // ||, or operator
        Expression ParseLogicalOr()
        {
            Expression left = ParseLogicalAnd();
            while (_token.Id == TokenId.DoubleBar || TokenIdentifierIs("or"))
            {
                Token op = _token;
                NextToken();
                Expression right = ParseLogicalAnd();
                CheckAndPromoteOperands(typeof(ILogicalSignatures), op.Text, ref left, ref right, op.Pos);
                left = Expression.OrElse(left, right);
            }
            return left;
        }

        // &&, and operator
        Expression ParseLogicalAnd()
        {
            Expression left = ParseComparison();
            while (_token.Id == TokenId.DoubleAmphersand || TokenIdentifierIs("and"))
            {
                Token op = _token;
                NextToken();
                Expression right = ParseComparison();
                CheckAndPromoteOperands(typeof(ILogicalSignatures), op.Text, ref left, ref right, op.Pos);
                left = Expression.AndAlso(left, right);
            }
            return left;
        }

        // =, ==, !=, <>, >, >=, <, <= operators
        Expression ParseComparison()
        {
            Expression left = ParseAdditive();
            while (_token.Id == TokenId.Equal || _token.Id == TokenId.DoubleEqual ||
                _token.Id == TokenId.ExclamationEqual || _token.Id == TokenId.LessGreater ||
                _token.Id == TokenId.GreaterThan || _token.Id == TokenId.GreaterThanEqual ||
                _token.Id == TokenId.LessThan || _token.Id == TokenId.LessThanEqual)
            {
                Token op = _token;
                NextToken();
                Expression right = ParseAdditive();
                bool isEquality = op.Id == TokenId.Equal || op.Id == TokenId.DoubleEqual ||
                    op.Id == TokenId.ExclamationEqual || op.Id == TokenId.LessGreater;
                if (isEquality && !left.Type.IsValueType && !right.Type.IsValueType)
                {
                    if (left.Type != right.Type)
                    {
                        if (left.Type.IsAssignableFrom(right.Type))
                        {
                            right = Expression.Convert(right, left.Type);
                        }
                        else if (right.Type.IsAssignableFrom(left.Type))
                        {
                            left = Expression.Convert(left, right.Type);
                        }
                        else
                        {
                            throw IncompatibleOperandsError(op.Text, left, right, op.Pos);
                        }
                    }
                }
                else if (IsEnumType(left.Type) || IsEnumType(right.Type))
                {
                    if (left.Type != right.Type)
                    {
                        Expression e;
                        if ((e = PromoteExpression(right, left.Type, true)) != null)
                        {
                            right = e;
                        }
                        else if ((e = PromoteExpression(left, right.Type, true)) != null)
                        {
                            left = e;
                        }
                        else
                        {
                            throw IncompatibleOperandsError(op.Text, left, right, op.Pos);
                        }
                    }
                }
                else
                {
                    CheckAndPromoteOperands(isEquality ? typeof(IEqualitySignatures) : typeof(IRelationalSignatures),
                        op.Text, ref left, ref right, op.Pos);
                }
                switch (op.Id)
                {
                    case TokenId.Equal:
                        left = GenerateAssign(left, right);
                        break;
                    case TokenId.DoubleEqual:
                        left = GenerateEqual(left, right);
                        break;
                    case TokenId.ExclamationEqual:
                    case TokenId.LessGreater:
                        left = GenerateNotEqual(left, right);
                        break;
                    case TokenId.GreaterThan:
                        left = GenerateGreaterThan(left, right);
                        break;
                    case TokenId.GreaterThanEqual:
                        left = GenerateGreaterThanEqual(left, right);
                        break;
                    case TokenId.LessThan:
                        left = GenerateLessThan(left, right);
                        break;
                    case TokenId.LessThanEqual:
                        left = GenerateLessThanEqual(left, right);
                        break;
                }
            }
            return left;
        }

        // +, -, & operators
        Expression ParseAdditive()
        {
            Expression left = ParseMultiplicative();
            while (_token.Id == TokenId.Plus || _token.Id == TokenId.Minus ||
                _token.Id == TokenId.Amphersand)
            {
                Token op = _token;
                NextToken();
                Expression right = ParseMultiplicative();
                switch (op.Id)
                {
                    case TokenId.Plus:
                        if (left.Type == typeof(string) || right.Type == typeof(string))
                            goto case TokenId.Amphersand;
                        CheckAndPromoteOperands(typeof(IAddSignatures), op.Text, ref left, ref right, op.Pos);
                        left = GenerateAdd(left, right);
                        break;
                    case TokenId.Minus:
                        CheckAndPromoteOperands(typeof(ISubtractSignatures), op.Text, ref left, ref right, op.Pos);
                        left = GenerateSubtract(left, right);
                        break;
                    case TokenId.Amphersand:
                        left = GenerateStringConcat(left, right);
                        break;
                }
            }
            return left;
        }

        // *, /, %, mod operators
        Expression ParseMultiplicative()
        {
            Expression left = ParseUnary();
            while (_token.Id == TokenId.Asterisk || _token.Id == TokenId.Slash ||
                _token.Id == TokenId.Percent || TokenIdentifierIs("mod"))
            {
                Token op = _token;
                NextToken();
                Expression right = ParseUnary();
                CheckAndPromoteOperands(typeof(IArithmeticSignatures), op.Text, ref left, ref right, op.Pos);
                switch (op.Id)
                {
                    case TokenId.Asterisk:
                        left = Expression.Multiply(left, right);
                        break;
                    case TokenId.Slash:
                        left = Expression.Divide(left, right);
                        break;
                    case TokenId.Percent:
                    case TokenId.Identifier:
                        left = Expression.Modulo(left, right);
                        break;
                }
            }
            return left;
        }

        // -, !, not unary operators
        Expression ParseUnary()
        {
            if (_token.Id == TokenId.Minus || _token.Id == TokenId.Exclamation ||
                TokenIdentifierIs("not"))
            {
                Token op = _token;
                NextToken();
                if (op.Id == TokenId.Minus && (_token.Id == TokenId.IntegerLiteral ||
                    _token.Id == TokenId.RealLiteral))
                {
                    _token.Text = "-" + _token.Text;
                    _token.Pos = op.Pos;
                    return ParsePrimary();
                }
                Expression expr = ParseUnary();
                if (op.Id == TokenId.Minus)
                {
                    CheckAndPromoteOperand(typeof(INegationSignatures), op.Text, ref expr, op.Pos);
                    expr = Expression.Negate(expr);
                }
                else
                {
                    CheckAndPromoteOperand(typeof(INotSignatures), op.Text, ref expr, op.Pos);
                    expr = Expression.Not(expr);
                }
                return expr;
            }
            return ParsePrimary();
        }

        Expression ParsePrimary()
        {
            var expr = ParsePrimaryStart();
            while (true)
            {
                if (_token.Id == TokenId.Dot)
                {
                    NextToken();
                    expr = ParseMemberAccess(null, expr);
                }
                else if (_token.Id == TokenId.OpenBracket)
                {
                    expr = ParseElementAccess(expr);
                }
                else
                {
                    break;
                }
            }
            return expr;
        }

        Expression ParsePrimaryStart()
        {
            switch (_token.Id)
            {
                case TokenId.Identifier:
                    return ParseIdentifier();
                case TokenId.StringLiteral:
                    return ParseStringLiteral();
                case TokenId.IntegerLiteral:
                    return ParseIntegerLiteral();
                case TokenId.RealLiteral:
                    return ParseRealLiteral();
                case TokenId.OpenParen:
                    return ParseParenExpression();
                default:
                    throw ParseError(Res.ExpressionExpected);
            }
        }

        Expression ParseStringLiteral()
        {
            ValidateToken(TokenId.StringLiteral);
            char quote = _token.Text[0];
            string s = _token.Text.Substring(1, _token.Text.Length - 2);
            int start = 0;
            while (true)
            {
                int i = s.IndexOf(quote, start);
                if (i < 0) break;
                s = s.Remove(i, 1);
                start = i + 1;
            }
            if (quote == '\'')
            {
                if (s.Length != 1)
                    throw ParseError(Res.InvalidCharacterLiteral);
                NextToken();
                return CreateLiteral(s[0], s);
            }
            NextToken();
            return CreateLiteral(s, s);
        }

        Expression ParseIntegerLiteral()
        {
            ValidateToken(TokenId.IntegerLiteral);
            string text = _token.Text;
            if (text[0] != '-')
            {
                ulong value;
                if (!UInt64.TryParse(text, out value))
                    throw ParseError(Res.InvalidIntegerLiteral, text);
                NextToken();
                if (value <= (ulong)Int32.MaxValue) return CreateLiteral((int)value, text);
                if (value <= (ulong)UInt32.MaxValue) return CreateLiteral((uint)value, text);
                if (value <= (ulong)Int64.MaxValue) return CreateLiteral((long)value, text);
                return CreateLiteral(value, text);
            }
            else
            {
                long value;
                if (!Int64.TryParse(text, out value))
                    throw ParseError(Res.InvalidIntegerLiteral, text);
                NextToken();
                if (value >= Int32.MinValue && value <= Int32.MaxValue)
                    return CreateLiteral((int)value, text);
                return CreateLiteral(value, text);
            }
        }

        Expression ParseRealLiteral()
        {
            ValidateToken(TokenId.RealLiteral);
            string text = _token.Text;
            object value = null;
            char last = text[text.Length - 1];
            if (last == 'F' || last == 'f')
            {
                float f;
                if (Single.TryParse(text.Substring(0, text.Length - 1), out f)) value = f;
            }
            else
            {
                double d;
                if (Double.TryParse(text, out d)) value = d;
            }
            if (value == null) throw ParseError(Res.InvalidRealLiteral, text);
            NextToken();
            return CreateLiteral(value, text);
        }

        Expression CreateLiteral(object value, string text)
        {
            ConstantExpression expr = Expression.Constant(value);
            _literals.Add(expr, text);
            return expr;
        }

        Expression ParseParenExpression()
        {
            ValidateToken(TokenId.OpenParen, Res.OpenParenExpected);
            NextToken();
            Expression e = ParseExpression();
            ValidateToken(TokenId.CloseParen, Res.CloseParenOrOperatorExpected);
            NextToken();
            return e;
        }

        Expression ParseIdentifier()
        {
            ValidateToken(TokenId.Identifier);
            object value;
            if (_keywords.TryGetValue(_token.Text, out value))
            {
                if (value is Type) return ParseTypeAccess((Type)value);
                if (value == (object)KeywordIt) return ParseIt();
                if (value == (object)KeywordIif) return ParseIif();
                if (value == (object)KeywordNew) return ParseNew();
                NextToken();
                return (Expression)value;
            }
            if (_symbols.TryGetValue(_token.Text, out value) ||
                _externals != null && _externals.TryGetValue(_token.Text, out value))
            {
                Expression expr = value as Expression;
                if (expr == null)
                {
                    expr = Expression.Constant(value);
                }
                else
                {
                    LambdaExpression lambda = expr as LambdaExpression;
                    if (lambda != null) return ParseLambdaInvocation(lambda);
                }
                NextToken();
                return expr;
            }
            if (_it != null) return ParseMemberAccess(null, _it);
            throw ParseError(Res.UnknownIdentifier, _token.Text);
        }

        Expression ParseIt()
        {
            if (_it == null)
                throw ParseError(Res.NoItInScope);
            NextToken();
            return _it;
        }

        Expression ParseIif()
        {
            int errorPos = _token.Pos;
            NextToken();
            var args = ParseArgumentList();
            if (args.Count != 3)
                throw ParseError(errorPos, Res.IifRequiresThreeArgs);
            return GenerateConditional(args[0], args[1], args[2], errorPos);
        }

        Expression GenerateConditional(Expression test, Expression expr1, Expression expr2, int errorPos)
        {
            if (test.Type != typeof(bool))
                throw ParseError(errorPos, Res.FirstExprMustBeBool);
            if (expr1.Type != expr2.Type)
            {
                Expression expr1as2 = expr2 != NullLiteral ? PromoteExpression(expr1, expr2.Type, true) : null;
                Expression expr2as1 = expr1 != NullLiteral ? PromoteExpression(expr2, expr1.Type, true) : null;
                if (expr1as2 != null && expr2as1 == null)
                {
                    expr1 = expr1as2;
                }
                else if (expr2as1 != null && expr1as2 == null)
                {
                    expr2 = expr2as1;
                }
                else
                {
                    string type1 = expr1 != NullLiteral ? expr1.Type.Name : "null";
                    string type2 = expr2 != NullLiteral ? expr2.Type.Name : "null";
                    if (expr1as2 != null && expr2as1 != null)
                        throw ParseError(errorPos, Res.BothTypesConvertToOther, type1, type2);
                    throw ParseError(errorPos, Res.NeitherTypeConvertsToOther, type1, type2);
                }
            }
            return Expression.Condition(test, expr1, expr2);
        }

        Expression ParseNew()
        {
            NextToken();
            ValidateToken(TokenId.OpenParen, Res.OpenParenExpected);
            NextToken();
            var properties = new List<DynamicProperty>();
            var expressions = new List<Expression>();
            while (true)
            {
                int exprPos = _token.Pos;
                Expression expr = ParseExpression();
                string propertyName;
                if (TokenIdentifierIs("as"))
                {
                    NextToken();
                    propertyName = GetIdentifier();
                    NextToken();
                }
                else
                {
                    MemberExpression me = expr as MemberExpression;
                    if (me == null) throw ParseError(exprPos, Res.MissingAsClause);
                    propertyName = me.Member.Name;
                }
                expressions.Add(expr);
                properties.Add(new DynamicProperty(propertyName, expr.Type));
                if (_token.Id != TokenId.Comma) break;
                NextToken();
            }
            ValidateToken(TokenId.CloseParen, Res.CloseParenOrCommaExpected);
            NextToken();
            Type type = DynamicTypeFactory.Instance.CreateType(properties.ToArray());
            MemberBinding[] bindings = new MemberBinding[properties.Count];
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i] = Expression.Bind(type.GetProperty(properties[i].Name), expressions[i]);
            }
            return Expression.MemberInit(Expression.New(type), bindings);
        }

        Expression ParseLambdaInvocation(LambdaExpression lambda)
        {
            var errorPos = _token.Pos;
            NextToken();
            var args = ParseArgumentList();
            MethodBase method;
            if (FindMethod(lambda.Type, "Invoke", false, args, out method) != 1)
                throw ParseError(errorPos, Res.ArgsIncompatibleWithLambda);
            return Expression.Invoke(lambda, args);
        }

        Expression ParseTypeAccess(Type type)
        {
            int errorPos = _token.Pos;
            NextToken();
            if (_token.Id == TokenId.Question)
            {
                if (!type.IsValueType || IsNullableType(type))
                    throw ParseError(errorPos, Res.TypeHasNoNullableForm, GetTypeName(type));
                type = typeof(Nullable<>).MakeGenericType(type);
                NextToken();
            }
            if (_token.Id == TokenId.OpenParen)
            {
                var args = ParseArgumentList();
                MethodBase method;
                switch (FindBestMethod(type.GetConstructors(), args, out method))
                {
                    case 0:
                        if (args.Count == 1)
                            return GenerateConversion(args[0], type, errorPos);
                        throw ParseError(errorPos, Res.NoMatchingConstructor, GetTypeName(type));
                    case 1:
                        return Expression.New((ConstructorInfo)method, args);
                    default:
                        throw ParseError(errorPos, Res.AmbiguousConstructorInvocation, GetTypeName(type));
                }
            }
            ValidateToken(TokenId.Dot, Res.DotOrOpenParenExpected);
            NextToken();
            return ParseMemberAccess(type, null);
        }

        Expression GenerateConversion(Expression expr, Type type, int errorPos)
        {
            Type exprType = expr.Type;
            if (exprType == type) return expr;
            if (exprType.IsValueType && type.IsValueType)
            {
                if ((IsNullableType(exprType) || IsNullableType(type)) &&
                    GetNonNullableType(exprType) == GetNonNullableType(type))
                    return Expression.Convert(expr, type);
                if ((IsNumericType(exprType) || IsEnumType(exprType)) &&
                    (IsNumericType(type)) || IsEnumType(type))
                    return Expression.ConvertChecked(expr, type);
            }
            if (exprType.IsAssignableFrom(type) || type.IsAssignableFrom(exprType) ||
                exprType.IsInterface || type.IsInterface)
                return Expression.Convert(expr, type);
            throw ParseError(errorPos, Res.CannotConvertValue,
                GetTypeName(exprType), GetTypeName(type));
        }

        Expression ParseMemberAccess(Type type, Expression instance)
        {
            if (instance != null) type = instance.Type;
            int errorPos = _token.Pos;
            string id = GetIdentifier();
            NextToken();
            if (_token.Id == TokenId.OpenParen)
            {
                if (instance != null && type != typeof(string))
                {
                    Type enumerableType = FindGenericType(typeof(IEnumerable<>), type);
                    if (enumerableType != null)
                    {
                        Type elementType = enumerableType.GetGenericArguments()[0];
                        var expression = ParseAggregate(instance, elementType, id, errorPos);
                        if (expression != null)
                            return expression;
                    }
                }
                var args = ParseArgumentList();
                MethodBase mb;
                switch (FindMethod(type, id, instance == null, args, out mb))
                {
                    case 0:
                        throw ParseError(errorPos, Res.NoApplicableMethod, id, GetTypeName(type));
                    case 1:
                        var method = (MethodInfo)mb;
                        //if (!IsPredefinedType(method.DeclaringType)) //now ok
                        //    throw ParseError(errorPos, Res.MethodsAreInaccessible, GetTypeName(method.DeclaringType));
                        //if (method.ReturnType == typeof(void)) //now ok
                        //    throw ParseError(errorPos, Res.MethodIsVoid, id, GetTypeName(method.DeclaringType));
                        return Expression.Call(instance, (MethodInfo)method, args);
                    default:
                        throw ParseError(errorPos, Res.AmbiguousMethodInvocation,
                            id, GetTypeName(type));
                }
            }
            else
            {
                var member = FindPropertyOrField(type, id, instance == null);
                if (member == null)
                    throw ParseError(errorPos, Res.UnknownPropertyOrField,
                        id, GetTypeName(type));
                return member is PropertyInfo ?
                    Expression.Property(instance, (PropertyInfo)member) :
                    Expression.Field(instance, (FieldInfo)member);
            }
        }

        static Type FindGenericType(Type generic, Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == generic) return type;
                if (generic.IsInterface)
                {
                    foreach (Type intfType in type.GetInterfaces())
                    {
                        Type found = FindGenericType(generic, intfType);
                        if (found != null) return found;
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

        Expression ParseAggregate(Expression instance, Type elementType, string methodName, int errorPos)
        {
            var outerIt = _it;
            var innerIt = Expression.Parameter(elementType, "");
            _it = innerIt;
            var position = _textPos;
            var token = _token;
            var args = ParseArgumentList();
            _it = outerIt;
            MethodBase signature;
            if (FindMethod(typeof(IEnumerableSignatures), methodName, false, args, out signature) != 1)
            {
                //throw ParseError(errorPos, Res.NoApplicableAggregate, methodName);
                SetTextPos(position);
                _token = token;
                return null;
            }
            Type[] typeArgs;
            if (signature.Name == "Min" || signature.Name == "Max")
            {
                typeArgs = new Type[] { elementType, args[0].Type };
            }
            else
            {
                typeArgs = new Type[] { elementType };
            }
            if (args.Count == 0)
            {
                args = new List<Expression> { instance };
            }
            else
            {
                args = new List<Expression> { instance, Expression.Lambda(args[0], innerIt) };
            }

            return Expression.Call(typeof(Enumerable), signature.Name, typeArgs, args.ToArray());
        }

        List<Expression> ParseArgumentList()
        {
            ValidateToken(TokenId.OpenParen, Res.OpenParenExpected);
            NextToken();
            var args = _token.Id != TokenId.CloseParen ? ParseArguments() : new List<Expression>();
            ValidateToken(TokenId.CloseParen, Res.CloseParenOrCommaExpected);
            NextToken();
            return args;
        }

        List<Expression> ParseArguments()
        {
            var args = new List<Expression>();
            while (true)
            {
                args.Add(ParseExpression());
                if (_token.Id != TokenId.Comma) break;
                NextToken();
            }
            return args;
        }

        Expression ParseElementAccess(Expression expr)
        {
            int errorPos = _token.Pos;
            ValidateToken(TokenId.OpenBracket, Res.OpenParenExpected);
            NextToken();
            var args = ParseArguments();
            ValidateToken(TokenId.CloseBracket, Res.CloseBracketOrCommaExpected);
            NextToken();
            if (expr.Type.IsArray)
            {
                if (expr.Type.GetArrayRank() != 1 || args.Count != 1)
                    throw ParseError(errorPos, Res.CannotIndexMultiDimArray);
                var index = PromoteExpression(args[0], typeof(int), true);
                if (index == null)
                    throw ParseError(errorPos, Res.InvalidIndex);
                return Expression.ArrayIndex(expr, index);
            }
            else
            {
                MethodBase mb;
                switch (FindIndexer(expr.Type, args, out mb))
                {
                    case 0:
                        throw ParseError(errorPos, Res.NoApplicableIndexer,
                            GetTypeName(expr.Type));
                    case 1:
                        return Expression.Call(expr, (MethodInfo)mb, args);
                    default:
                        throw ParseError(errorPos, Res.AmbiguousIndexerInvocation,
                            GetTypeName(expr.Type));
                }
            }
        }

        static bool IsPredefinedType(Type type)
        {
            foreach (Type t in PredefinedTypes) if (t == type) return true;
            return false;
        }

        static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        static string GetTypeName(Type type)
        {
            Type baseType = GetNonNullableType(type);
            string s = baseType.Name;
            if (type != baseType) s += '?';
            return s;
        }

        static bool IsNumericType(Type type)
        {
            return GetNumericTypeKind(type) != 0;
        }

        static bool IsSignedIntegralType(Type type)
        {
            return GetNumericTypeKind(type) == 2;
        }

        static bool IsUnsignedIntegralType(Type type)
        {
            return GetNumericTypeKind(type) == 3;
        }

        static int GetNumericTypeKind(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum) return 0;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return 1;
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return 2;
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return 3;
                default:
                    return 0;
            }
        }

        static bool IsEnumType(Type type)
        {
            return GetNonNullableType(type).IsEnum;
        }

        void CheckAndPromoteOperand(Type signatures, string opName, ref Expression expr, int errorPos)
        {
            var args = new List<Expression> { expr };
            MethodBase method;
            if (FindMethod(signatures, "F", false, args, out method) != 1)
                throw ParseError(errorPos, Res.IncompatibleOperand,
                    opName, GetTypeName(args[0].Type));
            expr = args[0];
        }

        void CheckAndPromoteOperands(Type signatures, string opName, ref Expression left, ref Expression right, int errorPos)
        {
            var args = new List<Expression> { left, right };
            MethodBase method;
            if (FindMethod(signatures, "F", false, args, out method) != 1)
                throw IncompatibleOperandsError(opName, left, right, errorPos);
            left = args[0];
            right = args[1];
        }

        Exception IncompatibleOperandsError(string opName, Expression left, Expression right, int pos)
        {
            return ParseError(pos, Res.IncompatibleOperands,
                opName, GetTypeName(left.Type), GetTypeName(right.Type));
        }

        MemberInfo FindPropertyOrField(Type type, string memberName, bool staticAccess)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly |
                (staticAccess ? BindingFlags.Static : BindingFlags.Instance);
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.FindMembers(MemberTypes.Property | MemberTypes.Field,
                    flags, Type.FilterNameIgnoreCase, memberName);
                if (members.Length != 0) return members[0];
            }
            return null;
        }

        int FindMethod(Type type, string methodName, bool staticAccess, List<Expression> args, out MethodBase method)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly |
                (staticAccess ? BindingFlags.Static : BindingFlags.Instance); //BindingFlags.Instance | BindingFlags.Static
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.FindMembers(MemberTypes.Method,
                    flags, Type.FilterNameIgnoreCase, methodName);
                int count = FindBestMethod(members.Cast<MethodBase>(), args, out method);
                if (count != 0) return count;
            }
            method = null;
            return 0;
        }

        int FindIndexer(Type type, List<Expression> args, out MethodBase method)
        {
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.GetDefaultMembers();
                if (members.Length != 0)
                {
                    IEnumerable<MethodBase> methods = members.
                        OfType<PropertyInfo>().
                        Select(p => (MethodBase)p.GetGetMethod()).
                        Where(m => m != null);
                    int count = FindBestMethod(methods, args, out method);
                    if (count != 0) return count;
                }
            }
            method = null;
            return 0;
        }

        static IEnumerable<Type> SelfAndBaseTypes(Type type)
        {
            if (type.IsInterface)
            {
                List<Type> types = new List<Type>();
                AddInterface(types, type);
                return types;
            }
            return SelfAndBaseClasses(type);
        }

        static IEnumerable<Type> SelfAndBaseClasses(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        static void AddInterface(List<Type> types, Type type)
        {
            if (!types.Contains(type))
            {
                types.Add(type);
                foreach (Type t in type.GetInterfaces()) AddInterface(types, t);
            }
        }

        class MethodData
        {
            public MethodBase MethodBase;
            public ParameterInfo[] Parameters;
            public Expression[] Args;
        }

        int FindBestMethod(IEnumerable<MethodBase> methods, List<Expression> args, out MethodBase method)
        {
            var applicable = methods.
                Select(m => new MethodData { MethodBase = m, Parameters = m.GetParameters() }).
                Where(m => IsApplicable(m, args)).
                ToArray();
            if (applicable.Length > 1)
            {
                applicable = applicable.
                    Where(m => applicable.All(n => m == n || IsBetterThan(args, m, n))).
                    ToArray();
            }
            if (applicable.Length == 1)
            {
                MethodData md = applicable[0];
                for (int i = 0; i < args.Count; i++)
                {
                    if (i >= md.Args.Length)
                    {
                        args.RemoveRange(i, args.Count - md.Args.Length);
                    }
                    else
                    {
                        args[i] = md.Args[i];
                    }
                }
                method = md.MethodBase;
            }
            else
            {
                method = null;
            }
            return applicable.Length;
        }

        bool IsApplicable(MethodData method, List<Expression> args)
        {
            //if (method.Parameters.Length != args.Count)
            //{
            //    return false;
            //}
            //if (method.Parameters.Length < args.Count)
            //{
            //    return false;
            //}
            if (args.Count > 0 && method.Parameters.Length == 0)
            {
                return false;
            }
            var promotedArgs = new Expression[method.Parameters.Length];
            for (var i = 0; i < method.Parameters.Length; i++) //args.Count
            {
                //if (i >= method.Parameters.Length)
                //{
                //    return false;
                //}
                var pi = method.Parameters[i];
                if (pi.IsOut)
                {
                    return false;
                }
                Expression promoted;
                if (pi.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                {
                    var promotedParamsArgs = new Expression[args.Count - method.Parameters.Length + 1];
                    var elementType = pi.ParameterType.GetElementType();
                    int j = 0;
                    for (var k = i; k < args.Count; k++)
                    {
                        promoted = PromoteExpression(args[k], elementType, false);
                        if (promoted == null)
                        {
                            return false;
                        }
                        promotedParamsArgs[j++] = promoted;
                    }
                    promotedArgs[i] = Expression.NewArrayInit(elementType, promotedParamsArgs);
                    break;
                }
                if (i >= args.Count)
                {
                    if (pi.IsOptional)
                    {
                        //promoted = PromoteExpression(Expression.Constant(pi.DefaultValue), pi.ParameterType, false);
                        promoted = PromoteExpression(Expression.Convert(Expression.Constant(pi.DefaultValue), pi.ParameterType), pi.ParameterType, false);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    promoted = PromoteExpression(args[i], pi.ParameterType, false);
                }
                if (promoted == null)
                {
                    return false;
                }
                promotedArgs[i] = promoted;
            }
            method.Args = promotedArgs;
            return true;
        }

        Expression PromoteExpression(Expression expr, Type type, bool exact)
        {
            if (expr.Type == type) return expr;
            if (expr is ConstantExpression)
            {
                ConstantExpression ce = (ConstantExpression)expr;
                if (ce == NullLiteral)
                {
                    if (!type.IsValueType || IsNullableType(type))
                        return Expression.Constant(null, type);
                }
                else
                {
                    string text;
                    if (_literals.TryGetValue(ce, out text))
                    {
                        Type target = GetNonNullableType(type);
                        Object value = null;
                        switch (Type.GetTypeCode(ce.Type))
                        {
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                                value = ParseNumber(text, target);
                                break;
                            case TypeCode.Double:
                                if (target == typeof(decimal)) value = ParseNumber(text, target);
                                break;
                            case TypeCode.String:
                                value = ParseEnum(text, target);
                                break;
                        }
                        if (value != null)
                            return Expression.Constant(value, type);
                    }
                }
            }
            if (IsCompatibleWith(expr.Type, type))
            {
                if (expr.Type == typeof(void))
                {
                    return null;
                }
                else //if (type.IsValueType || exact)
                {
                    return Expression.Convert(expr, type);
                }
                //else
                //{
                //    return expr;
                //}
            }
            return null;
        }

        static object ParseNumber(string text, Type type)
        {
            switch (Type.GetTypeCode(GetNonNullableType(type)))
            {
                case TypeCode.SByte:
                    sbyte sb;
                    if (sbyte.TryParse(text, out sb)) return sb;
                    break;
                case TypeCode.Byte:
                    byte b;
                    if (byte.TryParse(text, out b)) return b;
                    break;
                case TypeCode.Int16:
                    short s;
                    if (short.TryParse(text, out s)) return s;
                    break;
                case TypeCode.UInt16:
                    ushort us;
                    if (ushort.TryParse(text, out us)) return us;
                    break;
                case TypeCode.Int32:
                    int i;
                    if (int.TryParse(text, out i)) return i;
                    break;
                case TypeCode.UInt32:
                    uint ui;
                    if (uint.TryParse(text, out ui)) return ui;
                    break;
                case TypeCode.Int64:
                    long l;
                    if (long.TryParse(text, out l)) return l;
                    break;
                case TypeCode.UInt64:
                    ulong ul;
                    if (ulong.TryParse(text, out ul)) return ul;
                    break;
                case TypeCode.Single:
                    float f;
                    if (float.TryParse(text, out f)) return f;
                    break;
                case TypeCode.Double:
                    double d;
                    if (double.TryParse(text, out d)) return d;
                    break;
                case TypeCode.Decimal:
                    decimal e;
                    if (decimal.TryParse(text, out e)) return e;
                    break;
            }
            return null;
        }

        static object ParseEnum(string name, Type type)
        {
            if (type.IsEnum)
            {
                MemberInfo[] memberInfos = type.FindMembers(MemberTypes.Field,
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static,
                    Type.FilterNameIgnoreCase, name);
                if (memberInfos.Length != 0) return ((FieldInfo)memberInfos[0]).GetValue(null);
            }
            return null;
        }

        static bool IsCompatibleWith(Type source, Type target)
        {
            if (source == target) return true;
            if (!target.IsValueType) return target.IsAssignableFrom(source);
            Type st = GetNonNullableType(source);
            Type tt = GetNonNullableType(target);
            if (st != source && tt == target) return false;
            TypeCode sc = st.IsEnum ? TypeCode.Object : Type.GetTypeCode(st);
            TypeCode tc = tt.IsEnum ? TypeCode.Object : Type.GetTypeCode(tt);
            switch (sc)
            {
                case TypeCode.SByte:
                    switch (tc)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Byte:
                    switch (tc)
                    {
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int16:
                    switch (tc)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt16:
                    switch (tc)
                    {
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int32:
                    switch (tc)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt32:
                    switch (tc)
                    {
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int64:
                    switch (tc)
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt64:
                    switch (tc)
                    {
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Single:
                    switch (tc)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                    }
                    break;
                default:
                    if (st == tt) return true;
                    break;
            }
            return false;
        }

        static bool IsBetterThan(List<Expression> args, MethodData m1, MethodData m2)
        {
            bool better = false;
            for (int i = 0; i < args.Count; i++)
            {
                int c = CompareConversions(args[i].Type,
                    m1.Parameters[i].ParameterType,
                    m2.Parameters[i].ParameterType);
                if (c < 0) return false;
                if (c > 0) better = true;
            }
            return better;
        }

        // Return 1 if s -> t1 is a better conversion than s -> t2
        // Return -1 if s -> t2 is a better conversion than s -> t1
        // Return 0 if neither conversion is better
        static int CompareConversions(Type s, Type t1, Type t2)
        {
            if (t1 == t2) return 0;
            if (s == t1) return 1;
            if (s == t2) return -1;
            bool t1t2 = IsCompatibleWith(t1, t2);
            bool t2t1 = IsCompatibleWith(t2, t1);
            if (t1t2 && !t2t1) return 1;
            if (t2t1 && !t1t2) return -1;
            if (IsSignedIntegralType(t1) && IsUnsignedIntegralType(t2)) return 1;
            if (IsSignedIntegralType(t2) && IsUnsignedIntegralType(t1)) return -1;
            return 0;
        }

        private static void RequiresCanRead(Expression expression, string paramName)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(paramName);
            }
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        var memberExpression = (MemberExpression)expression;
                        var member = memberExpression.Member;
                        if (member.MemberType == MemberTypes.Property)
                        {
                            var pi = (PropertyInfo)member;
                            if (!pi.CanRead)
                            {
                                throw new ArgumentException("ExpressionMustBeReadable", paramName);
                            }
                        }
                        break;
                    }
                case ExpressionType.Index:
                    {
                        IndexExpression indexExpression = (IndexExpression)expression;
                        if ((indexExpression.Indexer != null) && !indexExpression.Indexer.CanRead)
                        {
                            throw new ArgumentException("ExpressionMustBeReadable", paramName);
                        }
                        break;
                    }
            }
        }

        private static void RequiresCanWrite(Expression expression, string paramName)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(paramName);
            }
            bool canWrite = false;
            ExpressionType nodeType = expression.NodeType;
            switch (nodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        MemberExpression expression3 = (MemberExpression)expression;
                        switch (expression3.Member.MemberType)
                        {
                            case MemberTypes.Field:
                                {
                                    FieldInfo fi = (FieldInfo)expression3.Member;
                                    canWrite = !fi.IsInitOnly && !fi.IsLiteral;
                                    break;
                                }
                            case MemberTypes.Property:
                                {
                                    PropertyInfo pi = (PropertyInfo)expression3.Member;
                                    canWrite = pi.CanWrite;
                                    break;
                                }
                        }
                        throw new ArgumentException("ExpressionMustBeWriteable", paramName);
                    }
                case ExpressionType.Parameter:
                    canWrite = true;
                    throw new ArgumentException("ExpressionMustBeWriteable", paramName);
            }

            if (nodeType == ExpressionType.Index)
            {
                IndexExpression indexExpression = (IndexExpression)expression;
                if (indexExpression.Indexer != null)
                {
                    canWrite = indexExpression.Indexer.CanWrite;
                }
                else
                {
                    canWrite = true;
                }
            }

        }

        public static Expression GenerateAssign(Expression left, Expression right)
        {
            UnaryExpression unaryExpression = left as UnaryExpression;
            try
            {
                //if (left.NodeType == ExpressionType.Convert)
                if (unaryExpression != null)
                {
                    return Expression.Assign(unaryExpression.Operand, Expression.Convert(right, unaryExpression.Operand.Type));
                }
                else
                {
                    return Expression.Assign(left, Expression.Convert(right, left.Type));
                }
            }
            catch
            {
                throw new ReflectionException(Res.WriteableExpressionExpected);
            }

        }

        Expression GenerateEqual(Expression left, Expression right)
        {
            return Expression.Equal(left, right);
        }

        Expression GenerateNotEqual(Expression left, Expression right)
        {
            return Expression.NotEqual(left, right);
        }

        Expression GenerateGreaterThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThan(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.GreaterThan(left, right);
        }

        Expression GenerateGreaterThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThanOrEqual(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.GreaterThanOrEqual(left, right);
        }

        Expression GenerateLessThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThan(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.LessThan(left, right);
        }

        Expression GenerateLessThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThanOrEqual(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.LessThanOrEqual(left, right);
        }

        Expression GenerateAdd(Expression left, Expression right)
        {
            if (left.Type == typeof(string) && right.Type == typeof(string))
            {
                return GenerateStaticMethodCall("Concat", left, right);
            }
            return Expression.Add(left, right);
        }

        Expression GenerateSubtract(Expression left, Expression right)
        {
            return Expression.Subtract(left, right);
        }

        Expression GenerateStringConcat(Expression left, Expression right)
        {
            return Expression.Call(
                null,
                typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) }),
                new[] { left, right });
        }

        MethodInfo GetStaticMethod(string methodName, Expression left, Expression right)
        {
            return left.Type.GetMethod(methodName, new[] { left.Type, right.Type });
        }

        Expression GenerateStaticMethodCall(string methodName, Expression left, Expression right)
        {
            return Expression.Call(null, GetStaticMethod(methodName, left, right), new[] { left, right });
        }

        void SetTextPos(int pos)
        {
            _textPos = pos;
            _ch = _textPos < _textLen ? _text[_textPos] : '\0';
        }

        void NextChar()
        {
            if (_textPos < _textLen) _textPos++;
            _ch = _textPos < _textLen ? _text[_textPos] : '\0';
        }

        void NextToken()
        {
            while (Char.IsWhiteSpace(_ch)) NextChar();
            TokenId t;
            int tokenPos = _textPos;
            switch (_ch)
            {
                case '!':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.ExclamationEqual;
                    }
                    else
                    {
                        t = TokenId.Exclamation;
                    }
                    break;
                case '%':
                    NextChar();
                    t = TokenId.Percent;
                    break;
                case '&':
                    NextChar();
                    if (_ch == '&')
                    {
                        NextChar();
                        t = TokenId.DoubleAmphersand;
                    }
                    else
                    {
                        t = TokenId.Amphersand;
                    }
                    break;
                case '(':
                    NextChar();
                    t = TokenId.OpenParen;
                    break;
                case ')':
                    NextChar();
                    t = TokenId.CloseParen;
                    break;
                case '*':
                    NextChar();
                    t = TokenId.Asterisk;
                    break;
                case '+':
                    NextChar();
                    t = TokenId.Plus;
                    break;
                case ',':
                    NextChar();
                    t = TokenId.Comma;
                    break;
                case '-':
                    NextChar();
                    t = TokenId.Minus;
                    break;
                case '.':
                    NextChar();
                    t = TokenId.Dot;
                    break;
                case '/':
                    NextChar();
                    t = TokenId.Slash;
                    break;
                case ':':
                    NextChar();
                    t = TokenId.Colon;
                    break;
                case '<':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.LessThanEqual;
                    }
                    else if (_ch == '>')
                    {
                        NextChar();
                        t = TokenId.LessGreater;
                    }
                    else
                    {
                        t = TokenId.LessThan;
                    }
                    break;
                case '=':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.DoubleEqual;
                    }
                    else
                    {
                        t = TokenId.Equal;
                    }
                    break;
                case '>':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.GreaterThanEqual;
                    }
                    else
                    {
                        t = TokenId.GreaterThan;
                    }
                    break;
                case '?':
                    NextChar();
                    t = TokenId.Question;
                    break;
                case '[':
                    NextChar();
                    t = TokenId.OpenBracket;
                    break;
                case ']':
                    NextChar();
                    t = TokenId.CloseBracket;
                    break;
                case '|':
                    NextChar();
                    if (_ch == '|')
                    {
                        NextChar();
                        t = TokenId.DoubleBar;
                    }
                    else
                    {
                        t = TokenId.Bar;
                    }
                    break;
                case '"':
                case '\'':
                    char quote = _ch;
                    do
                    {
                        NextChar();
                        while (_textPos < _textLen && _ch != quote) NextChar();
                        if (_textPos == _textLen)
                            throw ParseError(_textPos, Res.UnterminatedStringLiteral);
                        NextChar();
                    } while (_ch == quote);
                    t = TokenId.StringLiteral;
                    break;
                default:
                    if (Char.IsLetter(_ch) || _ch == '@' || _ch == '_')
                    {
                        do
                        {
                            NextChar();
                        } while (Char.IsLetterOrDigit(_ch) || _ch == '_');
                        t = TokenId.Identifier;
                        break;
                    }
                    if (Char.IsDigit(_ch))
                    {
                        t = TokenId.IntegerLiteral;
                        do
                        {
                            NextChar();
                        } while (Char.IsDigit(_ch));
                        if (_ch == '.')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                            ValidateDigit();
                            do
                            {
                                NextChar();
                            } while (Char.IsDigit(_ch));
                        }
                        if (_ch == 'E' || _ch == 'e')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                            if (_ch == '+' || _ch == '-') NextChar();
                            ValidateDigit();
                            do
                            {
                                NextChar();
                            } while (Char.IsDigit(_ch));
                        }
                        if (_ch == 'F' || _ch == 'f') NextChar();
                        break;
                    }
                    if (_textPos == _textLen)
                    {
                        t = TokenId.End;
                        break;
                    }
                    throw ParseError(_textPos, Res.InvalidCharacter, _ch);
            }
            _token.Id = t;
            _token.Text = _text.Substring(tokenPos, _textPos - tokenPos);
            _token.Pos = tokenPos;
        }

        bool TokenIdentifierIs(string id)
        {
            return _token.Id == TokenId.Identifier && String.Equals(id, _token.Text, StringComparison.OrdinalIgnoreCase);
        }

        string GetIdentifier()
        {
            ValidateToken(TokenId.Identifier, Res.IdentifierExpected);
            var id = _token.Text;
            if (id.Length > 1 && id[0] == '@') id = id.Substring(1);
            return id;
        }

        void ValidateDigit()
        {
            if (!Char.IsDigit(_ch)) throw ParseError(_textPos, Res.DigitExpected);
        }

        void ValidateToken(TokenId t, string errorMessage)
        {
            if (_token.Id != t) throw ParseError(errorMessage);
        }

        void ValidateToken(TokenId t)
        {
            if (_token.Id != t) throw ParseError(Res.SyntaxError);
        }

        Exception ParseError(string format, params object[] args)
        {
            return ParseError(_token.Pos, format, args);
        }

        Exception ParseError(int pos, string format, params object[] args)
        {
            return new ParseException(string.Format(System.Globalization.CultureInfo.CurrentCulture, format, args), pos);
        }

        static Dictionary<string, object> CreateKeywords()
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "true", TrueLiteral }, { "false", FalseLiteral }, { "null", NullLiteral }, { KeywordIt, KeywordIt }, { KeywordIif, KeywordIif }, { KeywordNew, KeywordNew } };
            foreach (var type in PredefinedTypes) d.Add(type.Name, type);
            return d;
        }
    }
}
