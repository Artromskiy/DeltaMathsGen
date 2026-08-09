using KibiHex.MathsGen.Types;

namespace KibiHex.MathsGen.Members
{
    internal class StaticProperty : Property
    {
        public StaticProperty(string name, AbstractType type) : base(name, type)
        {
            Static = true;
        }
    }
}

