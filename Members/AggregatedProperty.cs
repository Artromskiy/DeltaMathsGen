using DVG.GLSH.Generator.Types;
using System.Collections.Generic;

namespace DVG.GLSH.Generator.Members
{
    internal class AggregatedProperty : Property
    {
        public AggregatedProperty(IEnumerable<string> fields, string name, AbstractType type, string sep, string comment) : base(name, type)
        {
            GetterLine = fields.Aggregated(" " + sep + " ");
            Comment = comment;
        }
    }
}
