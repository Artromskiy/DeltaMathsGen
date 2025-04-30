using DVG.GLSH.Generator.Members;
using System;
using System.Collections.Generic;

namespace DVG.GLSH.Generator.Types
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
