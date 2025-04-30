using DVG.GLSH.Generator.Types;

namespace DVG.GLSH.Generator.Members
{
    internal class StaticProperty : Property
    {
        public StaticProperty(string name, AbstractType type) : base(name, type)
        {
            Static = true;
        }
    }
}
