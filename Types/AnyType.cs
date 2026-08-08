using Kibix.MathsGen.Members;
using System;
using System.Collections.Generic;

namespace Kibix.MathsGen.Types
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
