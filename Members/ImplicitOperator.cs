using Kibix.MathsGen.Types;

namespace Kibix.MathsGen.Members
{
    internal class ImplicitOperator : Function
    {
        public override string MemberPrefix => base.MemberPrefix + " implicit";
        public override string FunctionName => ReturnType.Name;
        public override string ReturnName => "operator";

        public ImplicitOperator(AbstractType type) : base(type, type.Name)
        {
            Static = true;
        }
    }
}
