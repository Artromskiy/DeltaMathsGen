using GLSHGenerator.Members;
using System;
using System.Collections.Generic;

namespace GLSHGenerator.Types
{
    internal class ArrayType : AbstractType
    {
        /// <summary>
        /// Array suffix
        /// </summary>
        public string ArraySuffix { get; set; }

        public ArrayType(BuiltinType type, string suffix = "[]")
        {
            BaseType = type;
            ArraySuffix = suffix;
        }

        public override string Name => BaseType.Name + ArraySuffix;

        public override string TypeComment
        {
            get { throw new NotSupportedException(); }
        }

        public override IEnumerable<Member> GenerateMembers()
        {
            throw new NotSupportedException();
        }
    }
}
