using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Rules
{
    interface IResults
    {
        bool Success { get; }
        string[] Errors { get; }
    }

    delegate IResults Rule(string entry);
}
