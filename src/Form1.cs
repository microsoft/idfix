using IdFix.Controls;
using IdFix.Rules;
using IdFix.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IdFix
{
    public partial class Form1 : Form
    {
        Files files = new Files();

        RulesRunner runner;

        internal bool firstRun = false;

        internal const int maxUserNameLength = 64;
        internal const int maxDomainLength = 48;

        public Form1()
        {
            try
            {
                this.firstRun = true; //Only the first time.
                InitializeComponent();
                statusDisplay("Initialized - " + StringLiterals.IdFixVersion);
                MessageBox.Show(StringLiterals.IdFixPrivacyBody,
                        StringLiterals.IdFixPrivacyTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                this.Text = StringLiterals.IdFixVersion;


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
                            statusDisplay(string.Format("Query Count: {0} Error Count: {1} Duplicates: {2} Skipped: {3}", results.TotalProcessed, results.TotalErrors, results.TotalDuplicates, results.TotalSkips));
                            statusDisplay(string.Format("Total Elapsed Time: {0}s", results.TotalElapsed.TotalSeconds));

                            // set the results on our grid which will handle filling itself
                            this.grid.SetResults(results);
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

                /* We can hide them, but we won't.
                acceptToolStripMenuItem.Visible = false;
                applyToolStripMenuItem.Visible = false;
                exportToolStripMenuItem.Visible = false;
                */
            }
            else
            {
                //We need to enable all these buttons
                acceptToolStripMenuItem.Enabled = true;
                applyToolStripMenuItem.Enabled = true;
                exportToolStripMenuItem.Enabled = true;

                /*
                acceptToolStripMenuItem.Visible = true;
                applyToolStripMenuItem.Visible = true;
                exportToolStripMenuItem.Visible = true;
                */
            }

        }

        #region Form1_FormClosed

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
        #endregion

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

                runner.RunWorkerAsync(new RulesRunnerDoWorkArgs() { Files = files });
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
                runner.CancelAsync();
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
                    foreach (DataGridViewRow rowError in grid.Rows)
                    {
                        if (rowError.Cells[StringLiterals.Action].Value == null
                            && !String.IsNullOrEmpty(rowError.Cells[StringLiterals.Update].Value.ToString()))
                        {
                            if (rowError.Cells[StringLiterals.Update].Value.ToString().Length > 3)
                            {
                                switch (rowError.Cells[StringLiterals.Update].Value.ToString().Substring(0, 3))
                                {
                                    case "[C]":
                                        rowError.Cells[StringLiterals.Action].Value = StringLiterals.Complete;
                                        break;
                                    case "[E]":
                                        rowError.Cells[StringLiterals.Action].Value = StringLiterals.Edit;
                                        break;
                                    case "[R]":
                                        rowError.Cells[StringLiterals.Action].Value = StringLiterals.Remove;
                                        break;
                                    default:
                                        rowError.Cells[StringLiterals.Action].Value = StringLiterals.Edit;
                                        break;
                                }
                            }
                            else
                            {
                                rowError.Cells[StringLiterals.Action].Value = StringLiterals.Edit;
                            }
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
            //try
            //{
            //    #region display confirmation and create update file
            //    DialogResult result = MessageBox.Show(StringLiterals.ApplyPendingBody,
            //        StringLiterals.ApplyPending,
            //        MessageBoxButtons.YesNo,
            //        MessageBoxIcon.Question,
            //        MessageBoxDefaultButton.Button2);
            //    if (result != DialogResult.Yes)
            //    {
            //        return;
            //    }
            //    else
            //    {
            //        if (grid.Rows.Count < 1)
            //        {
            //            return;
            //        }
            //    }

            //    statusDisplay(StringLiterals.ApplyPending);
            //    if (grid.Rows.Count > 0)
            //    {
            //        grid.CurrentCell = grid.Rows[0].Cells[StringLiterals.DistinguishedName];
            //    }

            //    string attributeString;
            //    string updateString;
            //    string valueString;
            //    string actionString;
            //    #endregion

            //    foreach (DataGridViewRow rowError in grid.Rows)
            //    {
            //        if (rowError.Cells[StringLiterals.Action].Value != null)
            //        {
            //            #region nothing to do 
            //            actionString = rowError.Cells[StringLiterals.Action].Value.ToString();
            //            if (actionString == StringLiterals.Complete || actionString == StringLiterals.Fail)
            //            {
            //                continue;
            //            }
            //            #endregion

            //            #region get Update, & Value strings
            //            attributeString = rowError.Cells[StringLiterals.Attribute].Value.ToString();
            //            updateString = (rowError.Cells[StringLiterals.Update].Value != null ? rowError.Cells[StringLiterals.Update].Value.ToString() : String.Empty);
            //            if (updateString.Length > 3)
            //            {
            //                switch (updateString.Substring(0, 3))
            //                {
            //                    case "[C]":
            //                        updateString = updateString.Substring(3);
            //                        break;
            //                    case "[E]":
            //                        updateString = updateString.Substring(3);
            //                        break;
            //                    case "[R]":
            //                        updateString = updateString.Substring(3);
            //                        break;
            //                }
            //            }
            //            valueString = (rowError.Cells[StringLiterals.Value].Value != null ? rowError.Cells[StringLiterals.Value].Value.ToString() : String.Empty);
            //            #endregion

            //            # region server, target, port
            //            string dnMod = rowError.Cells[StringLiterals.DistinguishedName].Value.ToString();
            //            string domainMod = dnMod.Substring(dnMod.IndexOf("dc=", StringComparison.CurrentCultureIgnoreCase));
            //            string domainModName = domainMod.ToLowerInvariant().Replace(",dc=", ".").Replace("dc=", "");

            //            string serverName = string.Empty;

            //            if (SettingsManager.Instance.CurrentDirectoryType == DirectoryType.ActiveDirectory)
            //            {
            //                serverName = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, domainModName)).FindDomainController().Name;
            //                statusDisplay(String.Format("Using server {0} for updating", serverName));
            //            }

            //            if (SettingsManager.Instance.Port == 3268)
            //            {
            //                // TODO:: not sure we want to do this, maybe need a local port number
            //                SettingsManager.Instance.Port = 389;
            //            }
            //            #endregion

            //            #region connection
            //            using (LdapConnection connection = new LdapConnection(serverName + ":" + SettingsManager.Instance.Port))
            //            {
            //                #region connection parameters
            //                if (SettingsManager.Instance.Port == 636)
            //                {
            //                    connection.SessionOptions.ProtocolVersion = 3;
            //                    connection.SessionOptions.SecureSocketLayer = true;
            //                    connection.AuthType = AuthType.Negotiate;
            //                }
            //                if (SettingsManager.Instance.CurrentCredentialMode == CredentialMode.Specified)
            //                {
            //                    NetworkCredential credential = new NetworkCredential(SettingsManager.Instance.Username, SettingsManager.Instance.Password);
            //                    connection.Credential = credential;
            //                }
            //                connection.Timeout = TimeSpan.FromSeconds(120);
            //                #endregion

            //                #region get the object
            //                SearchRequest findme = new SearchRequest();
            //                findme.DistinguishedName = dnMod;
            //                findme.Filter = SettingsManager.Instance.Filter;
            //                findme.Scope = System.DirectoryServices.Protocols.SearchScope.Base;
            //                SearchResponse results = (SearchResponse)connection.SendRequest(findme);
            //                SearchResultEntryCollection entries = results.Entries;
            //                SearchResultEntry entry;
            //                if (results.Entries.Count != 1)
            //                {
            //                    statusDisplay(StringLiterals.Exception + "Found " + results.Entries.Count.ToString() + " entries when searching for " + dnMod);
            //                }
            //                else
            //                {
            //                    entry = entries[0];
            //                }
            //                #endregion

            //                #region apply updates
            //                switch (rowError.Cells[StringLiterals.Action].Value.ToString())
            //                {
            //                    case "REMOVE":
            //                        #region Remove
            //                        rowError.Cells[StringLiterals.Update].Value = String.Empty;
            //                        updateString = String.Empty;
            //                        if (attributeString.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
            //                        {
            //                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { valueString });
            //                        }
            //                        else
            //                        {
            //                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, null);
            //                        }
            //                        break;
            //                    #endregion
            //                    case "EDIT":
            //                        #region Edit
            //                        if (attributeString.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
            //                        {
            //                            if (String.IsNullOrEmpty(updateString))
            //                            {
            //                                updateString = String.Empty;
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { valueString });
            //                            }
            //                            else
            //                            {
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { valueString });
            //                                directoryResponse = connection.SendRequest(modifyRequest);
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Add, attributeString, new string[] { updateString });
            //                            }
            //                        }
            //                        else
            //                        {
            //                            if (String.IsNullOrEmpty(updateString))
            //                            {
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, null);
            //                            }
            //                            else
            //                            {
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Replace, attributeString, updateString);
            //                            }
            //                        }
            //                        break;
            //                    #endregion
            //                    case "UNDO":
            //                        #region Undo
            //                        if (attributeString.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
            //                        {
            //                            if (!String.IsNullOrEmpty(rowError.Cells[StringLiterals.Update].Value.ToString()))
            //                            {
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { updateString });
            //                                directoryResponse = connection.SendRequest(modifyRequest);
            //                            }
            //                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Add, attributeString, new string[] { valueString });
            //                        }
            //                        else
            //                        {
            //                            if (String.IsNullOrEmpty(valueString))
            //                            {
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, null);
            //                            }
            //                            else
            //                            {
            //                                modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Replace, attributeString, valueString);
            //                            }
            //                        }
            //                        break;
            //                        #endregion
            //                }
            //                #endregion

            //                #region modifyRequest
            //                try
            //                {
            //                    //DirectoryAttributeModification modifyAttribute = new DirectoryAttributeModification();
            //                    //modifyAttribute.Operation = DirectoryAttributeOperation.Replace;
            //                    //modifyAttribute.Name = "description";
            //                    //modifyAttribute.Add("modified");

            //                    //ModifyRequest modifyRequest = new ModifyRequest(dnMod, modifyAttribute);
            //                    //DirectoryResponse response = connection.SendRequest(modifyRequest);

            //                    //ModifyRequest modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, "proxyAddresses", new string[] {"smtp:modified@e2k10.com"});
            //                    directoryResponse = connection.SendRequest(modifyRequest);
            //                }
            //                catch (Exception ex)
            //                {
            //                    if (!action.Items.Contains("FAIL"))
            //                    {
            //                        action.Items.Add("FAIL");
            //                    }
            //                    statusDisplay(StringLiterals.Exception + "Update Failed: "
            //                        + rowError.Cells[StringLiterals.DistinguishedName].Value.ToString()
            //                        + "  " + ex.Message);
            //                    rowError.Cells[StringLiterals.Action].Value = StringLiterals.Fail;
            //                }
            //                #endregion

            //                #region write update
            //                if (!rowError.Cells[StringLiterals.Action].Value.ToString().Equals(StringLiterals.Fail, StringComparison.CurrentCultureIgnoreCase))
            //                {
            //                    writeUpdate(rowError.Cells[StringLiterals.DistinguishedName].Value.ToString(),
            //                        rowError.Cells[StringLiterals.ObjectClass].Value.ToString(),
            //                        rowError.Cells[StringLiterals.Attribute].Value.ToString(),
            //                        rowError.Cells[StringLiterals.Error].Value.ToString(),
            //                        valueString,
            //                        updateString,
            //                        actionString);

            //                    rowError.Cells[StringLiterals.Action].Value = StringLiterals.Complete;
            //                }
            //                #endregion
            //            }
            //            #endregion
            //        }
            //    }
            //    statusDisplay(StringLiterals.Complete);
            //}
            //catch (Exception ex)
            //{
            //    statusDisplay(StringLiterals.Exception + toolStripStatusLabel1.Text + "  " + ex.Message);
            //    throw;
            //}
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                statusDisplay(StringLiterals.ExportFile);
                using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
                {
                    saveFileDialog1.Filter = StringLiterals.ExportFileFilter;
                    saveFileDialog1.Title = StringLiterals.ExportFile;
                    saveFileDialog1.ShowDialog();
                    string fileNameValue = saveFileDialog1.FileName;
                    if (!String.IsNullOrEmpty(fileNameValue))
                    {
                        using (StreamWriter saveFile = new StreamWriter(saveFileDialog1.FileName))
                        {
                            switch (saveFileDialog1.FilterIndex)
                            {
                                case 1:
                                    #region save to CSV
                                    int cols = grid.Columns.Count;
                                    for (int i = 0; i < cols; i++)
                                    {
                                        saveFile.Write(grid.Columns[i].Name.ToString().ToUpper(CultureInfo.CurrentCulture));
                                        if (i < cols - 1)
                                        {
                                            saveFile.Write(",");
                                        }
                                    }
                                    saveFile.WriteLine();

                                    for (int i = 0; i < grid.Rows.Count; i++)
                                    {
                                        for (int j = 0; j < cols; j++)
                                        {
                                            if (grid.Rows[i].Cells[j].Value != null)
                                            {
                                                if (grid.Rows[i].Cells[j].Value.ToString().IndexOf(",") == -1)
                                                {
                                                    saveFile.Write(grid.Rows[i].Cells[j].Value.ToString());
                                                }
                                                else
                                                {
                                                    saveFile.Write("\"" + grid.Rows[i].Cells[j].Value.ToString());
                                                }
                                            }
                                            if (j < cols - 1)
                                            {
                                                if (grid.Rows[i].Cells[j].Value.ToString().IndexOf(",") == -1)
                                                {
                                                    saveFile.Write(",");
                                                }
                                                else
                                                {
                                                    saveFile.Write("\",");
                                                }
                                            }
                                        }

                                        saveFile.WriteLine();
                                    }
                                    break;
                                #endregion
                                case 2:
                                    #region save to LDF
                                    string vl;
                                    string up;
                                    string at;
                                    foreach (DataGridViewRow rowError in grid.Rows)
                                    {
                                        vl = rowError.Cells[StringLiterals.Value].Value.ToString();
                                        up = rowError.Cells[StringLiterals.Update].Value.ToString();
                                        at = rowError.Cells[StringLiterals.Attribute].Value.ToString();

                                        saveFile.WriteLine("dn: " + rowError.Cells[StringLiterals.DistinguishedName].Value.ToString());
                                        saveFile.WriteLine("changetype: modify");

                                        if (at.ToUpperInvariant() == StringLiterals.ProxyAddresses.ToUpperInvariant())
                                        {
                                            saveFile.WriteLine("delete: " + at);
                                            saveFile.WriteLine(at + ": " + vl);
                                            saveFile.WriteLine("-");
                                            saveFile.WriteLine();
                                            saveFile.WriteLine("dn: " + rowError.Cells[StringLiterals.DistinguishedName].Value.ToString());
                                            saveFile.WriteLine("changetype: modify");
                                            saveFile.WriteLine("add: " + at);
                                        }
                                        else
                                        {
                                            saveFile.WriteLine("replace: " + at);
                                        }

                                        //if (update != String.Empty)
                                        if (!String.IsNullOrEmpty(up))
                                        {
                                            saveFile.WriteLine(at + ": " + up);
                                        }
                                        else
                                        {
                                            saveFile.WriteLine(at + ": " + vl);
                                        }
                                        saveFile.WriteLine("-");
                                        saveFile.WriteLine();
                                    }
                                    break;
                                    #endregion
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
            //try
            //{
            //    statusDisplay(StringLiterals.ImportFile);
            //    SettingsManager.Instance.DistinguishedName = string.Empty;
            //    if (SettingsManager.Instance.CurrentRuleMode == RuleMode.MultiTenant)
            //    {
            //        this.Text = StringLiterals.IdFixVersion + StringLiterals.MultiTenant + " - Import";
            //    }
            //    else
            //    {
            //        this.Text = StringLiterals.IdFixVersion + StringLiterals.Dedicated + " - Import";
            //    }
            //    grid.Rows.Clear();
            //    grid.Refresh();
            //    grid.Columns[StringLiterals.Update].ReadOnly = false;

            //    action.Items.Clear();
            //    action.Items.Add(StringLiterals.Edit);
            //    action.Items.Add(StringLiterals.Remove);
            //    action.Items.Add(StringLiterals.Complete);
            //    editActionToolStripMenuItem.Visible = true;
            //    removeActionToolStripMenuItem.Visible = true;
            //    undoActionToolStripMenuItem.Visible = false;
            //    errDict.Clear();

            //    using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
            //    {
            //        openFileDialog1.Filter = StringLiterals.ImportFileFilter;
            //        openFileDialog1.Title = StringLiterals.ImportFile;
            //        openFileDialog1.ShowDialog();
            //        string fileNameValue = openFileDialog1.FileName;
            //        if (!String.IsNullOrEmpty(fileNameValue))
            //        {
            //            //First reset firstRun
            //            firstRun = false;
            //            EnableButtons();
            //            statusDisplay(StringLiterals.ImportFile);
            //            using (StreamReader reader = new StreamReader(openFileDialog1.FileName))
            //            {
            //                int newRow = 0;
            //                string line;
            //                string col;
            //                String[] cols;
            //                Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            //                #region Import CSV lines
            //                while ((line = reader.ReadLine()) != null)
            //                {
            //                    try
            //                    {
            //                        if (newRow == 0)
            //                        {
            //                            line = reader.ReadLine();
            //                        }
            //                        grid.Rows.Add();

            //                        cols = csvParser.Split(line);
            //                        for (int i = 0; i < 7; i++)
            //                        {
            //                            col = doubleQuotes.Replace(cols[i], "");
            //                            col = col.Replace("\"\"", "\"");
            //                            if (i == 6)
            //                            {
            //                                switch (cols[i])
            //                                {
            //                                    case "EDIT":
            //                                        grid.Rows[newRow].Cells[i].Value = col;
            //                                        break;
            //                                    case "REMOVE":
            //                                        grid.Rows[newRow].Cells[i].Value = col;
            //                                        break;
            //                                    case "COMPLETE":
            //                                        grid.Rows[newRow].Cells[i].Value = col;
            //                                        break;
            //                                    case "UNDO":
            //                                        grid.Rows[newRow].Cells[i].Value = col;
            //                                        break;
            //                                    case "FAIL":
            //                                        grid.Rows[newRow].Cells[i].Value = col;
            //                                        break;
            //                                        //default:
            //                                        //    dataGridView1.Rows[newRow].Cells[i].Value = String.Empty;
            //                                        //    break;
            //                                }
            //                            }
            //                            else
            //                            {
            //                                if (!String.IsNullOrEmpty(cols[i]))
            //                                {
            //                                    grid.Rows[newRow].Cells[i].Value = col;
            //                                }
            //                            }

            //                        }
            //                        newRow++;
            //                    }
            //                    catch (Exception exLine)
            //                    {
            //                        statusDisplay(StringLiterals.Exception + "Import CSV Line: [" + line + "] " + exLine.Message);
            //                    }
            //                }
            //                #endregion

            //                grid.Sort(grid.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
            //                if (grid.RowCount >= 1)
            //                {
            //                    grid.CurrentCell = grid.Rows[0].Cells[StringLiterals.DistinguishedName];
            //                }
            //            }
            //            statusDisplay(StringLiterals.ActionSelection);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    statusDisplay(StringLiterals.Exception + "Import CSV - " + ex.Message);
            //    throw;
            //}
        }

        private void undoUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
            //    {
            //        openFileDialog1.Filter = StringLiterals.UpdateFileFilter;
            //        openFileDialog1.Title = StringLiterals.UndoUpdates;
            //        openFileDialog1.ShowDialog();
            //        string fileNameValue = openFileDialog1.FileName;

            //        #region select Undo
            //        if (!String.IsNullOrEmpty(fileNameValue))
            //        {
            //            //First reset firstRun
            //            firstRun = false;
            //            EnableButtons();
            //            statusDisplay(StringLiterals.LoadingUpdates);
            //            grid.Columns[StringLiterals.Update].ReadOnly = true;
            //            grid.Rows.Clear();
            //            action.Items.Clear();
            //            action.Items.Add(StringLiterals.Undo);
            //            action.Items.Add(StringLiterals.Complete);
            //            editActionToolStripMenuItem.Visible = false;
            //            removeActionToolStripMenuItem.Visible = false;
            //            undoActionToolStripMenuItem.Visible = true;
            //            errDict.Clear();

            //            using (StreamReader reader = new StreamReader(openFileDialog1.FileName))
            //            {
            //                int newRow = 0;
            //                string line;
            //                while ((line = reader.ReadLine()) != null)
            //                {
            //                    if (line.IndexOf(": ", StringComparison.CurrentCulture) > -1)
            //                    {
            //                        switch (line.Substring(0, line.IndexOf(": ", StringComparison.CurrentCulture)))
            //                        {
            //                            case "distinguishedName":
            //                                grid.Rows.Add();
            //                                grid.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
            //                                break;
            //                            case "objectClass":
            //                                grid.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
            //                                break;
            //                            case "attribute":
            //                                grid.Rows[newRow].Cells[StringLiterals.Attribute].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
            //                                break;
            //                            case "error":
            //                                grid.Rows[newRow].Cells[StringLiterals.Error].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
            //                                break;
            //                            case "value":
            //                                grid.Rows[newRow].Cells[StringLiterals.Value].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
            //                                break;
            //                            case "update":
            //                                grid.Rows[newRow].Cells[StringLiterals.Update].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
            //                                newRow++;
            //                                break;
            //                        }
            //                    }
            //                }
            //                grid.Sort(grid.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
            //                if (grid.RowCount >= 1)
            //                {
            //                    grid.CurrentCell = grid.Rows[0].Cells[StringLiterals.DistinguishedName];
            //                }
            //            }
            //            statusDisplay(StringLiterals.ActionSelection);
            //        }
            //        #endregion
            //    }
            //}
            //catch (Exception ex)
            //{
            //    statusDisplay(StringLiterals.Exception + toolStripStatusLabel1.Text + "  " + ex.Message);
            //    throw;
            //}
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    if (displayCount >= errDict.Count)
            //    {
            //        statusDisplay("No more errors");
            //        return;
            //    }

            //    grid.Rows.Clear();
            //    int newRow = 0;
            //    int dictCount = 0;
            //    foreach (KeyValuePair<string, ErrorClass> errorPair in errDict)
            //    {
            //        if (dictCount++ < displayCount)
            //        {
            //            //dictCount++;
            //            continue;
            //        }
            //        grid.Rows.Add();
            //        grid.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = errorPair.Value.distinguishedName;
            //        grid.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = errorPair.Value.objectClass;
            //        grid.Rows[newRow].Cells[StringLiterals.Attribute].Value = errorPair.Value.attribute;
            //        grid.Rows[newRow].Cells[StringLiterals.Error].Value = errorPair.Value.type.Substring(0, errorPair.Value.type.Length - 1);
            //        grid.Rows[newRow].Cells[StringLiterals.Value].Value = errorPair.Value.value;
            //        grid.Rows[newRow].Cells[StringLiterals.Update].Value = errorPair.Value.update;
            //        newRow++;

            //        if (dictCount >= blockSize + displayCount)
            //        {
            //            displayCount = dictCount;
            //            break;
            //        }
            //    }
            //    grid.Sort(grid.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
            //    if (grid.RowCount >= 1)
            //    {
            //        grid.CurrentCell = grid.Rows[0].Cells[StringLiterals.DistinguishedName];
            //    }
            //    statusDisplay(StringLiterals.ElapsedTimePopulateDataGridView + (DateTime.Now - stopwatch).ToString());
            //    statusDisplay("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
            //        + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
            //        + "  Displayed Count: " + grid.Rows.Count.ToString(CultureInfo.CurrentCulture));
            //}
            //catch (Exception ex)
            //{
            //    statusDisplay(StringLiterals.Exception + StringLiterals.MenuNext + "  " + ex.Message);
            //    throw;
            //}
        }

        private void previousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    if (displayCount - blockSize > 0)
            //    {
            //        statusDisplay("Loading errors");
            //    }
            //    else
            //    {
            //        statusDisplay("No more errors");
            //        return;
            //    }

            //    long startCount = displayCount - blockSize - grid.Rows.Count > 0 ? displayCount - blockSize - grid.Rows.Count : 0;
            //    displayCount = startCount;
            //    grid.Rows.Clear();
            //    int newRow = 0;
            //    int dictCount = 0;
            //    foreach (KeyValuePair<string, ErrorClass> errorPair in errDict)
            //    {
            //        if (dictCount < startCount)
            //        {
            //            dictCount++;
            //            continue;
            //        }
            //        grid.Rows.Add();
            //        grid.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = errorPair.Value.distinguishedName;
            //        grid.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = errorPair.Value.objectClass;
            //        grid.Rows[newRow].Cells[StringLiterals.Attribute].Value = errorPair.Value.attribute;
            //        grid.Rows[newRow].Cells[StringLiterals.Error].Value = errorPair.Value.type.Substring(0, errorPair.Value.type.Length - 1);
            //        grid.Rows[newRow].Cells[StringLiterals.Value].Value = errorPair.Value.value;
            //        grid.Rows[newRow].Cells[StringLiterals.Update].Value = errorPair.Value.update;
            //        newRow++;
            //        displayCount++;
            //        if (newRow >= blockSize)
            //        {
            //            break;
            //        }
            //    }
            //    grid.Sort(grid.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
            //    if (grid.RowCount >= 1)
            //    {
            //        grid.CurrentCell = grid.Rows[0].Cells[StringLiterals.DistinguishedName];
            //    }
            //    statusDisplay(StringLiterals.ElapsedTimePopulateDataGridView + (DateTime.Now - stopwatch).ToString());
            //    statusDisplay("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
            //        + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
            //        + "  Displayed Count: " + grid.Rows.Count.ToString(CultureInfo.CurrentCulture));
            //}
            //catch (Exception ex)
            //{
            //    statusDisplay(StringLiterals.Exception + StringLiterals.MenuPrevious + "  " + ex.Message);
            //    throw;
            //}
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

        #region file actions
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

        private void writeUpdate(string wuDistinguishedName, string wuObjectClass, string wuAttribute, string wuError, string wuValue, string wuUpdate, string wuAction)
        {
            try
            {
                statusDisplay("Update: [" + wuDistinguishedName + "]"
                    + "[" + wuObjectClass + "]"
                    + "[" + wuAttribute + "]"
                    + "[" + wuError + "]"
                    + "[" + wuValue + "]"
                    + "[" + wuUpdate + "]"
                    + "[" + wuAction + "]");

                if (wuAction == StringLiterals.Remove || wuAction == StringLiterals.Edit || wuAction == StringLiterals.Undo)
                {
                    files.AppendTo(FileTypes.Apply, (writer) =>
                    {
                        writer.WriteLine("distinguishedName: " + wuDistinguishedName);
                        writer.WriteLine("objectClass: " + wuObjectClass);
                        writer.WriteLine("attribute: " + wuAttribute);
                        writer.WriteLine("error: " + wuError);
                        writer.WriteLine("value: " + wuValue);
                        writer.WriteLine("update: " + wuUpdate);
                        writer.WriteLine("action: " + wuAction);
                        writer.WriteLine("-");
                        writer.WriteLine();
                    });
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.WriteUpdate + "  " + ex.Message);
                throw;
            }
        }
        #endregion

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
