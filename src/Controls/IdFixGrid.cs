using IdFix.Rules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public IdFixGrid() : base()
        {
            this._results = null;
            this.CurrentPage = 1;
            this.PageSize = 50000;
            this._totalResults = 0;
            this._pageCount = 0;
        }

        public event OnStatusUpdateDelegate OnStatusUpdate;

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
                return this._currentPage;
            }
            set
            {
                if (value != this._currentPage)
                {
                    if (value < 1)
                    {
                        throw new ArgumentOutOfRangeException("PageSize", value, "Pages start at 1.");
                    }

                    // allow them to set the first page as 1
                    this._currentPage = value - 1;

                    // redraw the grid using the new page
                    this.FillGrid();
                }
            }
        }

        public void Reset()
        {
            this.Rows.Clear();
            this.Refresh();
            if (this.Columns.Contains(StringLiterals.Update))
            {
                this.Columns[StringLiterals.Update].ReadOnly = false;
            }            
        }

        private void FillGrid()
        {
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                this.OnStatusUpdate?.Invoke("Populating DataGrid");
                this.Reset();
                if (this._results != null)
                {
                    var displaySet = this._results.Skip(this._currentPage * this.PageSize).Take(this.PageSize);
                    foreach (var errorData in displaySet)
                    {
                        var rowIndex = this.Rows.Add();
                        var row = this.Rows[rowIndex];
                        row.Cells[StringLiterals.DistinguishedName].Value = errorData.EntityDistinguishedName;
                        row.Cells[StringLiterals.ObjectClass].Value = errorData.ObjectType;
                        row.Cells[StringLiterals.Attribute].Value = errorData.AttributeName;
                        row.Cells[StringLiterals.Error].Value = errorData.ErrorsToString();
                        row.Cells[StringLiterals.Value].Value = errorData.OriginalValue;
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

        public void SetResults(RulesRunnerResult results)
        {
            this._results = results.ToDataset();
            this._totalResults = this._results.Count();
            this._pageCount = (results.Count + this.PageSize - 1) / this.PageSize;
            this.FillGrid();
        }
    }
}
