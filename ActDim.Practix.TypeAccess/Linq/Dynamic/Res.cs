namespace ActDim.Practix.TypeAccess.Linq.Dynamic
{
	public static class Res
	{
		public const string DuplicateIdentifier = "The identifier '{0}' was defined more than once";
		public const string ExpressionTypeMismatch = "Expression of targetType '{0}' expected";
		public const string ExpressionExpected = "Expression expected";
		public const string InvalidCharacterLiteral = "Character literal must contain exactly one character";
		public const string InvalidIntegerLiteral = "Invalid integer literal '{0}'";
		public const string InvalidRealLiteral = "Invalid real literal '{0}'";
		public const string UnknownIdentifier = "Unknown identifier '{0}'";
		public const string NoItInScope = "No 'it' is in scope";
		public const string IifRequiresThreeArgs = "The 'iif' function requires three arguments";
		public const string FirstExprMustBeBool = "The first expression must be of targetType 'Boolean'";
		public const string BothTypesConvertToOther = "Both of the types '{0}' and '{1}' convert to the other";
		public const string NeitherTypeConvertsToOther = "Neither of the types '{0}' and '{1}' converts to the other";
		public const string MissingAsClause = "Expression is missing an 'as' clause";
		public const string ArgsIncompatibleWithLambda = "Argument list incompatible with lambda expression";
		public const string TypeHasNoNullableForm = "Type '{0}' has no nullable form";
		public const string NoMatchingConstructor = "No matching constructor in targetType '{0}'";
		public const string AmbiguousConstructorInvocation = "Ambiguous invocation of '{0}' constructor";
		public const string CannotConvertValue = "A value of targetType '{0}' cannot be converted to targetType '{1}'";
		public const string NoApplicableMethod = "No applicable method '{0}' exists in targetType '{1}'";
		public const string MethodsAreInaccessible = "Methods on targetType '{0}' are not accessible";
		public const string MethodIsVoid = "Method '{0}' in targetType '{1}' does not return a value";
		public const string AmbiguousMethodInvocation = "Ambiguous invocation of method '{0}' in targetType '{1}'";
		public const string UnknownPropertyOrField = "No property or field '{0}' exists in targetType '{1}'";
		public const string NoApplicableAggregate = "No applicable aggregate method '{0}' exists";
		public const string CannotIndexMultiDimArray = "Indexing of multi-dimensional arrays is not supported";
		public const string InvalidIndex = "Array index must be an integer expression";
		public const string NoApplicableIndexer = "No applicable indexer exists in targetType '{0}'";
		public const string AmbiguousIndexerInvocation = "Ambiguous invocation of indexer in targetType '{0}'";
		public const string IncompatibleOperand = "Operator '{0}' incompatible with operand targetType '{1}'";
		public const string IncompatibleOperands = "Operator '{0}' incompatible with operand types '{1}' and '{2}'";
		public const string UnterminatedStringLiteral = "Unterminated string literal";
		public const string InvalidCharacter = "Syntax error '{0}'";
		public const string DigitExpected = "Digit expected";
		public const string SyntaxError = "Syntax error";
		public const string TokenExpected = "{0} expected";
		public const string ParseExceptionFormat = "{0} (at index {1})";
		public const string ColonExpected = "':' expected";
		public const string OpenParenExpected = "'(' expected";
		public const string CloseParenOrOperatorExpected = "')' or operator expected";
		public const string CloseParenOrCommaExpected = "')' or ',' expected";
		public const string DotOrOpenParenExpected = "'.' or '(' expected";
		public const string OpenBracketExpected = "'[' expected";
		public const string CloseBracketOrCommaExpected = "']' or ',' expected";
		public const string IdentifierExpected = "Identifier expected";
		//"Left side of assignment operator must be a writeable member (access) expression: (public) field or property with (public) setter accessor"
		public const string WriteableExpressionExpected = "Writeable expression expected on the left side of assignment operator"; //left-hand part
	}
}
