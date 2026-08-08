using Kibix.MathsGen.Types;

namespace Kibix.MathsGen.Members
{
    internal class Operator : Function
    {
        public Operator(AbstractType type, string op) : base(type, "operator" + op)
        {
            Static = true;
        }
    }
}
