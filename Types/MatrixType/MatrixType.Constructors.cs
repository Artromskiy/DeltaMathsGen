using KibiHex.MathsGen.Members;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Types
{
    internal partial class MatrixType
    {
        /// <summary>
        /// 5 Operators and Expressions.
        /// 5.4.2 Vector and Matrix Constructors.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> Constructors()
        {
            // Constructors

            yield return new Constructor(this, Fields)
            {
                Parameters = new string[] { $"{BaseTypeName} s" },
                Code = Fields.Select(f => $"this{f} = " + (IsDiagonal(f) ? "s;" : "0;")).ToArray(),
                Comment = string.Format("Constructs diagonal matrix with scalar, non diagonal values are set to zero.")
            };
            var args = FieldsNames.ToArray();
            yield return new Constructor(this, Fields)
            {
                Parameters = args.TypedArgs(BaseType),
                Code = Enumerable.Range(0, FieldCount).Select(i => $"this{FieldFor(i)} = {args[i]};").ToArray(),
                Initializers = Fields.ToArray(),
                Comment = "Component-wise constructor"
            };

            // for mat4 count of unique vector/scalar constructors is 20569!!!
            var vecType = new VectorType(BaseType, Rows);
            yield return new Constructor(this, Fields)
            {
                Parameters = Enumerable.Range(0, Columns).Select(v => $"{vecType.Name} v{v}").ToArray(),
                Code = Enumerable.Range(0, Columns).Select(r => $"this[{r}] = v{r};").ToArray(),
                Comment = string.Format("Constructs matrix from a series of column vectors.")
            };

            // by-mat-ctors

            // TODO use MemoryMarshal.Cast<float4x4, float>(new Span<float4x4>(ref this))
            for (var rows = 2; rows <= 4; ++rows)
                for (var cols = 2; cols <= 4; ++cols)
                {
                    var paramMatrix = new MatrixType(BaseType, cols, rows);
                    yield return new Constructor(this, Fields)
                    {
                        Parameters = paramMatrix.TypedArgs("m"),
                        Code = Fields.Select(f => $"this{f} = {(paramMatrix.HasField(f) ? $"m{f}" : IsDiagonal(f) ? OneValue : ZeroValue)};").ToArray(),
                        Comment = $"Constructs matrix from a {paramMatrix.Name} which will occupie left upper corner. Non-overwritten fields are from an Identity matrix."
                    };
                }
        }
    }
}


