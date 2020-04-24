using IdFix.Controls;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IdFix
{
    static class IdFixGridExtensions
    {
        public static void ToCsv(this IdFixGrid grid, StreamWriter writer)
        {
            Func<object, string> csvEscape = (object obj) =>
            {
                if (obj == null)
                {
                    return string.Empty;
                }

                var str = obj.ToString();

                return string.IsNullOrEmpty(str) ? string.Empty : str.IndexOf(",") > -1 ? string.Format("\"{0}\"", str) : str;
            };

            // write the headers
            writer.WriteLine(string.Join(",", grid.Columns.Cast<DataGridViewColumn>().Select(c => c.Name.ToUpper(CultureInfo.CurrentCulture))));

            // now we write all the rows from the grid
            foreach (DataGridViewRow row in grid.Rows)
            {
                writer.WriteLine(string.Join(",", row.Cells.Cast<DataGridViewCell>().Select(c => csvEscape(c.Value))));
            }
        }

        public static void ToLdf(this IdFixGrid grid, StreamWriter writer)
        {
            string vl;
            string up;
            string at;
            foreach (DataGridViewRow row in grid.Rows)
            {
                vl = row.GetCellString(StringLiterals.Value);
                up = row.GetCellString(StringLiterals.Update);
                at = row.GetCellString(StringLiterals.Attribute);

                writer.WriteLine("dn: " + row.GetCellString(StringLiterals.DistinguishedName));
                writer.WriteLine("changetype: modify");

                if (at.ToUpperInvariant() == StringLiterals.ProxyAddresses.ToUpperInvariant())
                {
                    writer.WriteLine("delete: " + at);
                    writer.WriteLine(at + ": " + vl);
                    writer.WriteLine("-");
                    writer.WriteLine();
                    writer.WriteLine("dn: " + row.GetCellString(StringLiterals.DistinguishedName));
                    writer.WriteLine("changetype: modify");
                    writer.WriteLine("add: " + at);
                }
                else
                {
                    writer.WriteLine("replace: " + at);
                }

                //if (update != String.Empty)
                if (!String.IsNullOrEmpty(up))
                {
                    writer.WriteLine(at + ": " + up);
                }
                else
                {
                    writer.WriteLine(at + ": " + vl);
                }
                writer.WriteLine("-");
                writer.WriteLine();
            }
        }

        public static string GetCellString(this DataGridViewRow row, string cellName, string @default = "")
        {
            if (row.Cells[cellName] == null || row.Cells[cellName].Value == null)
            {
                return @default;
            }

            return row.Cells[cellName].Value.ToString();
        }
    }
}
