using DVG.GLSH.Generator.Types;

namespace DVG.GLSH.Generator.Members
{
    internal class Operator : Function
    {
        public Operator(AbstractType type, string op) : base(type, "operator" + op)
        {
            Static = true;
        }
    }
}
