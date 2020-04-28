using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IdFix.Settings
{
    /// <summary>
    /// Reads the list of valid tld's from the resource file and stores them in memory
    /// </summary>
    class ValidTLDList : List<string>
    {
        private static List<string> _cachedList = null;

        /// <summary>
        /// Creates a new instance of the <see cref="ValidTLDList"/> class
        /// </summary>
        /// <param name="useCached"></param>
        public ValidTLDList(bool useCached = true) : base()
        {
            this.Init(useCached);
        }

        /// <summary>
        /// Determines if a given tld is valid based on its existence in this list
        /// </summary>
        /// <param name="tld">value to check</param>
        /// <returns>true if found, false otherwise</returns>
        public bool IsValid(string tld)
        {
            return this.Contains(tld);
        }

        /// <summary>
        /// Inits this collection
        /// </summary>
        /// <param name="useCached">If true and a cached list exists it is returned, otherwise a new list is read</param>
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
