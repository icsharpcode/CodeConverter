namespace ICSharpCode.CodeConverter.Util.FromRoslyn;
#pragma warning disable CA1069 // Enums values should not be duplicated

/// <summary>
/// Converted from https://github.com/dotnet/roslyn/blob/85f155be47147a702305c8f49c64eaf51a53d734/src/Compilers/VisualBasic/Portable/Semantics/Conversions.vb#L282
/// </summary>
[Flags]
internal enum VbConversionKind
{
    // If there is a conversion, either [Widening] or [Narrowing] bit must be set, but not both.
    // All VB conversions are either Widening or Narrowing.
    // 
    // To indicate the fact that no conversion exists:
    // 1) Neither [Widening] nor [Narrowing] are set.
    // 2) Additional flags may be set in order to provide specific reason.
    // 
    // Bits from the following values are never set at the same time :
    // Identity, Numeric, Nullable, Reference, Array, TypeParameter, Value, [String], WideningNothingLiteral, InterpolatedString

    FailedDueToNumericOverflow = 1 << 31, // Failure flag
    FailedDueToIntegerOverflow = FailedDueToNumericOverflow | 1 << 30, // Failure flag
    FailedDueToNumericOverflowMask = FailedDueToNumericOverflow | FailedDueToIntegerOverflow,
    FailedDueToQueryLambdaBodyMismatch = 1 << 29, // Failure flag to indicate that conversion failed because body of a query lambda couldn't be converted to the target delegate return type.
    FailedDueToArrayLiteralElementConversion = 1 << 28, // Failed because array literal element could not be converted to the target element type.

    // If there is a conversion, one and only one of the following two bits must be set.
    // All VB conversions are either Widening or Narrowing.
    Widening = 1 << 0,
    Narrowing = 1 << 1,

    /// <summary>
    /// Because flags can be combined, use the method IsIdentityConversion when testing for ConversionKind.Identity
    /// </summary>
    Identity = Widening | 1 << 2, // According to VB spec, identity conversion is Widening
    Numeric = 1 << 3,
    WideningNumeric = Widening | Numeric,
    NarrowingNumeric = Narrowing | Numeric,

    /// <summary>
    /// Can be combined with <see cref="VbConversionKind.Tuple"/> to indicate that the underlying value conversion is a predefined tuple conversion
    /// </summary>
    Nullable = 1 << 4,
    WideningNullable = Widening | Nullable,
    NarrowingNullable = Narrowing | Nullable,
    Reference = 1 << 5,
    WideningReference = Widening | Reference,
    NarrowingReference = Narrowing | Reference,
    Array = 1 << 6,
    WideningArray = Widening | Array,
    NarrowingArray = Narrowing | Array,
    TypeParameter = 1 << 7,
    WideningTypeParameter = Widening | TypeParameter,
    NarrowingTypeParameter = Narrowing | TypeParameter,
    Value = 1 << 8,
    WideningValue = Widening | Value,
    NarrowingValue = Narrowing | Value,
    String = 1 << 9,
    WideningString = Widening | String,
    NarrowingString = Narrowing | String,
    Boolean = 1 << 10,
    // Note: there are no widening boolean conversions.
    NarrowingBoolean = Narrowing | Boolean,
    WideningNothingLiteral = Widening | 1 << 11,

    // Compiler might be interested in knowing if constant numeric conversion involves narrowing
    // for constant's original type. When user-defined conversions are involved, this flag can be
    // combined with widening conversions other than WideningNumericConstant.
    // 
    // If this flag is combined with Narrowing, there should be no other reasons to treat
    // conversion as narrowing. In some scenarios overload resolution is likely to dismiss
    // narrowing in presence of this flag. Also, it appears that with Option Strict On, Dev10
    // compiler does not report errors for narrowing conversions from an integral constant
    // expression to an integral type (assuming integer overflow checks are disabled) or from a
    // floating constant to a floating type.
    InvolvesNarrowingFromNumericConstant = 1 << 12,

    // This flag is set when conversion involves conversion enum <-> underlying type,
    // or conversion between two enums, etc 
    InvolvesEnumTypeConversions = 1 << 13,

    // Lambda conversion
    Lambda = 1 << 14,

    // Delegate relaxation levels for Lambda and Delegate conversions
    DelegateRelaxationLevelNone = 0, // Identity / Whidbey
    DelegateRelaxationLevelWidening = 1 << 15,
    DelegateRelaxationLevelWideningDropReturnOrArgs = 2 << 15,
    DelegateRelaxationLevelWideningToNonLambda = 3 << 15,
    DelegateRelaxationLevelNarrowing = 4 << 15,  // OrcasStrictOff
    DelegateRelaxationLevelInvalid = 5 << 15, // Keep invalid the biggest number
    DelegateRelaxationLevelMask = 7 << 15, // Three bits used!

    // Can be combined with Narrowing
    VarianceConversionAmbiguity = 1 << 18,

    // This bit can be combined with NoConversion to indicate that, even though there is no conversion
    // from the language point of view, there is a slight chance that conversion might succeed at run-time
    // under the right circumstances. It is used to detect possibly ambiguous variance conversions to an
    // interface, so it is set only for scenarios that are relevant to variance conversions to an
    // interface. 
    MightSucceedAtRuntime = 1 << 19,
    AnonymousDelegate = 1 << 20,
    NeedAStub = 1 << 21,
    ConvertedToExpressionTree = 1 << 22,   // Combined with Lambda, indicates a conversion of lambda to Expression(Of T).
    UserDefined = 1 << 23,

    // Some variance delegate conversions are treated as special narrowing (Dev10 #820752).
    // This flag is combined with Narrowing to indicate the fact.
    NarrowingDueToContraVarianceInDelegate = 1 << 24,

    // Interpolated string conversions
    InterpolatedString = Widening | 1 << 25,

    // Tuple conversions
    /// <summary>
    /// Can be combined with <see cref="VbConversionKind.Nullable"/> to indicate that the underlying value conversion is a predefined tuple conversion
    /// </summary>
    Tuple = 1 << 26,
    WideningTuple = Widening | Tuple,
    NarrowingTuple = Narrowing | Tuple,
    WideningNullableTuple = WideningNullable | Tuple,
    NarrowingNullableTuple = NarrowingNullable | Tuple
}