using System.Windows.Forms;

namespace IdFix
{
    static class DataGridViewRowExtensions
    {
        /// <summary>
        /// Safely gets a cell's value from a <see cref="DataGridViewRow"/> as a string - or results the provided default
        /// </summary>
        /// <param name="row">"this" row</param>
        /// <param name="cellName">Name of the cell whose value we want</param>
        /// <param name="default">A default value if this row doesn't have a cell by that name or the value is null</param>
        /// <returns></returns>
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
