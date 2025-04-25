using GLSHGenerator.Types;

namespace GLSHGenerator.Members
{
    internal class StaticProperty : Property
    {
        public StaticProperty(string name, AbstractType type) : base(name, type)
        {
            Static = true;
        }
    }
}
