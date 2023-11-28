using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WrathPatches
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class WrathPatchAttribute(string name) : Attribute
    {
        public readonly string Name = name;
        public string Description = "";
    }
}
