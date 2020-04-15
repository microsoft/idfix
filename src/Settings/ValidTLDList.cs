using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace IdFix.Settings
{
    class ValidTLDList : List<string>
    {
        public ValidTLDList() : base()
        {
            this.Init();
        }

        private void Init()
        {
            using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("IdFix.domains.txt")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var formattedLine = line.Trim().ToLowerInvariant();
                    if (!this.Contains(formattedLine))
                    {
                        this.Add(formattedLine);
                    }
                }
            }
        }
    }
}
