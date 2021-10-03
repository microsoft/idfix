using System;
using System.IO;
using System.Text.RegularExpressions;

namespace IdFix
{
    enum FileTypes
    {
        Verbose,
        Apply,
        Error,
    }

    // TODO:: how is this used
    // TODO:: ensure we are usign all these, writing error files? apply files?

    /// <summary>
    /// Files helper to write to various logging files
    /// </summary>
    class Files
    {
        private string _directory = string.Empty;
        private string _fileBase;

        public Files(string seed = null)
        {
            if (string.IsNullOrEmpty(seed))
            {
                seed = new Regex(@"[/:]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(DateTime.Now.ToString(), "-");
            }
            this._fileBase = seed;

            // Test if the user has permissions to write to the current directory.
            try
            {
                var testFileName = "test.txt";
                File.WriteAllText(testFileName, "test");
                File.Delete(testFileName);
            }
            catch
            {
                _directory = Path.GetDirectoryName(Path.GetTempPath());
                if (!_directory.EndsWith(@"\"))
                {
                    _directory += @"\";
                }
            }
        }

        public string VerboseFileName
        {
            get
            {
                return string.Format(_directory + "Verbose {0}.txt", this._fileBase);
            }
        }

        public string ApplyFileName
        {
            get
            {
                return string.Format(_directory + "Update {0}.ldf", this._fileBase);
            }
        }

        public string ErrorFileName
        {
            get
            {
                return string.Format(_directory + "Error {0}.txt", this._fileBase);
            }
        }

        public void AppendTo(FileTypes type, Action<StreamWriter> action)
        {
            using (var writer = new StreamWriter(this.GetNameFromType(type), true))
            {
                action(writer);
            }
        }

        public void ReadFrom(FileTypes type, Action<StreamReader> action)
        {
            using (var reader = new StreamReader(this.GetNameFromType(type)))
            {
                action(reader);
            }
        }

        public bool ExistsByType(FileTypes type)
        {
            return File.Exists(this.GetNameFromType(type));
        }

        public void DeleteByType(FileTypes type)
        {
            if (File.Exists(this.GetNameFromType(type)))
            {
                File.Delete(this.GetNameFromType(type));
            }
        }

        public void DeleteAll()
        {
            this.DeleteByType(FileTypes.Apply);
            this.DeleteByType(FileTypes.Error);
            this.DeleteByType(FileTypes.Verbose);
        }

        private string GetNameFromType(FileTypes type)
        {
            string fileName;
            switch (type)
            {
                case FileTypes.Apply:
                    fileName = this.ApplyFileName;
                    break;
                case FileTypes.Verbose:
                    fileName = this.VerboseFileName;
                    break;
                case FileTypes.Error:
                    fileName = this.ErrorFileName;
                    break;
                default:
                    fileName = this.VerboseFileName;
                    break;
            }
            return fileName;
        }
    }
}
