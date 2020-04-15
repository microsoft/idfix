using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdFix
{
    enum FileTypes
    {
        Verbose,
        Apply,
        Error,
        Duplicate,
        Filtered,
        Merge,
    }

    class Files
    {
        private string _fileBase;

        public Files(string seed = null)
        {
            if (string.IsNullOrEmpty(seed))
            {
                seed = new Regex(@"[/:]").Replace(DateTime.Now.ToString(), "-");
            }
            this._fileBase = seed;
        }

        public string VerboseFileName
        {
            get
            {
                return string.Format("Verbose {0}.txt", this._fileBase);
            }
        }

        public string ApplyFileName
        {
            get
            {
                return string.Format("Update {0}.ldf", this._fileBase);
            }
        }

        public string DuplicateFileName
        {
            get
            {
                return string.Format("Duplicate {0}.txt", this._fileBase);
            }
        }

        public string ErrorFileName
        {
            get
            {
                return string.Format("Error {0}.txt", this._fileBase);
            }
        }

        public string MergeFileName
        {
            get
            {
                return string.Format("Merge {0}.txt", this._fileBase);
            }
        }

        public string FilteredFileName
        {
            get
            {
                return string.Format("Filtered {0}.txt", this._fileBase);
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
            this.DeleteByType(FileTypes.Duplicate);
            this.DeleteByType(FileTypes.Error);
            this.DeleteByType(FileTypes.Filtered);
            this.DeleteByType(FileTypes.Merge);
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
                case FileTypes.Duplicate:
                    fileName = this.DuplicateFileName;
                    break;
                case FileTypes.Error:
                    fileName = this.ErrorFileName;
                    break;
                case FileTypes.Filtered:
                    fileName = this.FilteredFileName;
                    break;
                case FileTypes.Merge:
                    fileName = this.MergeFileName;
                    break;
                default:
                    fileName = this.VerboseFileName;
                    break;
            }
            return fileName;
        }
    }
}
