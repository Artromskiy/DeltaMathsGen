using GLSHGenerator.Types;

namespace GLSHGenerator.Members
{
    internal class Operator : Function
    {
        public Operator(AbstractType type, string op) : base(type, "operator" + op)
        {
            Static = true;
        }
    }
}
