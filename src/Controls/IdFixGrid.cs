using IdFix.Rules;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IdFix.Controls
{
    /// <summary>
    /// Defines the IdFix grid
    /// </summary>
    public class IdFixGrid : DataGridView
    {
        /// <summary>
        /// Used to hold the set of results when this grid is populated through <see cref="SetFromResults"/>  
        /// </summary>
        private IEnumerable<ComposedRuleResult> _results;

        /// <summary>
        /// Internally records the current display page as a zero-based index. User page 1 == _currentPage 0
        /// </summary>
        private int _currentPage;

        /// <summary>
        /// Size of the display page for results (default: 50,000)
        /// </summary>
        private int _pageSize;

        /// <summary>
        /// The number of pages available, populated through <see cref="SetFromResults"/>
        /// </summary>
        private int _pageCount;

        /// <summary>
        /// The total number of results
        /// </summary>
        private int _totalResults;

        /// <summary>
        /// Tracks if the currently displayed results were filled from query results - or another souce such as CSV or LDF import
        /// </summary>
        private bool _filledFromResults;

        public IdFixGrid() : base()
        {
            this._results = null;
            this._currentPage = 0;
            this._pageSize = 50000;
            this._totalResults = 0;
            this._pageCount = 0;
            this._filledFromResults = false;
        }

        /// <summary>
        /// Used to send status messages to the hosting form
        /// </summary>
        public event OnStatusUpdateDelegate OnStatusUpdate;

        #region props

        /// <summary>
        /// Gets or sets the current size of the page to display
        /// </summary>
        public int PageSize
        {
            get
            {
                return this._pageSize;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("PageSize", value, "Pages must have a size greater than zero.");
                }

                if (value != this._pageSize)
                {
                    this._pageSize = value;
                    // redraw the grid using the new page
                    this.FillGrid();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current page to display starting with first page = 1
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the size provided is less that 1</exception>
        public int CurrentPage
        {
            get
            {
                // show them a 1 based page number
                return this._currentPage + 1;
            }
            set
            {
                var realPage = this._currentPage + 1;
                if (value != realPage)
                {
                    if (value < 1)
                    {
                        throw new ArgumentOutOfRangeException("PageSize", value, "Pages start at 1.");
                    }

                    if (value <= this._pageCount)
                    {
                        // allow them to set the first page as 1
                        this._currentPage = value - 1;

                        // redraw the grid using the new page
                        this.FillGrid();
                    }
                }
            }
        }

        /// <summary>
        /// Indicates if the grid has previous pages
        /// </summary>
        public bool HasPrev
        {
            get
            {
                return this.CurrentPage > 1;
            }
        }

        /// <summary>
        /// Indicates if the grid has more pages available
        /// </summary>
        public bool HasNext
        {
            get
            {
                return this.CurrentPage < this._pageCount;
            }
        }

        #endregion

        #region Reset

        /// <summary>
        /// Resets the grid's state to empty and resets internal counters
        /// </summary>
        public void Reset()
        {
            this.Rows.Clear();
            this.Refresh();
            if (this.Columns.Contains(StringLiterals.Update))
            {
                this.Columns[StringLiterals.Update].ReadOnly = false;
            }
            this._filledFromResults = true;
            this._results = null;
            this._currentPage = 0;
            this._totalResults = 0;
            this._pageCount = 0;
        }

        #endregion

        #region FillGrid

        /// <summary>
        /// Fills the grid using the current page and page size values from the available this._results
        /// </summary>
        private void FillGrid()
        {
            this.OnStatusUpdate?.Invoke("Clearing Grid");
            this.Rows.Clear();
            this.OnStatusUpdate?.Invoke("Cleared Grid");

            if (!this._filledFromResults || this._results == null || this._results.Count() < 1)
            {
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                this.OnStatusUpdate?.Invoke("Populating DataGrid");

                // calculate the results to show based on page size and current page
                var displaySet = this._results.Skip(this._currentPage * this.PageSize).Take(this.PageSize).ToArray();
                var len = displaySet.Length;
                // show those results
                for (var i = 0; i < len; i++)
                {
                    var item = displaySet[i];
                    var rowIndex = this.Rows.Add();
                    var row = this.Rows[rowIndex];
                    row.Cells[StringLiterals.DistinguishedName].Value = item.EntityDistinguishedName;
                    row.Cells[StringLiterals.ObjectClass].Value = item.ObjectType;
                    row.Cells[StringLiterals.Attribute].Value = item.AttributeName;
                    row.Cells[StringLiterals.Error].Value = item.ErrorsToString();
                    row.Cells[StringLiterals.Value].Value = item.OriginalValue;
                    row.Cells[StringLiterals.ProposedAction].Value = item.ProposedAction.ToString();
                    row.Cells[StringLiterals.Update].Value = item.ProposedValue;
                }

                if (this.RowCount > 0)
                {
                    this.CurrentCell = this.Rows[0].Cells[StringLiterals.DistinguishedName];
                }

                this.OnStatusUpdate?.Invoke("Populated DataGrid");

                timer.Stop();
                this.OnStatusUpdate?.Invoke(StringLiterals.ElapsedTimePopulateDataGridView + timer.Elapsed.TotalSeconds);

                if (this._pageCount > 1)
                {
                    this.OnStatusUpdate?.Invoke(string.Format("Total Error Count: {0} Displayed Count: {1} Page {2} of {3}", this._totalResults, this.Rows.Count, this.CurrentPage, this._pageCount));
                }
                else
                {
                    this.OnStatusUpdate?.Invoke(string.Format("Total Error Count: {0}", this._totalResults));
                }
            }
            catch (Exception err)
            {
                this.OnStatusUpdate?.Invoke(StringLiterals.Exception + StringLiterals.Threadsafe + "  " + err.Message);
                throw;
            }
        }

        #endregion

        #region SetResults

        /// <summary>
        /// Set the grid display from the parsed results of a search query
        /// </summary>
        /// <param name="results"><seealso cref="RulesRunnerResult"/> from an LDAP query</param>
        public void SetFromResults(RulesRunnerResult results)
        {
            this._filledFromResults = true;
            this.Reset();
            var result = this.BeginInvoke(new Func<IEnumerable<ComposedRuleResult>>(() =>
            {
                var r = results.ToDataset();
                this._totalResults = r.Count();
                return r;
            }));
            this._results = (IEnumerable<ComposedRuleResult>)this.EndInvoke(result);
            this._totalResults = this._results.Count();
            this._pageCount = (this._totalResults + this.PageSize - 1) / this.PageSize;
            this.FillGrid();
        }

        #endregion

        #region ToCsv

        /// <summary>
        /// Writes this grid to a stream as a CSV file
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="writer"></param>
        public void ToCsv(StreamWriter writer)
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
            writer.WriteLine(string.Join(",", this.Columns.Cast<DataGridViewColumn>().Select(c => c.Name.ToUpper(CultureInfo.CurrentCulture))));

            // now we write all the rows from the grid
            foreach (DataGridViewRow row in this.Rows)
            {
                writer.WriteLine(string.Join(",", row.Cells.Cast<DataGridViewCell>().Select(c => csvEscape(c.Value))));
            }
        }

        #endregion

        #region SetFromCsv

        /// <summary>
        /// Sets the results from a stream reader represeting a csv file previously exported by this webpart
        /// </summary>
        /// <param name="reader"></param>
        public void SetFromCsv(StreamReader reader)
        {
            this.Reset();
            this._filledFromResults = false;
            this._pageCount = 1;

            var parser = new TextFieldParser(reader);
            parser.Delimiters = new string[] { "," };
            parser.TrimWhiteSpace = true;
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TextFieldType = FieldType.Delimited;

            if (parser.EndOfData)
            {
                // they gave us an empty stream
                return;
            }

            try
            {
                // the first line is expected to have headers so we clear it
                var headers = parser.ReadFields().Select(f => f.ToUpper()).ToArray();
                var mappers = new List<Action<DataGridViewRow, string>>(headers.Length);
                Func<string, Action<DataGridViewRow, string>> mappingBinder = (string field) => (row, value) => row.Cells[field].Value = value;

                for (var i = 0; i < headers.Length; i++)
                {
                    switch (headers[i])
                    {
                        case "DISTINGUISHEDNAME":
                            mappers.Add(mappingBinder(StringLiterals.DistinguishedName));
                            break;
                        case "OBJECTCLASS":
                            mappers.Add(mappingBinder(StringLiterals.ObjectClass));
                            break;
                        case "ATTRIBUTE":
                            mappers.Add(mappingBinder(StringLiterals.Attribute));
                            break;
                        case "ERROR":
                            mappers.Add(mappingBinder(StringLiterals.Error));
                            break;
                        case "VALUE":
                            mappers.Add(mappingBinder(StringLiterals.Value));
                            break;
                        case "UPDATE":
                            mappers.Add(mappingBinder(StringLiterals.Update));
                            break;
                        case "PROPOSEDACTION":
                            mappers.Add(mappingBinder(StringLiterals.ProposedAction));
                            break;
                        case "ACTION":
                            mappers.Add((DataGridViewRow row, string value) =>
                            {
                                row.Cells[StringLiterals.Action].Value = new string[] { "EDIT", "REMOVE", "COMPLETE", "UNDO", "FAIL" }.Contains(value) ? value : string.Empty;
                            });
                            break;
                    }
                }

                // now loop on all the rows and return them to the grid
                while (!parser.EndOfData)
                {
                    var row = this.Rows[this.Rows.Add()];
                    var fields = parser.ReadFields();

                    for (var i = 0; i < fields.Length; i++)
                    {
                        // map the field into the row
                        mappers[i](row, fields[i]);
                    }
                }
            }
            catch (Exception err)
            {
                this.OnStatusUpdate?.Invoke(StringLiterals.Exception + "Import CSV Line: [" + parser.LineNumber + "] " + err.Message);
            }
            finally
            {
                parser.Dispose();
            }

            if (this.RowCount >= 1)
            {
                this.Sort(this.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
                this.CurrentCell = this.Rows[0].Cells[StringLiterals.DistinguishedName];
            }

            this._totalResults = this.RowCount;
        }

        #endregion

        #region ToLdf

        /// <summary>
        /// Writes this grid's contents to a stream as an LDF file <seealso cref="SetFromLdf"/>
        /// </summary>
        /// <param name="writer"></param>
        public void ToLdf(StreamWriter writer)
        {
            string vl;
            string up;
            string at;
            foreach (DataGridViewRow row in this.Rows)
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

        #endregion

        #region SetFromLdf

        /// <summary>
        /// Enables filling from the ldf file written by <seealso cref="ToLdf"/>
        /// </summary>
        /// <param name="reader"></param>
        public void SetFromLdf(StreamReader reader, bool isUndo = false)
        {
            this.Reset();
            this._filledFromResults = false;
            this._pageCount = 1;

            string line;
            int lineIndex = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.IndexOf(": ", StringComparison.CurrentCulture) > -1)
                {
                    switch (line.Substring(0, line.IndexOf(": ", StringComparison.CurrentCulture)))
                    {
                        case "distinguishedName":
                            lineIndex = this.Rows.Add();
                            this.Rows[lineIndex].Cells[StringLiterals.DistinguishedName].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                            break;
                        case "objectClass":
                            this.Rows[lineIndex].Cells[StringLiterals.ObjectClass].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                            break;
                        case "attribute":
                            this.Rows[lineIndex].Cells[StringLiterals.Attribute].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                            break;
                        case "error":
                            this.Rows[lineIndex].Cells[StringLiterals.Error].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                            break;
                        case "value":
                            this.Rows[lineIndex].Cells[StringLiterals.Value].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                            break;
                        case "update":
                            this.Rows[lineIndex].Cells[StringLiterals.Update].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                            break;
                    }
                }
            }

            if (isUndo)
            {
                for (var i = 0; i < this.Rows.Count; i++)
                {
                    // auto fill the proposed action to undo so if they accept it will fill the drop downs as expected
                    this.Rows[i].Cells[StringLiterals.ProposedAction].Value = StringLiterals.Undo;
                }
            }

            this.Sort(this.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
            if (this.RowCount >= 1)
            {
                this.CurrentCell = this.Rows[0].Cells[StringLiterals.DistinguishedName];
            }

            // record the total number of rows added
            this._totalResults = lineIndex + 1;
        }

        #endregion
    }
}
