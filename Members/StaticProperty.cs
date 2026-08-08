using Kibix.MathsGen.Types;

namespace Kibix.MathsGen.Members
{
    internal class StaticProperty : Property
    {
        public StaticProperty(string name, AbstractType type) : base(name, type)
        {
            Static = true;
        }
    }
}
