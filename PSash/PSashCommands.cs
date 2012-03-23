using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSash
{
    public sealed class PSashCommands : ValueObject<PSashCommands.PSashCommand, string, PSashCommands>.Values<PSashCommands>
    {
        const string TUTORIAL = "tutorial";
        private PSashCommands() 
        {
            Tutorial = Add(TUTORIAL);
        }
        private PSashCommand Tutorial;

        public static PSashCommand From(string s)
        {
            PSashCommand cmd;
            if (PSashCommands.Instance.TryGetValue(s.ToLowerInvariant(), out cmd))
                return cmd;
            return null;
        }

        [Serializable]
        public sealed class PSashCommand : ValueObject<PSashCommand, string, PSashCommands>
        {
            private PSashCommand(string v) : base(v) { }
        }
    }
}
