using IdFix.Rules;
using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Windows.Forms;

namespace IdFix
{
    public partial class FormApp : Form
    {
        Files files = new Files();

        RulesRunner runner;

        internal bool firstRun = false;

        internal const int maxUserNameLength = 64;
        internal const int maxDomainLength = 48;

        public FormApp()
        {
            try
            {
                this.firstRun = true; //Only the first time.
                InitializeComponent();
                statusDisplay("Initialized - " + StringLiterals.IdFixVersionFormat);
                MessageBox.Show(StringLiterals.IdFixPrivacyBody,
                        StringLiterals.IdFixPrivacyTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + "Initialize  " + ex.Message);
                throw;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Let's enable only the first run buttons
            EnableButtons();
            try
            {
                this.Text = string.Format(StringLiterals.IdFixVersionFormat, Application.ProductVersion);

                // setup the grid to display results
                if (firstRun)
                {
                    this.grid.OnStatusUpdate += (string message) =>
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            statusDisplay(message);
                        });
                    };
                }

                // setup the background worker
                runner = new RulesRunner();
                runner.OnStatusUpdate += (string message) =>
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        statusDisplay(message);
                    });
                };
                runner.RunWorkerCompleted += (object s, RunWorkerCompletedEventArgs args) =>
                {
                    if (args.Error != null || args.Result is Exception)
                    {
                        if (args.Result is Exception)
                        {
                            MessageBox.Show((args.Result as Exception).Message);
                        }
                        else
                        {
                            MessageBox.Show(args.Error.Message);
                        }
                    }
                    else if (args.Cancelled)
                    {
                        statusDisplay(StringLiterals.CancelQuery);

                        // TODO:: on cancel need to update button display, check with original code to see how they did it.

                    }
                    else
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            firstRun = false;

                            queryToolStripMenuItem.Enabled = true;
                            cancelToolStripMenuItem.Enabled = false;
                            acceptToolStripMenuItem.Enabled = true;
                            applyToolStripMenuItem.Enabled = true;
                            exportToolStripMenuItem.Enabled = true;
                            importToolStripMenuItem.Enabled = true;
                            undoToolStripMenuItem.Enabled = true;

                            // this should update the UI grid with all our error results
                            // also need to check for errors
                            var results = (RulesRunnerResult)args.Result;

                            // update status with final results
                            statusDisplay(string.Format("Total Elapsed Time: {0}s", results.TotalElapsed.TotalSeconds));
                            statusDisplay(string.Format("Total Entries Found: {0} Error Count: {1} Duplicates: {2} Skipped: {3}", results.TotalFound, results.TotalErrors, results.TotalDuplicates, results.TotalSkips));

                            // set the results on our grid which will handle filling itself
                            this.grid.SetFromResults(results);
                            this.SetPagingVisibility();
                        });
                    }
                };
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + "Form Load  " + ex.Message);
                throw;
            }
        }

        private void EnableButtons()
        {
            //We use the firstRun flag to decide what to do
            if (firstRun)
            {
                //Disable everything except Query & Import
                acceptToolStripMenuItem.Enabled = false;
                applyToolStripMenuItem.Enabled = false;
                exportToolStripMenuItem.Enabled = false;
            }
            else
            {
                //We need to enable all these buttons
                acceptToolStripMenuItem.Enabled = true;
                applyToolStripMenuItem.Enabled = true;
                exportToolStripMenuItem.Enabled = true;
            }

        }

        private void SetPagingVisibility()
        {
            previousToolStripMenuItem.Visible = this.grid.HasPrev;
            nextToolStripMenuItem.Visible = this.grid.HasNext;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                Application.Exit();
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.Exit + "  " + ex.Message);
                throw;
            }
        }

        #region menu
        private void queryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                statusDisplay(StringLiterals.Query);

                // reset (clear) our grid
                this.grid.Reset();

                queryToolStripMenuItem.Enabled = false;
                cancelToolStripMenuItem.Enabled = true;
                acceptToolStripMenuItem.Enabled = false;
                applyToolStripMenuItem.Enabled = false;
                exportToolStripMenuItem.Enabled = false;
                importToolStripMenuItem.Enabled = false;
                undoToolStripMenuItem.Enabled = false;
                nextToolStripMenuItem.Visible = false;
                previousToolStripMenuItem.Visible = false;

                editActionToolStripMenuItem.Visible = true;
                removeActionToolStripMenuItem.Visible = true;
                undoActionToolStripMenuItem.Visible = false;

                action.Items.Clear();
                action.Items.Add(StringLiterals.Edit);
                action.Items.Add(StringLiterals.Remove);
                action.Items.Add(StringLiterals.Complete);

                // clear the log file ahead of running the scan
                files.DeleteByType(FileTypes.Verbose);
                runner.RunWorkerAsync(new RulesRunnerDoWorkArgs());
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + toolStripStatusLabel1.Text + "  " + ex.Message);
                throw;
            }
        }

        private void cancelQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    cancelToolStripMenuItem.Enabled = false;
                });
                statusDisplay("Canceling...");
                runner.CancelAsync();
                statusDisplay("Canceled");
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.CancelQuery + "  " + ex.Message);
                throw;
            }
        }

        private void acceptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show(StringLiterals.AcceptAllUpdatesBody,
                    StringLiterals.AcceptAllUpdates,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (!Enum.TryParse(row.GetCellString(StringLiterals.ProposedAction, "Edit"), true, out ActionType proposedAction))
                        {
                            proposedAction = ActionType.Edit;
                        }

                        switch (proposedAction)
                        {
                            case ActionType.Complete:
                                row.Cells[StringLiterals.Action].Value = StringLiterals.Complete;
                                break;
                            case ActionType.Edit:
                                row.Cells[StringLiterals.Action].Value = StringLiterals.Edit;
                                break;
                            case ActionType.Remove:
                                row.Cells[StringLiterals.Action].Value = StringLiterals.Remove;
                                break;
                            case ActionType.Undo:
                                row.Cells[StringLiterals.Action].Value = StringLiterals.Undo;
                                break;
                            default:
                                row.Cells[StringLiterals.Action].Value = StringLiterals.Edit;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.MenuAccept + "  " + ex.Message);
                throw;
            }
        }

        private void applyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO:: can we improve this or break the code out somehow?

            DialogResult result = MessageBox.Show(StringLiterals.ApplyPendingBody,
                StringLiterals.ApplyPending,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes || grid.Rows.Count < 1)
            {
                return;
            }

            statusDisplay(StringLiterals.ApplyPending);
            grid.CurrentCell = grid.Rows[0].Cells[StringLiterals.DistinguishedName];

            files.DeleteByType(FileTypes.Apply);

            var connectionManager = new ConnectionManager();
            var connectionCache = new Dictionary<string, LdapConnection>();

            try
            {
                foreach (DataGridViewRow row in this.grid.Rows)
                {
                    if (row.Cells[StringLiterals.Action].Value == null)
                    {
                        continue;
                    }

                    // let's convert the action string into one of our known action types
                    if (!Enum.TryParse(row.GetCellString(StringLiterals.Action), true, out ActionType updateAction))
                    {
                        // fail to no action, safest choice
                        updateAction = ActionType.None;
                    }

                    if (updateAction == ActionType.Complete || updateAction == ActionType.None || updateAction == ActionType.Fail)
                    {
                        // there is nothing to do or we already failed on this row so don't try again
                        continue;
                    }

                    // this is the current value for this entity and attribute at the time the scan was done
                    var currentValue = row.GetCellString(StringLiterals.Value);

                    // this is the value, either proposed by us or edited by the user, that we will use to update the attribute for the given entity
                    var updateValue = row.GetCellString(StringLiterals.Update);

                    // this is the attribute name tied to this value
                    var updateAttribute = row.GetCellString(StringLiterals.Attribute);

                    // calculate the server & port combination we need to conduct the update
                    string distinguishedName = row.GetCellString(StringLiterals.DistinguishedName);
                    string domain = distinguishedName.Substring(distinguishedName.IndexOf("dc=", StringComparison.CurrentCultureIgnoreCase));
                    string modificationDomainName = domain.ToLowerInvariant().Replace(",dc=", ".").Replace("dc=", "");
                    string serverName = modificationDomainName;

                    if (SettingsManager.Instance.CurrentDirectoryType == DirectoryType.ActiveDirectory)
                    {
                        serverName = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, modificationDomainName)).FindDomainController().Name;
                        statusDisplay(String.Format("Using server {0} for updating", serverName));
                    }

                    // logic from original application
                    var updatePort = SettingsManager.Instance.Port == 3268 ? 389 : SettingsManager.Instance.Port;

                    // this is the full name used to create the connection
                    var fullServerName = string.Format("{0}:{1}", serverName, updatePort);

                    // let's see if we already have one of these connections available, or else we create one
                    LdapConnection connection;
                    if (connectionCache.ContainsKey(fullServerName))
                    {
                        connection = connectionCache[fullServerName];
                    }
                    else
                    {
                        connection = connectionManager.CreateConnection(fullServerName);
                        connectionCache.Add(fullServerName, connection);
                    }

                    var findObject = new SearchRequest();
                    findObject.DistinguishedName = distinguishedName;
                    findObject.Filter = SettingsManager.Instance.Filter;
                    findObject.Scope = System.DirectoryServices.Protocols.SearchScope.Base;
                    var entries = ((SearchResponse)connection.SendRequest(findObject))?.Entries;
                    if (entries == null || entries.Count != 1)
                    {
                        var count = entries != null ? entries.Count : 0;
                        statusDisplay(StringLiterals.Exception + "Found " + count + " entries when searching for " + distinguishedName + ", expected to find 1. Skipping update operation.");
                        continue;
                    }

                    // this are the requests sent to execute the modify operation
                    List<ModifyRequest> modifyRequest = new List<ModifyRequest>();

                    switch (updateAction)
                    {
                        case ActionType.Edit:
                            if (updateAttribute.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (String.IsNullOrEmpty(updateValue))
                                {
                                    updateValue = String.Empty;
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, updateAttribute, new string[] { currentValue }));
                                }
                                else
                                {
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, updateAttribute, new string[] { currentValue }));
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Add, updateAttribute, new string[] { updateValue }));
                                }
                            }
                            else
                            {
                                if (String.IsNullOrEmpty(updateValue))
                                {
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, updateAttribute, null));
                                }
                                else
                                {
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Replace, updateAttribute, updateValue));
                                }
                            }
                            break;
                        case ActionType.Remove:
                            row.Cells[StringLiterals.Update].Value = String.Empty;
                            if (updateAttribute.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
                            {
                                modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, updateAttribute, new string[] { currentValue }));
                            }
                            else
                            {
                                modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, updateAttribute, null));
                            }
                            break;
                        case ActionType.Undo:
                            if (updateAttribute.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (!String.IsNullOrEmpty(row.GetCellString(StringLiterals.Update)))
                                {
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, updateAttribute, new string[] { updateValue }));
                                }
                                modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Add, updateAttribute, new string[] { currentValue }));
                            }
                            else
                            {
                                if (String.IsNullOrEmpty(currentValue))
                                {
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Delete, updateAttribute, null));
                                }
                                else
                                {
                                    modifyRequest.Add(new ModifyRequest(distinguishedName, DirectoryAttributeOperation.Replace, updateAttribute, currentValue));
                                }
                            }
                            break;
                    }

                    try
                    {
                        // now we execute all of our collected modify requests (1 or 2) here so an error in one allows processing to continue
                        foreach (var request in modifyRequest)
                        {
                            connection.SendRequest(request);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!action.Items.Contains("FAIL"))
                        {
                            action.Items.Add("FAIL");
                        }
                        // mark this row as failed
                        row.Cells[StringLiterals.Action].Value = StringLiterals.Fail;

                        // show a status
                        statusDisplay(string.Format("{0}Update Failed: {1} with error: {2}.", StringLiterals.Exception, distinguishedName, ex.Message));
                    }

                    // we need to write to the apply file
                    if (!row.GetCellString(StringLiterals.Action).Equals(StringLiterals.Fail, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // mark this row as now complete
                        row.Cells[StringLiterals.Action].Value = StringLiterals.Complete;

                        // show a status (which also logs this to the verbose file)
                        statusDisplay(string.Format("Update: [{0}] [{1}] [{2}] [{3}] [{4}] [{5}] [{6}]",
                            distinguishedName,
                            row.GetCellString(StringLiterals.ObjectClass),
                            updateAttribute,
                            row.GetCellString(StringLiterals.Error),
                            currentValue,
                            updateValue,
                            updateAction.ToString()));

                        try
                        {
                            files.AppendTo(FileTypes.Apply, (writer) =>
                            {
                                writer.WriteLine("distinguishedName: " + distinguishedName);
                                writer.WriteLine("objectClass: " + row.GetCellString(StringLiterals.ObjectClass));
                                writer.WriteLine("attribute: " + updateAttribute);
                                writer.WriteLine("error: " + row.GetCellString(StringLiterals.Error));
                                writer.WriteLine("value: " + currentValue);
                                writer.WriteLine("update: " + updateValue);
                                writer.WriteLine("action: " + updateAction.ToString());
                                writer.WriteLine("-");
                                writer.WriteLine();
                            });
                        }
                        catch (Exception err)
                        {
                            statusDisplay(StringLiterals.Exception + StringLiterals.WriteUpdate + "  " + err.Message);
                            throw;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                statusDisplay(string.Format("{0}{1} {2}", StringLiterals.Exception, toolStripStatusLabel1.Text, err.Message));
                throw;
            }
            finally
            {
                foreach (var connPair in connectionCache)
                {
                    connPair.Value.Dispose();
                }
            }

            statusDisplay(StringLiterals.Complete);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                statusDisplay(StringLiterals.ExportFile);
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = StringLiterals.ExportFileFilter;
                    dialog.Title = StringLiterals.ExportFile;
                    dialog.ShowDialog();

                    if (!String.IsNullOrEmpty(dialog.FileName))
                    {
                        using (StreamWriter saveFile = new StreamWriter(dialog.FileName))
                        {
                            switch (dialog.FilterIndex)
                            {
                                case 1:
                                    this.grid.ToCsv(saveFile);
                                    break;
                                case 2:
                                    this.grid.ToLdf(saveFile);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + toolStripStatusLabel1.Text + "  " + ex.Message);
                throw;
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusDisplay(StringLiterals.ImportFile);
            SettingsManager.Instance.DistinguishedName = string.Empty;

            if (SettingsManager.Instance.CurrentRuleMode == RuleMode.MultiTenant)
            {
                this.Text = string.Format(StringLiterals.IdFixVersionFormat, Application.ProductVersion) + StringLiterals.MultiTenant + " - Import";
            }
            else
            {
                this.Text = string.Format(StringLiterals.IdFixVersionFormat, Application.ProductVersion) + StringLiterals.Dedicated + " - Import";
            }

            action.Items.Clear();
            action.Items.Add(StringLiterals.Edit);
            action.Items.Add(StringLiterals.Remove);
            action.Items.Add(StringLiterals.Complete);
            editActionToolStripMenuItem.Visible = true;
            removeActionToolStripMenuItem.Visible = true;
            undoActionToolStripMenuItem.Visible = false;

            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = StringLiterals.ImportFileFilter;
                    openFileDialog.Title = StringLiterals.ImportFile;
                    openFileDialog.ShowDialog();

                    if (!String.IsNullOrEmpty(openFileDialog.FileName))
                    {
                        //First reset firstRun
                        firstRun = false;
                        EnableButtons();

                        using (var reader = new StreamReader(openFileDialog.FileName))
                        {
                            this.grid.SetFromCsv(reader);
                        }

                        statusDisplay(StringLiterals.ActionSelection);
                    }
                }
            }
            catch (Exception err)
            {
                statusDisplay(StringLiterals.Exception + "Import CSV - " + err.Message);
                throw;
            }
        }

        private void undoUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = StringLiterals.UpdateFileFilter;
                    openFileDialog.Title = StringLiterals.UndoUpdates;
                    openFileDialog.ShowDialog();

                    if (!String.IsNullOrEmpty(openFileDialog.FileName))
                    {
                        //First reset firstRun
                        firstRun = false;
                        EnableButtons();
                        statusDisplay(StringLiterals.LoadingUpdates);
                        grid.Columns[StringLiterals.Update].ReadOnly = true;
                        grid.Rows.Clear();
                        action.Items.Clear();
                        action.Items.Add(StringLiterals.Undo);
                        action.Items.Add(StringLiterals.Complete);
                        editActionToolStripMenuItem.Visible = false;
                        removeActionToolStripMenuItem.Visible = false;
                        undoActionToolStripMenuItem.Visible = true;

                        using (StreamReader reader = new StreamReader(openFileDialog.FileName))
                        {
                            this.grid.SetFromLdf(reader, true);
                        }

                        statusDisplay(StringLiterals.ActionSelection);
                    }
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + toolStripStatusLabel1.Text + "  " + ex.Message);
                throw;
            }
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                this.grid.CurrentPage = this.grid.CurrentPage + 1;
                this.SetPagingVisibility();
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.MenuNext + "  " + ex.Message);
                throw;
            }
        }

        private void previousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.grid.CurrentPage > 1)
                {
                    this.grid.CurrentPage = this.grid.CurrentPage - 1;
                    this.SetPagingVisibility();
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.MenuPrevious + "  " + ex.Message);
                throw;
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FormSettings formSettings = new FormSettings(this.statusDisplay);
                formSettings.ShowDialog(this);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.MenuFilter + "  " + ex.Message);
                throw;
            }
        }

        private void feedbackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FormFeedback feedback = new FormFeedback(this);
                feedback.ShowDialog(this);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.MenuFeedback + "  " + ex.Message);
                throw;
            }
        }
        #endregion

        public void statusDisplay(string display)
        {
            try
            {
                toolStripStatusLabel1.Text = display;

                files.AppendTo(FileTypes.Verbose, (writer) =>
                {
                    writer.WriteLine(DateTime.Now.ToString() + " " + toolStripStatusLabel1.Text);
                });
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.Status + "  " + ex.Message);
                throw;
            }
        }

        #region contextMenu
        private void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    contextMenuStrip1.Show();
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.Click + "  " + ex.Message);
                throw;
            }
        }

        private void editActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                multiSelect(StringLiterals.Edit);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.Edit + "  " + ex.Message);
                throw;
            }
        }

        private void removeActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                multiSelect(StringLiterals.Remove);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.Remove + "  " + ex.Message);
                throw;
            }
        }

        private void undoActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                multiSelect(StringLiterals.Undo);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.Undo + "  " + ex.Message);
                throw;
            }
        }

        private void completeActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                multiSelect(StringLiterals.Complete);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.Complete + "  " + ex.Message);
                throw;
            }
        }

        public void multiSelect(string selectAction)
        {
            try
            {
                foreach (DataGridViewRow rowSelected in grid.Rows)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (rowSelected.Cells[i].Selected)
                        {
                            if (rowSelected.Cells[StringLiterals.Action].Value == null)
                            {
                                rowSelected.Cells[StringLiterals.Action].Value = selectAction;
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.MultiSelect + "  " + ex.Message);
                throw;
            }
        }
        #endregion
    }
}
