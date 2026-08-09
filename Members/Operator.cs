using KibiHex.MathsGen.Types;

namespace KibiHex.MathsGen.Members
{
    internal class Operator : Function
    {
        public Operator(AbstractType type, string op) : base(type, "operator" + op)
        {
            Static = true;
        }
    }
}

