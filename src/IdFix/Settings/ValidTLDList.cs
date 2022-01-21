using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace IdFix.Settings
{
    /// <summary>
    /// Reads the list of valid tld's from the resource file and stores them in memory
    /// </summary>
    class ValidTLDList : List<string>
    {
        /// <summary>
        /// Whether or not the user has seen the missing file warning
        /// </summary>
        public static bool HasSeenMissingFileWarning = false;

        /// <summary>
        /// Creates a new instance of the <see cref="ValidTLDList"/> class
        /// </summary>
        public ValidTLDList() : base()
        {
            this.Init();
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
        private void Init()
        {
            using (StreamReader reader = new StreamReader(this.GetDomainsStream()))
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

        /// <summary>
        /// Gets the domains stream from either the domains.txt file or the embedded defaultdomains.txt file if domains.txt does not exist.
        /// </summary>
        /// <returns></returns>
        private Stream GetDomainsStream()
        {
            if (!this.DoesDomainsFileExist())
            {
                if (!HasSeenMissingFileWarning)
                {
                    // Update on main thread.
                    FormApp.Instance.Invoke(new Action(() =>
                    {
                        var message = string.Format(StringLiterals.DomainsFileNotFoundMessage, this.GetDomainsFilePath());
                        MessageBox.Show(FormApp.Instance, message, StringLiterals.DomainsFileNotFoundTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        HasSeenMissingFileWarning = true;
                    }));
                }

                return this.GetEmbeddedDomainsStream();
            }

            return new StreamReader(this.GetDomainsFilePath()).BaseStream;
        }

        /// <summary>
        /// Gets the domains stream from the embedded defaultdomains.txt file
        /// </summary>
        /// <returns></returns>
        private Stream GetEmbeddedDomainsStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("IdFix.defaultdomains.txt");
        }

        /// <summary>
        /// Determines if the domains.txt file exists
        /// </summary>
        /// <returns>True, if the domains.txt file exists</returns>
        private bool DoesDomainsFileExist()
        {
            var domainsFilePath = this.GetDomainsFilePath();

            return File.Exists(domainsFilePath);
        }

        /// <summary>
        /// The location of the domains.txt file in the same directory as the executable
        /// </summary>
        /// <returns>The full path to the domains.txt file</returns>
        private string GetDomainsFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "domains.txt");
        }
    }
}
