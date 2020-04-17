using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IdFix.Settings
{
    class ValidTLDList : List<string>
    {
        private static List<string> _cachedList = null;

        public ValidTLDList(bool useCached = true) : base()
        {
            this.Init(useCached);
        }

        private void Init(bool useCached)
        {
            if (useCached && ValidTLDList._cachedList != null)
            {
                this.AddRange(ValidTLDList._cachedList);
            }
            else
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

                if (ValidTLDList._cachedList == null)
                {
                    ValidTLDList._cachedList = new List<string>();
                    ValidTLDList._cachedList.AddRange(this);
                }
            }
        }
    }
}
