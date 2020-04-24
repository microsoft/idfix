using IdFix.Controls;
using IdFix.Rules;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
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
            Func<string, string> csvEscape = (string str) =>
            {
                return str.IndexOf(",") > -1 ? string.Format("\"{0}\"", str) : str;
            };

            // write the headers
            writer.WriteLine(string.Join(",", grid.Columns.Cast<DataGridViewColumn>().Select(c => c.Name.ToUpper(CultureInfo.CurrentCulture))));

            // now we write all the rows from the grid
            foreach (DataGridViewRow row in grid.Rows)
            {
                writer.WriteLine(string.Join(",", row.Cells.Cast<DataGridViewCell>().Select(c => csvEscape(c.Value.ToString()))));
            }
        }

        public static void ToLdf(this IdFixGrid grid, StreamWriter writer)
        {
            string vl;
            string up;
            string at;
            foreach (DataGridViewRow row in grid.Rows)
            {
                vl = row.Cells[StringLiterals.Value].Value.ToString();
                up = row.Cells[StringLiterals.Update].Value.ToString();
                at = row.Cells[StringLiterals.Attribute].Value.ToString();

                writer.WriteLine("dn: " + row.Cells[StringLiterals.DistinguishedName].Value.ToString());
                writer.WriteLine("changetype: modify");

                if (at.ToUpperInvariant() == StringLiterals.ProxyAddresses.ToUpperInvariant())
                {
                    writer.WriteLine("delete: " + at);
                    writer.WriteLine(at + ": " + vl);
                    writer.WriteLine("-");
                    writer.WriteLine();
                    writer.WriteLine("dn: " + row.Cells[StringLiterals.DistinguishedName].Value.ToString());
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
    }
}
