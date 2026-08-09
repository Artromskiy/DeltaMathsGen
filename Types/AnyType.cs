using KibiHex.MathsGen.Members;
using System;
using System.Collections.Generic;

namespace KibiHex.MathsGen.Types
{
    internal class AnyType : AbstractType
    {
        public AnyType(string name)
        {
            Name = name;
        }

        public override string Name { get; }

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

