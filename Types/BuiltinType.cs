using DVG.GLSH.Generator.Members;
using System.Collections.Generic;

namespace DVG.GLSH.Generator.Types
{
    internal class BuiltinType : AbstractType
    {
        public static IEnumerable<BuiltinType> BaseTypes
        {
            get
            {
                yield return TypeInt;
                yield return TypeUint;
                yield return TypeFloat;
                yield return TypeBool;
                yield return TypeDouble;

                if (GenerateHalfs)
                    yield return TypeHalf;
                if (GenerateDecimals)
                    yield return TypeDecimal;
                if (GenerateLongs)
                    yield return TypeLong;
            }
        }

        public static List<KeyValuePair<BuiltinType, BuiltinType>> Upcasts
        {
            get
            {
                // from -> to
                var dic = new List<KeyValuePair<BuiltinType, BuiltinType>>
                {
                    new KeyValuePair<BuiltinType, BuiltinType>(TypeInt, TypeUint),
                    new KeyValuePair<BuiltinType, BuiltinType>(TypeInt, TypeFloat),
                    new KeyValuePair<BuiltinType, BuiltinType>(TypeInt, TypeDouble),

                    new KeyValuePair<BuiltinType, BuiltinType>(TypeUint, TypeFloat),
                    new KeyValuePair<BuiltinType, BuiltinType>(TypeUint, TypeDouble),

                    new KeyValuePair<BuiltinType, BuiltinType>(TypeFloat, TypeDouble),

                };
                if (GenerateLongs)
                {
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeInt, TypeLong));
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeUint, TypeLong));
                }
                if (GenerateHalfs)
                {
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeInt, TypeHalf));
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeUint, TypeHalf));
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeHalf, TypeFloat));
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeHalf, TypeDouble));
                }
                if (GenerateDecimals)
                {
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeInt, TypeDecimal));
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeUint, TypeDecimal));
                }
                if (GenerateDecimals && GenerateLongs)
                    dic.Add(new KeyValuePair<BuiltinType, BuiltinType>(TypeLong, TypeDecimal));
                return dic;
            }
        }

        public static readonly BuiltinType TypeInt = new BuiltinType
        {
            TypeName = "int",
            Prefix = "i",
            TypeConstants = new[] { "MaxValue", "MinValue" }
        };
        public static readonly BuiltinType TypeUint = new BuiltinType
        {
            TypeName = "uint",
            Prefix = "u",
            OneValueConstant = "1u",
            ZeroValueConstant = "0u",
            TypeConstants = new[] { "MaxValue", "MinValue" }
        };
        public static readonly BuiltinType TypeHalf = new BuiltinType
        {
            TypeName = "Half",
            Prefix = "h",
            OneValueConstant = "Half.One",
            ZeroValueConstant = "Half.Zero",
            TypeConstants = new[] { "MaxValue", "MinValue", "Epsilon", "NaN", "NegativeInfinity", "PositiveInfinity" },
        };
        public static readonly BuiltinType TypeFloat = new BuiltinType
        {
            TypeName = "float",
            OneValueConstant = "1f",
            ZeroValueConstant = "0f",
            TypeConstants = new[] { "MaxValue", "MinValue", "Epsilon", "NaN", "NegativeInfinity", "PositiveInfinity" },
        };
        public static readonly BuiltinType TypeDouble = new BuiltinType
        {
            TypeName = "double",
            Prefix = "d",
            OneValueConstant = "1.0",
            ZeroValueConstant = "0.0",
            TypeConstants = new[] { "MaxValue", "MinValue", "Epsilon", "NaN", "NegativeInfinity", "PositiveInfinity" },
        };
        public static readonly BuiltinType TypeDecimal = new BuiltinType
        {
            TypeName = "decimal",
            Prefix = "dec",
            OneValueConstant = "1m",
            ZeroValueConstant = "0m",
            TypeConstants = new[] { "MaxValue", "MinValue", "MinusOne" }
        };
        public static readonly BuiltinType TypeLong = new BuiltinType
        {
            TypeName = "long",
            Prefix = "l",
            TypeConstants = new[] { "MaxValue", "MinValue" }
        };
        public static readonly BuiltinType TypeBool = new BuiltinType
        {
            TypeName = "bool",
            Prefix = "b",
            HasArithmetics = false,
            OneValueConstant = "true",
            ZeroValueConstant = "false",
            IsBool = true,
        };

        public string TypeName { get; set; }
        public string Prefix { get; set; }

        public bool HasArithmetics { get; set; } = true;

        public string EqualFormat { get; set; } = "{0} == {1}";
        public string NotEqualFormat { get; set; } = "{0} != {1}";

        public bool IsBool { get; set; }

        public string OneValueConstant { get; set; } = "1";
        public string ZeroValueConstant { get; set; } = "0";

        public override string OneValue => OneValueConstant;
        public override string ZeroValue => ZeroValueConstant;

        public override string Name => TypeName;

        public override string TypeComment => "Builtin " + Name;

        public override IEnumerable<Member> GenerateMembers()
        {
            yield break; // no-op
        }

        public string[] TypeConstants { get; set; } = new string[] { };
    }
}
