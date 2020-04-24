using IdFix.Rules;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IdFix.Controls
{
    public class IdFixGrid : DataGridView
    {
        private IEnumerable<ComposedRuleResult> _results;
        private int _currentPage;
        private int _pageSize;
        private int _pageCount;
        private int _totalResults;
        private bool _filledFromResults;

        public IdFixGrid() : base()
        {
            this._results = null;
            this.CurrentPage = 1;
            this.PageSize = 50000;
            this._totalResults = 0;
            this._pageCount = 0;
            this._filledFromResults = false;
        }

        public event OnStatusUpdateDelegate OnStatusUpdate;

        #region props

        public int PageSize
        {
            get
            { return this._pageSize; }
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

        public int CurrentPage
        {
            get
            {
                // show them a 1 based page number
                return this._currentPage + 1;
            }
            set
            {
                if (value != this._currentPage)
                {
                    if (value < 1)
                    {
                        throw new ArgumentOutOfRangeException("PageSize", value, "Pages start at 1.");
                    }

                    if (value > this._pageCount)
                    {
                        throw new ArgumentOutOfRangeException("PageSize", value, string.Format("This grid has {0} pages.", this._pageCount));
                    }

                    // allow them to set the first page as 1
                    this._currentPage = value - 1;

                    // redraw the grid using the new page
                    this.FillGrid();
                }
            }
        }

        #endregion

        #region Reset

        public void Reset()
        {
            this.Rows.Clear();
            this.Refresh();
            if (this.Columns.Contains(StringLiterals.Update))
            {
                this.Columns[StringLiterals.Update].ReadOnly = false;
            }
            this._filledFromResults = true;
        }

        #endregion

        #region FillGrid

        private void FillGrid()
        {
            if (!this._filledFromResults)
            {
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                this.OnStatusUpdate?.Invoke("Populating DataGrid");
                this.Reset();
                if (this._results != null)
                {
                    // calculate the results to show based on page size and current page
                    var displaySet = this._results.Skip(this._currentPage * this.PageSize).Take(this.PageSize);

                    // show those results
                    foreach (var errorData in displaySet)
                    {
                        var rowIndex = this.Rows.Add();
                        var row = this.Rows[rowIndex];
                        row.Cells[StringLiterals.DistinguishedName].Value = errorData.EntityDistinguishedName;
                        row.Cells[StringLiterals.ObjectClass].Value = errorData.ObjectType;
                        row.Cells[StringLiterals.Attribute].Value = errorData.AttributeName;
                        row.Cells[StringLiterals.Error].Value = errorData.ErrorsToString();
                        row.Cells[StringLiterals.Value].Value = errorData.OriginalValue;
                        row.Cells[StringLiterals.ProposedAction].Value = errorData.ProposedAction.ToString();
                        row.Cells[StringLiterals.Update].Value = errorData.ProposedValue;
                    }
                }

                if (this.RowCount > 0)
                {
                    this.CurrentCell = this.Rows[0].Cells[StringLiterals.DistinguishedName];
                }

                timer.Stop();
                this.OnStatusUpdate?.Invoke(StringLiterals.ElapsedTimePopulateDataGridView + timer.Elapsed.TotalSeconds);

                if (this._pageCount > 1)
                {
                    this.OnStatusUpdate?.Invoke(string.Format("Total Error Count: {0} Displayed Count: {1}", this._totalResults, this.Rows.Count));
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

        public void SetResults(RulesRunnerResult results)
        {
            this._filledFromResults = true;
            this.Reset();
            this._results = results.ToDataset();
            this._totalResults = this._results.Count();
            this._pageCount = (results.Count + this.PageSize - 1) / this.PageSize;
            this.FillGrid();
        }

        #endregion

        #region SetFromCsv

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

                for (var i = 0; i < headers.Length; i++)
                {
                    switch (headers[i])
                    {
                        case "DISTINGUISHEDNAME":
                            mappers[i] = (DataGridViewRow row, string value) => row.Cells[StringLiterals.DistinguishedName].Value = value;
                            break;
                        case "OBJECTCLASS":
                            mappers[i] = (DataGridViewRow row, string value) => row.Cells[StringLiterals.ObjectClass].Value = value;
                            break;
                        case "ATTRIBUTE":
                            mappers[i] = (DataGridViewRow row, string value) => row.Cells[StringLiterals.Attribute].Value = value;
                            break;
                        case "ERROR":
                            mappers[i] = (DataGridViewRow row, string value) => row.Cells[StringLiterals.Error].Value = value;
                            break;
                        case "VALUE":
                            mappers[i] = (DataGridViewRow row, string value) => row.Cells[StringLiterals.Value].Value = value;
                            break;
                        case "UPDATE":
                            mappers[i] = (DataGridViewRow row, string value) => row.Cells[StringLiterals.Update].Value = value;
                            break;
                        case "PROPOSED":
                            mappers[i] = (DataGridViewRow row, string value) => row.Cells[StringLiterals.ProposedAction].Value = value;
                            break;
                        case "ACTION":
                            mappers[i] = (DataGridViewRow row, string value) =>
                            {
                                row.Cells[StringLiterals.DistinguishedName].Value = new string[] { "EDIT", "REMOVE", "COMPLETE", "UNDO", "FAIL" }.Contains(value) ? value : string.Empty;
                            };
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
        }

        #endregion

        #region SetFromLdf

        /// <summary>
        /// Enables filling from the ldf file written
        /// </summary>
        /// <param name="reader"></param>
        public void SetFromLdf(StreamReader reader)
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

            this.Sort(this.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
            if (this.RowCount >= 1)
            {
                this.CurrentCell = this.Rows[0].Cells[StringLiterals.DistinguishedName];
            }
        }

        #endregion
    }
}
