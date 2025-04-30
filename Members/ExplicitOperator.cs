using DVG.GLSH.Generator.Types;

namespace DVG.GLSH.Generator.Members
{
    internal class ExplicitOperator : Function
    {
        public override string MemberPrefix => base.MemberPrefix + " explicit";
        public override string FunctionName => ReturnType.Name;
        public override string ReturnName => "operator";

        public ExplicitOperator(AbstractType type) : base(type, type.Name)
        {
            Static = true;
        }
    }
}
