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
        #region members
        ValidTLDList tldList = null;
        Files files = new Files();

        Dictionary<string, long> dupDict = new Dictionary<string, long>();
        Dictionary<string, DuplicateClass> dupObjDict = new Dictionary<string, DuplicateClass>();
        Dictionary<string, ErrorClass> errDict = new Dictionary<string, ErrorClass>();

        int blockSize = 50000;
        long entryCount;
        long errorCount;
        long duplicateCount;
        long displayCount;

        ModifyRequest modifyRequest;
        DirectoryResponse directoryResponse;
        public string[] attributesToReturn = new string[] { StringLiterals.Cn,
                    StringLiterals.DisplayName, //Added in 1.11
                    StringLiterals.DistinguishedName,
                    StringLiterals.GivenName, //Added in 1.11
                    StringLiterals.GroupType, StringLiterals.HomeMdb, StringLiterals.IsCriticalSystemObject, StringLiterals.Mail,
                    StringLiterals.MailNickName, StringLiterals.MsExchHideFromAddressLists,
                    StringLiterals.MsExchRecipientTypeDetails, StringLiterals.ObjectClass, StringLiterals.ProxyAddresses,
                    StringLiterals.SamAccountName, StringLiterals.TargetAddress, StringLiterals.UserPrincipalName };
        Regex doubleQuotes = new Regex("^[\"]+|[\"]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Regex smtp = new Regex("smtp:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Regex periods = new Regex(@"^((?!^\.|\.$).)*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Regex domainPart = new Regex(@"@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[a-z]{2,17})$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Regex localPart = new Regex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Regex rfc2822 = new Regex(@"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[a-z]{2,17})$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        //UPN Regular Expression
        //Regex invalidUpnRegEx = new Regex(@"([\s\\% &*+/=?{}|<> ()\;\:\,\[\]""äëïöüÿÄËÏÖÜŸ])|(^@)|(\s+)|([@\.&\s]$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); 
        //Apparently rfc2822 covers umlauts.
        //Regex invalidUpnRegEx = new Regex(@"([\s\\% &*+/=?{}|<> ()\;\:\,\[\]""])|(^@)|(\s+)|([@\.&\s]$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        //Previous : [\s\\%&*+/=?`{}|<>()\;\:\,\[\]""]
        //Regex invalidUpnRegEx = new Regex(@"[\s\\% &*+/=?{}|<> ()\;\:\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        //Regex oldinvalidUpnRegEx = new Regex(@"[\s\\%&*+/=?`{}|<>()\;\:\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        //Regex      invalidUpnRegEx = new Regex(@"[\s\\%&*+/=?{}|<>()\;\:\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Regex invalidUpnRegEx = new Regex(@"[\s\\%&*+/=?{}|<>()\;\:\,\[\]""äëïöüÿÄËÏÖÜŸ]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        DateTime stopwatch;
        

        internal bool firstRun = false;

        internal const int maxUserNameLength = 64;
        internal const int maxDomainLength = 48;


        RulesRunner runner;




        #endregion

        #region Form1
        public Form1()
        {
            try
            {
                this.firstRun = true; //Only the first time.
                InitializeComponent();

                this.Text = StringLiterals.IdFixVersion;
                statusDisplay("Initialized - " + StringLiterals.IdFixVersion);
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
                statusDisplay("Loading TopLevelDomain List");
                tldList = new ValidTLDList();
                statusDisplay("Ready");

                runner = new RulesRunner();
                runner.OnStatusUpdate += (string message) => {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        statusDisplay(message);
                    });
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

                dataGridView1.Rows.Clear();
                dataGridView1.Refresh();
                dataGridView1.Columns[StringLiterals.Update].ReadOnly = false;

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

                var runner = new RulesRunner();
                runner.OnStatusUpdate += (string message) => this.BeginInvoke((MethodInvoker)delegate
                {
                    statusDisplay(message);
                });

                runner.RunWorkerAsync(new RulesRunnerDoWorkArgs() { Files = files });

                // backgroundWorker1.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + toolStripStatusLabel1.Text + "  " + ex.Message);
                throw;
            }
        }

        private void Runner_OnStatusUpdate(string message)
        {
            throw new NotImplementedException();
        }

        private void cancelQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    cancelToolStripMenuItem.Enabled = false;
                });
                backgroundWorker1.CancelAsync();
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
                    foreach (DataGridViewRow rowError in dataGridView1.Rows)
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
            try
            {
                #region display confirmation and create update file
                DialogResult result = MessageBox.Show(StringLiterals.ApplyPendingBody,
                    StringLiterals.ApplyPending,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    return;
                }
                else
                {
                    if (dataGridView1.Rows.Count < 1)
                    {
                        return;
                    }
                }

                statusDisplay(StringLiterals.ApplyPending);
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[StringLiterals.DistinguishedName];
                }

                string attributeString;
                string updateString;
                string valueString;
                string actionString;
                #endregion

                foreach (DataGridViewRow rowError in dataGridView1.Rows)
                {
                    if (rowError.Cells[StringLiterals.Action].Value != null)
                    {
                        #region nothing to do 
                        actionString = rowError.Cells[StringLiterals.Action].Value.ToString();
                        if (actionString == StringLiterals.Complete || actionString == StringLiterals.Fail)
                        {
                            continue;
                        }
                        #endregion

                        #region get Update, & Value strings
                        attributeString = rowError.Cells[StringLiterals.Attribute].Value.ToString();
                        updateString = (rowError.Cells[StringLiterals.Update].Value != null ? rowError.Cells[StringLiterals.Update].Value.ToString() : String.Empty);
                        if (updateString.Length > 3)
                        {
                            switch (updateString.Substring(0, 3))
                            {
                                case "[C]":
                                    updateString = updateString.Substring(3);
                                    break;
                                case "[E]":
                                    updateString = updateString.Substring(3);
                                    break;
                                case "[R]":
                                    updateString = updateString.Substring(3);
                                    break;
                            }
                        }
                        valueString = (rowError.Cells[StringLiterals.Value].Value != null ? rowError.Cells[StringLiterals.Value].Value.ToString() : String.Empty);
                        #endregion

                        # region server, target, port
                        string dnMod = rowError.Cells[StringLiterals.DistinguishedName].Value.ToString();
                        string domainMod = dnMod.Substring(dnMod.IndexOf("dc=", StringComparison.CurrentCultureIgnoreCase));
                        string domainModName = domainMod.ToLowerInvariant().Replace(",dc=", ".").Replace("dc=", "");

                        string serverName = string.Empty;

                        if (SettingsManager.Instance.CurrentDirectoryType == DirectoryType.ActiveDirectory)
                        {
                            serverName = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, domainModName)).FindDomainController().Name;
                            statusDisplay(String.Format("Using server {0} for updating", serverName));
                        }

                        if (SettingsManager.Instance.Port == 3268)
                        {
                            // TODO:: not sure we want to do this, maybe need a local port number
                            SettingsManager.Instance.Port = 389;
                        }
                        #endregion

                        #region connection
                        using (LdapConnection connection = new LdapConnection(serverName + ":" + SettingsManager.Instance.Port))
                        {
                            #region connection parameters
                            if (SettingsManager.Instance.Port == 636)
                            {
                                connection.SessionOptions.ProtocolVersion = 3;
                                connection.SessionOptions.SecureSocketLayer = true;
                                connection.AuthType = AuthType.Negotiate;
                            }
                            if (SettingsManager.Instance.CurrentCredentialMode == CredentialMode.Specified)
                            {
                                NetworkCredential credential = new NetworkCredential(SettingsManager.Instance.Username, SettingsManager.Instance.Password);
                                connection.Credential = credential;
                            }
                            connection.Timeout = TimeSpan.FromSeconds(120);
                            #endregion

                            #region get the object
                            SearchRequest findme = new SearchRequest();
                            findme.DistinguishedName = dnMod;
                            findme.Filter = SettingsManager.Instance.Filter;
                            findme.Scope = System.DirectoryServices.Protocols.SearchScope.Base;
                            SearchResponse results = (SearchResponse)connection.SendRequest(findme);
                            SearchResultEntryCollection entries = results.Entries;
                            SearchResultEntry entry;
                            if (results.Entries.Count != 1)
                            {
                                statusDisplay(StringLiterals.Exception + "Found " + results.Entries.Count.ToString() + " entries when searching for " + dnMod);
                            }
                            else
                            {
                                entry = entries[0];
                            }
                            #endregion

                            #region apply updates
                            switch (rowError.Cells[StringLiterals.Action].Value.ToString())
                            {
                                case "REMOVE":
                                    #region Remove
                                    rowError.Cells[StringLiterals.Update].Value = String.Empty;
                                    updateString = String.Empty;
                                    if (attributeString.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { valueString });
                                    }
                                    else
                                    {
                                        modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, null);
                                    }
                                    break;
                                #endregion
                                case "EDIT":
                                    #region Edit
                                    if (attributeString.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        if (String.IsNullOrEmpty(updateString))
                                        {
                                            updateString = String.Empty;
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { valueString });
                                        }
                                        else
                                        {
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { valueString });
                                            directoryResponse = connection.SendRequest(modifyRequest);
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Add, attributeString, new string[] { updateString });
                                        }
                                    }
                                    else
                                    {
                                        if (String.IsNullOrEmpty(updateString))
                                        {
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, null);
                                        }
                                        else
                                        {
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Replace, attributeString, updateString);
                                        }
                                    }
                                    break;
                                #endregion
                                case "UNDO":
                                    #region Undo
                                    if (attributeString.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        if (!String.IsNullOrEmpty(rowError.Cells[StringLiterals.Update].Value.ToString()))
                                        {
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, new string[] { updateString });
                                            directoryResponse = connection.SendRequest(modifyRequest);
                                        }
                                        modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Add, attributeString, new string[] { valueString });
                                    }
                                    else
                                    {
                                        if (String.IsNullOrEmpty(valueString))
                                        {
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, attributeString, null);
                                        }
                                        else
                                        {
                                            modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Replace, attributeString, valueString);
                                        }
                                    }
                                    break;
                                    #endregion
                            }
                            #endregion

                            #region modifyRequest
                            try
                            {
                                //DirectoryAttributeModification modifyAttribute = new DirectoryAttributeModification();
                                //modifyAttribute.Operation = DirectoryAttributeOperation.Replace;
                                //modifyAttribute.Name = "description";
                                //modifyAttribute.Add("modified");

                                //ModifyRequest modifyRequest = new ModifyRequest(dnMod, modifyAttribute);
                                //DirectoryResponse response = connection.SendRequest(modifyRequest);

                                //ModifyRequest modifyRequest = new ModifyRequest(dnMod, DirectoryAttributeOperation.Delete, "proxyAddresses", new string[] {"smtp:modified@e2k10.com"});
                                directoryResponse = connection.SendRequest(modifyRequest);
                            }
                            catch (Exception ex)
                            {
                                if (!action.Items.Contains("FAIL"))
                                {
                                    action.Items.Add("FAIL");
                                }
                                statusDisplay(StringLiterals.Exception + "Update Failed: "
                                    + rowError.Cells[StringLiterals.DistinguishedName].Value.ToString()
                                    + "  " + ex.Message);
                                rowError.Cells[StringLiterals.Action].Value = StringLiterals.Fail;
                            }
                            #endregion

                            #region write update
                            if (!rowError.Cells[StringLiterals.Action].Value.ToString().Equals(StringLiterals.Fail, StringComparison.CurrentCultureIgnoreCase))
                            {
                                writeUpdate(rowError.Cells[StringLiterals.DistinguishedName].Value.ToString(),
                                    rowError.Cells[StringLiterals.ObjectClass].Value.ToString(),
                                    rowError.Cells[StringLiterals.Attribute].Value.ToString(),
                                    rowError.Cells[StringLiterals.Error].Value.ToString(),
                                    valueString,
                                    updateString,
                                    actionString);

                                rowError.Cells[StringLiterals.Action].Value = StringLiterals.Complete;
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
                statusDisplay(StringLiterals.Complete);
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + toolStripStatusLabel1.Text + "  " + ex.Message);
                throw;
            }
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
                                    int cols = dataGridView1.Columns.Count;
                                    for (int i = 0; i < cols; i++)
                                    {
                                        saveFile.Write(dataGridView1.Columns[i].Name.ToString().ToUpper(CultureInfo.CurrentCulture));
                                        if (i < cols - 1)
                                        {
                                            saveFile.Write(",");
                                        }
                                    }
                                    saveFile.WriteLine();

                                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                                    {
                                        for (int j = 0; j < cols; j++)
                                        {
                                            if (dataGridView1.Rows[i].Cells[j].Value != null)
                                            {
                                                if (dataGridView1.Rows[i].Cells[j].Value.ToString().IndexOf(",") == -1)
                                                {
                                                    saveFile.Write(dataGridView1.Rows[i].Cells[j].Value.ToString());
                                                }
                                                else
                                                {
                                                    saveFile.Write("\"" + dataGridView1.Rows[i].Cells[j].Value.ToString());
                                                }
                                            }
                                            if (j < cols - 1)
                                            {
                                                if (dataGridView1.Rows[i].Cells[j].Value.ToString().IndexOf(",") == -1)
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
                                    foreach (DataGridViewRow rowError in dataGridView1.Rows)
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
            try
            {
                statusDisplay(StringLiterals.ImportFile);
                SettingsManager.Instance.DistinguishedName = string.Empty;
                if (SettingsManager.Instance.CurrentRuleMode == RuleMode.MultiTenant)
                {
                    this.Text = StringLiterals.IdFixVersion + StringLiterals.MultiTenant + " - Import";
                }
                else
                {
                    this.Text = StringLiterals.IdFixVersion + StringLiterals.Dedicated + " - Import";
                }
                dataGridView1.Rows.Clear();
                dataGridView1.Refresh();
                dataGridView1.Columns[StringLiterals.Update].ReadOnly = false;

                action.Items.Clear();
                action.Items.Add(StringLiterals.Edit);
                action.Items.Add(StringLiterals.Remove);
                action.Items.Add(StringLiterals.Complete);
                editActionToolStripMenuItem.Visible = true;
                removeActionToolStripMenuItem.Visible = true;
                undoActionToolStripMenuItem.Visible = false;
                errDict.Clear();

                using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
                {
                    openFileDialog1.Filter = StringLiterals.ImportFileFilter;
                    openFileDialog1.Title = StringLiterals.ImportFile;
                    openFileDialog1.ShowDialog();
                    string fileNameValue = openFileDialog1.FileName;
                    if (!String.IsNullOrEmpty(fileNameValue))
                    {
                        //First reset firstRun
                        firstRun = false;
                        EnableButtons();
                        statusDisplay(StringLiterals.ImportFile);
                        using (StreamReader reader = new StreamReader(openFileDialog1.FileName))
                        {
                            int newRow = 0;
                            string line;
                            string col;
                            String[] cols;
                            Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                            #region Import CSV lines
                            while ((line = reader.ReadLine()) != null)
                            {
                                try
                                {
                                    if (newRow == 0)
                                    {
                                        line = reader.ReadLine();
                                    }
                                    dataGridView1.Rows.Add();

                                    cols = csvParser.Split(line);
                                    for (int i = 0; i < 7; i++)
                                    {
                                        col = doubleQuotes.Replace(cols[i], "");
                                        col = col.Replace("\"\"", "\"");
                                        if (i == 6)
                                        {
                                            switch (cols[i])
                                            {
                                                case "EDIT":
                                                    dataGridView1.Rows[newRow].Cells[i].Value = col;
                                                    break;
                                                case "REMOVE":
                                                    dataGridView1.Rows[newRow].Cells[i].Value = col;
                                                    break;
                                                case "COMPLETE":
                                                    dataGridView1.Rows[newRow].Cells[i].Value = col;
                                                    break;
                                                case "UNDO":
                                                    dataGridView1.Rows[newRow].Cells[i].Value = col;
                                                    break;
                                                case "FAIL":
                                                    dataGridView1.Rows[newRow].Cells[i].Value = col;
                                                    break;
                                                    //default:
                                                    //    dataGridView1.Rows[newRow].Cells[i].Value = String.Empty;
                                                    //    break;
                                            }
                                        }
                                        else
                                        {
                                            if (!String.IsNullOrEmpty(cols[i]))
                                            {
                                                dataGridView1.Rows[newRow].Cells[i].Value = col;
                                            }
                                        }

                                    }
                                    newRow++;
                                }
                                catch (Exception exLine)
                                {
                                    statusDisplay(StringLiterals.Exception + "Import CSV Line: [" + line + "] " + exLine.Message);
                                }
                            }
                            #endregion

                            dataGridView1.Sort(dataGridView1.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
                            if (dataGridView1.RowCount >= 1)
                            {
                                dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[StringLiterals.DistinguishedName];
                            }
                        }
                        statusDisplay(StringLiterals.ActionSelection);
                    }
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + "Import CSV - " + ex.Message);
                throw;
            }
        }

        private void undoUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
                {
                    openFileDialog1.Filter = StringLiterals.UpdateFileFilter;
                    openFileDialog1.Title = StringLiterals.UndoUpdates;
                    openFileDialog1.ShowDialog();
                    string fileNameValue = openFileDialog1.FileName;

                    #region select Undo
                    if (!String.IsNullOrEmpty(fileNameValue))
                    {
                        //First reset firstRun
                        firstRun = false;
                        EnableButtons();
                        statusDisplay(StringLiterals.LoadingUpdates);
                        dataGridView1.Columns[StringLiterals.Update].ReadOnly = true;
                        dataGridView1.Rows.Clear();
                        action.Items.Clear();
                        action.Items.Add(StringLiterals.Undo);
                        action.Items.Add(StringLiterals.Complete);
                        editActionToolStripMenuItem.Visible = false;
                        removeActionToolStripMenuItem.Visible = false;
                        undoActionToolStripMenuItem.Visible = true;
                        errDict.Clear();

                        using (StreamReader reader = new StreamReader(openFileDialog1.FileName))
                        {
                            int newRow = 0;
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.IndexOf(": ", StringComparison.CurrentCulture) > -1)
                                {
                                    switch (line.Substring(0, line.IndexOf(": ", StringComparison.CurrentCulture)))
                                    {
                                        case "distinguishedName":
                                            dataGridView1.Rows.Add();
                                            dataGridView1.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                                            break;
                                        case "objectClass":
                                            dataGridView1.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                                            break;
                                        case "attribute":
                                            dataGridView1.Rows[newRow].Cells[StringLiterals.Attribute].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                                            break;
                                        case "error":
                                            dataGridView1.Rows[newRow].Cells[StringLiterals.Error].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                                            break;
                                        case "value":
                                            dataGridView1.Rows[newRow].Cells[StringLiterals.Value].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                                            break;
                                        case "update":
                                            dataGridView1.Rows[newRow].Cells[StringLiterals.Update].Value = line.Substring(line.IndexOf(": ", StringComparison.CurrentCulture) + 2);
                                            newRow++;
                                            break;
                                    }
                                }
                            }
                            dataGridView1.Sort(dataGridView1.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
                            if (dataGridView1.RowCount >= 1)
                            {
                                dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[StringLiterals.DistinguishedName];
                            }
                        }
                        statusDisplay(StringLiterals.ActionSelection);
                    }
                    #endregion
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
                if (displayCount >= errDict.Count)
                {
                    statusDisplay("No more errors");
                    return;
                }

                dataGridView1.Rows.Clear();
                int newRow = 0;
                int dictCount = 0;
                foreach (KeyValuePair<string, ErrorClass> errorPair in errDict)
                {
                    if (dictCount++ < displayCount)
                    {
                        //dictCount++;
                        continue;
                    }
                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = errorPair.Value.distinguishedName;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = errorPair.Value.objectClass;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Attribute].Value = errorPair.Value.attribute;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Error].Value = errorPair.Value.type.Substring(0, errorPair.Value.type.Length - 1);
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Value].Value = errorPair.Value.value;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Update].Value = errorPair.Value.update;
                    newRow++;

                    if (dictCount >= blockSize + displayCount)
                    {
                        displayCount = dictCount;
                        break;
                    }
                }
                dataGridView1.Sort(dataGridView1.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
                if (dataGridView1.RowCount >= 1)
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[StringLiterals.DistinguishedName];
                }
                statusDisplay(StringLiterals.ElapsedTimePopulateDataGridView + (DateTime.Now - stopwatch).ToString());
                statusDisplay("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
                    + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
                    + "  Displayed Count: " + dataGridView1.Rows.Count.ToString(CultureInfo.CurrentCulture));
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
                if (displayCount - blockSize > 0)
                {
                    statusDisplay("Loading errors");
                }
                else
                {
                    statusDisplay("No more errors");
                    return;
                }

                long startCount = displayCount - blockSize - dataGridView1.Rows.Count > 0 ? displayCount - blockSize - dataGridView1.Rows.Count : 0;
                displayCount = startCount;
                dataGridView1.Rows.Clear();
                int newRow = 0;
                int dictCount = 0;
                foreach (KeyValuePair<string, ErrorClass> errorPair in errDict)
                {
                    if (dictCount < startCount)
                    {
                        dictCount++;
                        continue;
                    }
                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = errorPair.Value.distinguishedName;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = errorPair.Value.objectClass;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Attribute].Value = errorPair.Value.attribute;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Error].Value = errorPair.Value.type.Substring(0, errorPair.Value.type.Length - 1);
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Value].Value = errorPair.Value.value;
                    dataGridView1.Rows[newRow].Cells[StringLiterals.Update].Value = errorPair.Value.update;
                    newRow++;
                    displayCount++;
                    if (newRow >= blockSize)
                    {
                        break;
                    }
                }
                dataGridView1.Sort(dataGridView1.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
                if (dataGridView1.RowCount >= 1)
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[StringLiterals.DistinguishedName];
                }
                statusDisplay(StringLiterals.ElapsedTimePopulateDataGridView + (DateTime.Now - stopwatch).ToString());
                statusDisplay("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
                    + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
                    + "  Displayed Count: " + dataGridView1.Rows.Count.ToString(CultureInfo.CurrentCulture));
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

        #region checks
        private void mtChecks(SearchResultEntry entry, string errorAttribute, int errorIndex, int errorLength,
            Regex errorCharacter, Regex errorFormat, bool errorDuplicate, bool errorBlank, bool errorBlankValue)
        {
            try
            {
                bool errorAdd = false;
                string errorUpdate = String.Empty;
                string errorString = String.Empty;

                if (entry.Attributes.Contains(errorAttribute))
                {
                    errorUpdate = entry.Attributes[errorAttribute][errorIndex].ToString();

                    #region errorCharacter
                    if (errorCharacter != null)
                    {
                        if (errorCharacter.IsMatch(errorUpdate))
                        {
                            errorAdd = true;
                            errorString += StringLiterals.Character;
                            errorUpdate = String.IsNullOrEmpty(errorCharacter.Replace(errorUpdate, ""))
                                ? entry.Attributes[StringLiterals.Cn][0].ToString()
                                : errorCharacter.Replace(errorUpdate, "");
                        }

                        //Additionally if the attribute is either ProxyAddresses or TargetAddress
                        //We get rid of the ":" in the suffix part of the value.
                        if (errorAttribute.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase) ||
                            errorAttribute.Equals(StringLiterals.TargetAddress, StringComparison.CurrentCultureIgnoreCase)
                          )
                        {
                            int colonPosn = errorUpdate.IndexOf(":");
                            if (colonPosn > 0)
                            {
                                //In case the suffix has a colon, we need to show a character error.
                                //And replace it.
                                string suffix = errorUpdate.Substring(colonPosn + 1);
                                if (suffix.Contains(":"))
                                {
                                    errorAdd = true;
                                    errorString += StringLiterals.Character;

                                    errorUpdate =
                                        errorUpdate.Substring(0, colonPosn) + ":" +
                                        suffix.Replace(":", "");
                                }
                            }
                            else
                            {
                                //If the format has to be X:Y, then add a format error here
                            }
                        }
                    }
                    #endregion

                    #region errorFormat
                    if (errorFormat != null)
                    {
                        string validateAttribute = smtp.IsMatch(errorUpdate)
                            ? errorUpdate.Substring(errorUpdate.IndexOf(":") + 1)
                            : errorUpdate;

                        #region periods
                        if (errorFormat == periods && !errorFormat.IsMatch(validateAttribute))
                        {
                            errorAdd = true;
                            errorString += StringLiterals.Format;
                            errorUpdate = String.IsNullOrEmpty(new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, ""))
                                ? entry.Attributes[StringLiterals.Cn][0].ToString()
                                : new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, "");
                            errorUpdate = String.IsNullOrEmpty(new Regex(@"\.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, ""))
                                ? entry.Attributes[StringLiterals.Cn][0].ToString()
                                : new Regex(@"\.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, "");
                        }
                        #endregion

                        #region rfc2822
                        if (errorFormat == rfc2822)
                        {
                            #region topleveldomain
                            if (validateAttribute.LastIndexOf(".") != -1)
                            {
                                string tldDomain = validateAttribute.ToLowerInvariant().Substring(validateAttribute.LastIndexOf("."));
                                if (tldDomain.Length > 1)
                                {
                                    tldDomain = tldDomain.Substring(tldDomain.IndexOf(".") + 1);
                                    if (!tldList.Contains(tldDomain))
                                    {
                                        errorAdd = true;
                                        errorString += StringLiterals.TopLevelDomain;
                                    }
                                }
                            }
                            #endregion

                            #region domainpart
                            if (!domainPart.IsMatch(validateAttribute))
                            {
                                errorAdd = true;
                                errorString += StringLiterals.DomainPart;
                                if (validateAttribute.LastIndexOf("@") != -1)
                                {
                                    validateAttribute = validateAttribute.Substring(0, validateAttribute.LastIndexOf("@") + 1)
                                        + (new Regex(@"[^a-z0-9.-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(validateAttribute.LastIndexOf("@") + 1), "");
                                }
                            }
                            #endregion

                            #region localpart
                            if (!localPart.IsMatch(validateAttribute) || (validateAttribute.Split('@').Length - 1 > 1))
                            {
                                errorAdd = true;
                                errorString += StringLiterals.LocalPart;
                                if (validateAttribute.LastIndexOf("@") != -1)
                                {
                                    string validateLocal = (new Regex(@"[^a-z0-9.!#$%&'*+/=?^_`{|}~-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(0, validateAttribute.LastIndexOf("@")), "");
                                    validateAttribute = (validateLocal + validateAttribute.Substring(validateAttribute.LastIndexOf("@"))).Replace(".@", "@");
                                }
                            }
                            #endregion

                            #region other
                            if (!rfc2822.IsMatch(validateAttribute))
                            {
                                errorAdd = true;
                                errorString += StringLiterals.Format;
                            }
                            #endregion

                            errorUpdate = smtp.IsMatch(errorUpdate)
                                ? errorUpdate.Substring(0, errorUpdate.IndexOf(":") + 1).Trim() + validateAttribute
                                : validateAttribute;
                        }
                        #endregion

                        #region other format errors
                        if (!errorFormat.IsMatch(validateAttribute))
                        {
                            if (errorFormat != periods && errorFormat != rfc2822)
                            {
                                errorAdd = true;
                                errorString += StringLiterals.Format;
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region errorLength
                    if (entry.Attributes[errorAttribute][errorIndex].ToString().Length > errorLength)
                    {
                        errorAdd = true;
                        errorString += StringLiterals.Length;
                        errorUpdate = errorUpdate.Length > errorLength ? errorUpdate.Substring(0, errorLength) : errorUpdate;
                    }

                    //For UPN, we also need to check the length of characters before and after @
                    if (errorAttribute.Equals(StringLiterals.UserPrincipalName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        string upn = entry.Attributes[errorAttribute][errorIndex].ToString();
                        int atPosn = upn.LastIndexOf("@", StringComparison.CurrentCulture);
                        //
                        if (atPosn != -1)
                        {
                            string upnUser = upn.Substring(0, atPosn);
                            if (upnUser.Length > maxUserNameLength)
                            {
                                errorAdd = true;
                                errorString += "userlength,";
                                upnUser = upn.Substring(0, maxUserNameLength);
                            }
                            string upnDomain = upn.Substring(atPosn + 1);
                            if (upnDomain.Length > maxDomainLength)
                            {
                                errorAdd = true;
                                errorString += "domainlength,";
                                upnDomain = upn.Substring(atPosn + 1, maxDomainLength);
                            }
                            //Since we are splitting out the user/domain part and combining, we need to replace
                            //@ & . as special characters.
                            upnUser = new Regex(@"[@.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(upnUser, "");
                            errorUpdate = errorCharacter.Replace(upnUser + "@" + upnDomain, "");
                        }
                        //We don't need the else part because rfc2822 check above looks for match of local & domain part.
                        //userNamePart has to end with @, domainPart has to begin with @

                    }
                    #endregion

                    #region errorDuplicate
                    if (errorDuplicate)
                    {
                        #region update suggestion
                        string dupUpdate = String.Empty;
                        if (errorUpdate == entry.Attributes[errorAttribute][errorIndex].ToString())
                        {
                            switch (errorAttribute.ToLowerInvariant())
                            {
                                case "proxyaddresses":
                                    if (smtp.IsMatch(errorUpdate))
                                    {
                                        if (entry.Attributes.Contains(StringLiterals.MailNickName))
                                        {
                                            if (errorUpdate.Substring(0, 5) == "SMTP:")
                                            {
                                                dupUpdate = "[C]" + errorUpdate;
                                            }
                                            else
                                            {
                                                dupUpdate = "[E]" + errorUpdate;
                                            }
                                        }
                                        else
                                        {
                                            dupUpdate = "[R]" + errorUpdate;
                                        }
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                                case "userprincipalname":
                                    if (entry.Attributes.Contains(StringLiterals.MailNickName))
                                    {
                                        dupUpdate = "[C]" + errorUpdate;
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                                case "mail":
                                    if (entry.Attributes.Contains(StringLiterals.MailNickName))
                                    {
                                        dupUpdate = "[C]" + errorUpdate;
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                                case "mailnickname":
                                    if (entry.Attributes.Contains(StringLiterals.HomeMdb))
                                    {
                                        dupUpdate = "[C]" + errorUpdate;
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                                case "samaccountname":
                                    if (entry.Attributes.Contains(StringLiterals.MailNickName))
                                    {
                                        dupUpdate = "[C]" + errorUpdate;
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                            }
                        }
                        #endregion

                        files.AppendTo(FileTypes.Duplicate, (writer) => {
                            writer.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                            writer.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                            writer.WriteLine("AT:" + errorAttribute);
                            writer.WriteLine("VL:" + entry.Attributes[errorAttribute][errorIndex].ToString());
                            writer.WriteLine("UP:" + dupUpdate);
                            writer.WriteLine("---");
                        });
                        duplicateCount++;
                    }
                    #endregion

                    #region errorBlankValue
                    //If the attribute present and it shouldn't be empty
                    if (errorBlankValue && String.IsNullOrEmpty(entry.Attributes[errorAttribute][errorIndex].ToString()))
                    {
                        if (String.IsNullOrEmpty(errorUpdate))
                        {
                            errorUpdate += entry.Attributes[StringLiterals.Cn][0].ToString();
                        }
                        errorUpdate = errorUpdate.Length > errorLength
                            ? errorUpdate.Substring(0, errorLength)
                            : errorUpdate;
                        errorString += StringLiterals.Blank;
                        errorAdd = true;
                    }
                    #endregion
                }
                else if (errorBlank)
                {
                    #region errorBlank
                    if (String.IsNullOrEmpty(errorUpdate))
                    {
                        errorUpdate += entry.Attributes[StringLiterals.Cn][0].ToString();
                    }
                    errorUpdate = errorUpdate.Length > errorLength
                        ? errorUpdate.Substring(0, errorLength)
                        : errorUpdate;
                    errorString = StringLiterals.Blank;
                    errorAdd = true;
                    #endregion
                }

                #region errorAdd
                if (errorAdd)
                {
                    files.AppendTo(FileTypes.Error, (writer) =>
                    {
                        writer.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                        writer.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                        writer.WriteLine("AT:" + errorAttribute);
                        writer.WriteLine("ER:" + errorString);
                        writer.WriteLine("VL:" + (entry.Attributes.Contains(errorAttribute) ? entry.Attributes[errorAttribute][errorIndex].ToString() : String.Empty));
                        writer.WriteLine("UP:" + errorUpdate);
                        writer.WriteLine("---");
                    });
                    errorCount++;
                }
                #endregion
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception
                    + entry.Attributes[StringLiterals.DistinguishedName][0].ToString() + "  "
                    + errorAttribute + "  "
                    + (entry.Attributes.Contains(errorAttribute) ? entry.Attributes[errorAttribute][errorIndex].ToString() : "blank") + "  "
                    + ex.Message);
            }
        }

        private void dChecks(SearchResultEntry entry, string errorAttribute, int errorIndex, int errorLength,
            Regex errorCharacter, Regex errorFormat, bool errorDuplicate, bool errorBlank)
        {
            try
            {
                bool errorAdd = false;
                string errorUpdate = String.Empty;
                string errorString = String.Empty;
                string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();

                if (entry.Attributes.Contains(errorAttribute))
                {
                    errorUpdate = entry.Attributes[errorAttribute][errorIndex].ToString();

                    #region errorCharacter
                    if (errorCharacter != null)
                    {
                        if (errorCharacter.IsMatch(entry.Attributes[errorAttribute][errorIndex].ToString()))
                        {
                            errorAdd = true;
                            errorString += StringLiterals.Character;
                            errorUpdate = String.IsNullOrEmpty(errorCharacter.Replace(errorUpdate, ""))
                                ? entry.Attributes[StringLiterals.Cn][0].ToString()
                                : errorCharacter.Replace(errorUpdate, "");
                        }
                    }
                    #endregion

                    #region errorFormat
                    if (errorFormat != null)
                    {
                        string validateAttribute = smtp.IsMatch(errorUpdate)
                            ? errorUpdate.Substring(errorUpdate.IndexOf(":") + 1)
                            : errorUpdate;

                        #region periods
                        if (errorFormat == periods && !errorFormat.IsMatch(validateAttribute))
                        {
                            errorAdd = true;
                            errorString += StringLiterals.Format;
                            errorUpdate = String.IsNullOrEmpty(new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, ""))
                                ? entry.Attributes[StringLiterals.Cn][0].ToString()
                                : new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, "");
                            errorUpdate = String.IsNullOrEmpty(new Regex(@"\.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, ""))
                                ? entry.Attributes[StringLiterals.Cn][0].ToString()
                                : new Regex(@"\.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(errorUpdate, "");
                        }
                        #endregion

                        #region rfc2822
                        if (errorFormat == rfc2822)
                        {
                            #region topleveldomain
                            if (validateAttribute.LastIndexOf(".") != -1)
                            {
                                string tldDomain = validateAttribute.ToLowerInvariant().Substring(validateAttribute.LastIndexOf("."));
                                if (tldDomain.Length > 1)
                                {
                                    tldDomain = tldDomain.Substring(tldDomain.IndexOf(".") + 1);
                                    if (!tldList.Contains(tldDomain))
                                    {
                                        errorAdd = true;
                                        errorString += StringLiterals.TopLevelDomain;
                                    }
                                }
                            }
                            #endregion

                            #region domainpart
                            if (!domainPart.IsMatch(validateAttribute))
                            {
                                errorAdd = true;
                                errorString += StringLiterals.DomainPart;
                                if (validateAttribute.LastIndexOf("@") != -1)
                                {
                                    validateAttribute = validateAttribute.Substring(0, validateAttribute.LastIndexOf("@") + 1)
                                        + (new Regex(@"[^a-z0-9.-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(validateAttribute.LastIndexOf("@") + 1), "");
                                }
                            }
                            #endregion

                            #region localpart
                            if (!localPart.IsMatch(validateAttribute) || (validateAttribute.Split('@').Length - 1 > 1))
                            {
                                errorAdd = true;
                                errorString += StringLiterals.LocalPart;
                                if (validateAttribute.LastIndexOf("@") != -1)
                                {
                                    string validateLocal = (new Regex(@"[^a-z0-9.!#$%&'*+/=?^_`{|}~-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(validateAttribute.Substring(0, validateAttribute.LastIndexOf("@")), "");
                                    validateAttribute = (validateLocal + validateAttribute.Substring(validateAttribute.LastIndexOf("@"))).Replace(".@", "@");
                                }
                            }
                            #endregion

                            #region other
                            if (!rfc2822.IsMatch(validateAttribute))
                            {
                                errorAdd = true;
                                errorString += StringLiterals.Format;
                            }
                            #endregion

                            errorUpdate = smtp.IsMatch(errorUpdate)
                                ? errorUpdate.Substring(0, errorUpdate.IndexOf(":") + 1).Trim() + validateAttribute
                                : validateAttribute;
                        }
                        #endregion

                        #region other format errors
                        if (!errorFormat.IsMatch(validateAttribute))
                        {
                            if (errorFormat != periods && errorFormat != rfc2822)
                            {
                                errorAdd = true;
                                errorString += StringLiterals.Format;
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region errorLength
                    if (errorAttribute.Equals(StringLiterals.UserPrincipalName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        string upn = entry.Attributes[errorAttribute][errorIndex].ToString();
                        if (upn.IndexOf("@", StringComparison.CurrentCulture) != -1)
                        {
                            string upnUser = upn.Substring(0, upn.IndexOf("@", StringComparison.CurrentCulture));
                            if (upnUser.Length > 64)
                            {
                                errorAdd = true;
                                errorString += "userlength,";
                                upnUser = upn.Substring(0, 64);
                            }
                            string upnDomain = upn.Substring(upn.IndexOf("@", StringComparison.CurrentCulture) + 1);
                            if (upnDomain.Length > 256)
                            {
                                errorAdd = true;
                                errorString += "domainlength,";
                                upnDomain = upn.Substring(upn.IndexOf("@", StringComparison.CurrentCulture) + 1, 256);
                            }
                            errorUpdate = errorCharacter.Replace(upnUser + "@" + upnDomain, "");
                        }
                    }
                    else
                    {
                        if (entry.Attributes[errorAttribute][errorIndex].ToString().Length > errorLength)
                        {
                            errorAdd = true;
                            errorString += StringLiterals.Length;
                            errorUpdate = errorUpdate.Length > errorLength ? errorUpdate.Substring(0, errorLength) : errorUpdate;
                        }
                    }
                    #endregion

                    #region errorDuplicate
                    if (errorDuplicate)
                    {
                        #region update suggestion
                        string dupUpdate = String.Empty;
                        if (errorUpdate == entry.Attributes[errorAttribute][errorIndex].ToString())
                        {
                            switch (errorAttribute.ToLowerInvariant())
                            {
                                case "proxyaddresses":
                                    if (smtp.IsMatch(errorUpdate))
                                    {
                                        if (entry.Attributes.Contains(StringLiterals.MailNickName))
                                        {
                                            if (errorUpdate.Substring(0, 5) == "SMTP:")
                                            {
                                                dupUpdate = "[C]" + errorUpdate;
                                            }
                                            else
                                            {
                                                dupUpdate = "[E]" + errorUpdate;
                                            }
                                        }
                                        else
                                        {
                                            dupUpdate = "[R]" + errorUpdate;
                                        }
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                                case "mailnickname":
                                    if (entry.Attributes.Contains(StringLiterals.HomeMdb))
                                    {
                                        dupUpdate = "[C]" + errorUpdate;
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                                case "mail":
                                    if (entry.Attributes.Contains(StringLiterals.MailNickName))
                                    {
                                        dupUpdate = "[C]" + errorUpdate;
                                    }
                                    else
                                    {
                                        dupUpdate = "[E]" + errorUpdate;
                                    }
                                    break;
                            }
                        }
                        #endregion

                        files.AppendTo(FileTypes.Duplicate, (writer) => {
                            writer.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                            writer.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                            writer.WriteLine("AT:" + errorAttribute);
                            writer.WriteLine("VL:" + entry.Attributes[errorAttribute][errorIndex].ToString());
                            writer.WriteLine("UP:" + dupUpdate);
                            writer.WriteLine("---");
                        });
                        duplicateCount++;
                    }
                    #endregion

                    #region contact or user: mail = targetAddress
                    if (errorAttribute.Equals(StringLiterals.TargetAddress, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (objectType.Equals("contact", StringComparison.CurrentCultureIgnoreCase) ||
                            (objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase) && !entry.Attributes.Contains(StringLiterals.HomeMdb)))
                        {
                            string mailMatch = smtp.IsMatch(entry.Attributes[errorAttribute][errorIndex].ToString())
                                ? entry.Attributes[errorAttribute][errorIndex].ToString().Substring(entry.Attributes[errorAttribute][errorIndex].ToString().IndexOf(":") + 1)
                                : entry.Attributes[errorAttribute][errorIndex].ToString();
                            if (!mailMatch.Equals(entry.Attributes[StringLiterals.Mail][errorIndex].ToString(), StringComparison.CurrentCultureIgnoreCase))
                            {
                                errorUpdate = "SMTP:" + entry.Attributes[StringLiterals.Mail][0].ToString();
                                errorUpdate = errorUpdate.Length > errorLength
                                    ? errorUpdate.Substring(0, errorLength)
                                    : errorUpdate;
                                errorString += "mailMatch,";
                                errorAdd = true;
                            }
                        }
                    }
                    #endregion
                }
                else if (errorBlank)
                {
                    #region errorBlank
                    #region group
                    if (objectType.Equals("group", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (errorAttribute.Equals(StringLiterals.DisplayName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorUpdate += entry.Attributes[StringLiterals.Cn][0].ToString();
                            errorUpdate = errorUpdate.Length > errorLength
                                ? errorUpdate.Substring(0, errorLength)
                                : errorUpdate;
                            errorString = StringLiterals.Blank;
                            errorAdd = true;
                        }
                    }
                    #endregion

                    #region contact
                    if (objectType.Equals("contact", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (errorAttribute.Equals(StringLiterals.MailNickName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (entry.Attributes.Contains(StringLiterals.GivenName))
                            {
                                errorUpdate += entry.Attributes[StringLiterals.GivenName][0].ToString();
                            }
                            if (entry.Attributes.Contains(StringLiterals.Sn))
                            {
                                if (!String.IsNullOrEmpty(errorUpdate))
                                {
                                    errorUpdate += ".";
                                }
                                errorUpdate += entry.Attributes[StringLiterals.Sn][0].ToString();
                            }
                            if (String.IsNullOrEmpty(errorUpdate))
                            {
                                if (entry.Attributes.Contains(StringLiterals.SamAccountName))
                                {
                                    errorUpdate += entry.Attributes[StringLiterals.SamAccountName][0].ToString();
                                }
                                else
                                {
                                    errorUpdate += entry.Attributes[StringLiterals.Cn][0].ToString();
                                }
                            }
                            errorUpdate = errorUpdate.Length > errorLength
                                ? errorUpdate.Substring(0, errorLength)
                                : errorUpdate;
                            errorString = "blank,";
                            errorAdd = true;
                        }

                        if (errorAttribute.Equals(StringLiterals.TargetAddress, StringComparison.CurrentCultureIgnoreCase))
                        {
                            errorUpdate = "SMTP:" + entry.Attributes[StringLiterals.Mail][0].ToString();
                            errorUpdate = errorUpdate.Length > errorLength
                                ? errorUpdate.Substring(0, errorLength)
                                : errorUpdate;
                            errorString += "blank,";
                            errorAdd = true;
                        }
                    }
                    #endregion

                    #region user
                    if (objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (errorAttribute.Equals(StringLiterals.MailNickName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (entry.Attributes.Contains(StringLiterals.GivenName))
                            {
                                errorUpdate += entry.Attributes[StringLiterals.GivenName][0].ToString();
                            }
                            if (entry.Attributes.Contains(StringLiterals.Sn))
                            {
                                if (String.IsNullOrEmpty(errorUpdate))
                                {
                                    errorUpdate += ".";
                                }
                                errorUpdate += entry.Attributes[StringLiterals.Sn][0].ToString();
                            }
                            if (String.IsNullOrEmpty(errorUpdate))
                            {
                                if (entry.Attributes.Contains(StringLiterals.SamAccountName))
                                {
                                    errorUpdate += entry.Attributes[StringLiterals.SamAccountName][0].ToString();
                                }
                                else
                                {
                                    errorUpdate += entry.Attributes[StringLiterals.Cn][0].ToString();
                                }
                            }
                            errorUpdate = errorUpdate.Length > errorLength
                                ? errorUpdate.Substring(0, errorLength)
                                : errorUpdate;
                            errorString = StringLiterals.Blank;
                            errorAdd = true;
                        }

                        if (errorAttribute.Equals(StringLiterals.TargetAddress, StringComparison.CurrentCultureIgnoreCase)
                            && !entry.Attributes.Contains(StringLiterals.HomeMdb))
                        {
                            errorUpdate += "SMTP:" + entry.Attributes[StringLiterals.Mail][0].ToString();
                            errorUpdate = errorUpdate.Length > errorLength
                                ? errorUpdate.Substring(0, errorLength)
                                : errorUpdate;
                            errorString = StringLiterals.Blank;
                            errorAdd = true;
                        }
                    }
                    #endregion
                    #endregion
                }

                #region errorAdd
                if (errorAdd)
                {
                    files.AppendTo(FileTypes.Error, (writer) =>
                    {
                        writer.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                        writer.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                        writer.WriteLine("AT:" + errorAttribute);
                        writer.WriteLine("ER:" + errorString);
                        writer.WriteLine("VL:" + (entry.Attributes.Contains(errorAttribute) ? entry.Attributes[errorAttribute][errorIndex].ToString() : String.Empty));
                        writer.WriteLine("UP:" + errorUpdate);
                        writer.WriteLine("---");
                    });
                    errorCount++;
                }
                #endregion
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception
                    + entry.Attributes[StringLiterals.DistinguishedName][0].ToString() + "  "
                    + errorAttribute + "  "
                    + (entry.Attributes.Contains(errorAttribute) ? entry.Attributes[errorAttribute][errorIndex].ToString() : "blank") + "  "
                    + ex.Message);
            }
        }

        /// <summary>
        /// This is a function written off of checks being done in mtChecks to valid upn.
        /// </summary>
        /// <param name="entry">SearchResult entry</param>
        /// <returns></returns>
        private bool IsValidUpn(SearchResultEntry entry)
        {
            if (!entry.Attributes.Contains(StringLiterals.UserPrincipalName))
            {
                return true;
            }

            if (SettingsManager.Instance.UseAlternateLogin)
            {
                return true;
            }

            string upn = entry.Attributes[StringLiterals.UserPrincipalName][0].ToString();

            //If any invalid character matches, we are done
            if (invalidUpnRegEx.IsMatch(upn))
            {
                return false;
            }

            //Additional rfc2822 related checks
            #region topleveldomain
            if (upn.LastIndexOf(".") != -1)
            {
                string tldDomain = upn.ToLowerInvariant().Substring(upn.LastIndexOf("."));
                if (tldDomain.Length > 1)
                {
                    tldDomain = tldDomain.Substring(tldDomain.IndexOf(".") + 1);
                    if (!tldList.Contains(tldDomain))
                    {
                        return false;
                    }
                }
            }
            #endregion

            if (
                (!domainPart.IsMatch(upn)) ||
                (!localPart.IsMatch(upn) ||
                (upn.Split('@').Length - 1 > 1)) ||
                !rfc2822.IsMatch(upn)
                )
            {
                return false;
            }

            return true;
        }


        #endregion

        #region file actions
        private void readDuplicateFile()
        {
            try
            {
                string line = String.Empty;
                string dn = String.Empty;
                string ob = String.Empty;
                string at = String.Empty;
                string vl = String.Empty;
                string up = String.Empty;
                string dupObjDictKey;
                string dupDictKey;
                long dupDictValue;
                DuplicateClass dupObjDictValue;

                if (files.ExistsByType(FileTypes.Duplicate))
                {
                    files.ReadFrom(FileTypes.Duplicate, (reader) =>
                    {
                        #region write split files
                        statusDisplay("Write split files");
                        List<string> splitList = new List<string>();
                        List<string> splitFileList = new List<string>();
                        int splitCount = 0;
                        int splitFile = 0;
                        string splitFileName;
                        try
                        {
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.Length < 3)
                                {
                                    statusDisplay(StringLiterals.Exception + "Duplicate File - Possible Escape Character in object: " + dn + "|" + ob + "|" + at + "|" + vl + "|" + up);
                                }
                                else
                                {
                                    switch (line.Substring(0, 3).ToString())
                                    {
                                        case "DN:":
                                            dn = line.Substring(3);
                                            break;
                                        case "OB:":
                                            ob = line.Substring(3);
                                            break;
                                        case "AT:":
                                            at = line.Substring(3);
                                            break;
                                        case "VL:":
                                            vl = line.Substring(3);
                                            break;
                                        case "UP:":
                                            up = line.Substring(3);
                                            break;
                                        case "---":
                                            splitCount++;
                                            splitList.Add(at.ToUpperInvariant() + "|" + (!String.IsNullOrEmpty(vl) ? vl.ToUpperInvariant() : "BLANK"));
                                            if (splitCount == blockSize)
                                            {
                                                splitCount = 0;
                                                splitFile++;
                                                splitFileName = "Split" + splitFile.ToString(CultureInfo.CurrentCulture) + ".txt";
                                                using (StreamWriter split = new StreamWriter(splitFileName, true))
                                                {
                                                    splitFileList.Add(splitFileName);
                                                    splitList.Sort();
                                                    foreach (string splitDuplicate in splitList)
                                                    {
                                                        split.WriteLine(splitDuplicate);
                                                    }
                                                    splitList.Clear();
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                            splitFile++;
                            splitFileName = "Split" + splitFile.ToString(CultureInfo.CurrentCulture) + ".txt";
                            using (StreamWriter split = new StreamWriter(splitFileName, true))
                            {
                                splitFileList.Add(splitFileName);
                                splitList.Sort();
                                foreach (string splitDuplicate in splitList)
                                {
                                    split.WriteLine(splitDuplicate);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            statusDisplay(StringLiterals.Exception + "Write Split: " + dn + "|" + ob + "|" + at + "|" + vl + "|" + up);
                            statusDisplay(StringLiterals.Exception + "Write Split: " + ex.Message);
                        }
                        #endregion

                        #region merge split files
                        statusDisplay("Merge split files");
                        List<StreamReader> splitReaders = new List<StreamReader>();
                        List<string> splitValue = new List<string>();
                        List<string> sortValue = new List<string>();
                        bool allMerged;
                        int splitIndex;
                        string writeSplit = String.Empty;

                        try
                        {
                            if (splitFileList.Count > 0)
                            {
                                foreach (string split in splitFileList)
                                {
                                    var splitReader = new StreamReader(split);
                                    splitReaders.Add(splitReader);
                                    splitValue.Add(String.Empty);
                                }

                                while (true)
                                {
                                    writeSplit = String.Empty;
                                    allMerged = true;
                                    splitIndex = 0;
                                    foreach (StreamReader splitReader in splitReaders)
                                    {
                                        // is there a value needed?
                                        if (String.IsNullOrEmpty(splitValue[splitIndex]))
                                        {
                                            // is there a value to read?
                                            if (splitReader.Peek() >= 0)
                                            {
                                                splitValue[splitIndex] = splitReader.ReadLine();
                                            }
                                        }
                                        splitIndex++;
                                    }

                                    // get the first non-empty value
                                    sortValue.Clear();
                                    foreach (string split in splitValue)
                                    {
                                        sortValue.Add(split);
                                    }

                                    // sortValue = splitValue;
                                    sortValue.Sort();
                                    foreach (string split in sortValue)
                                    {
                                        if (!String.IsNullOrEmpty(split))
                                        {
                                            writeSplit = split;
                                            break;
                                        }
                                    }
                                    if (String.IsNullOrEmpty(writeSplit))
                                    {
                                        break;
                                    }

                                    splitIndex = 0;
                                    for (int i = 0; i < splitValue.Count; i++)
                                    {
                                        // splitval = splitValue[i];
                                        if (!String.IsNullOrEmpty(splitValue[i]))
                                        {
                                            if (splitValue[i] == writeSplit)
                                            {
                                                files.AppendTo(FileTypes.Merge, (writer) =>
                                                {
                                                    writer.WriteLine(splitValue[i]);
                                                });
                                                splitValue[i] = String.Empty;
                                            }
                                            allMerged = false;
                                        }
                                    }

                                    // are we done yet
                                    if (allMerged)
                                    {
                                        break;
                                    }
                                }

                                foreach (StreamReader splitReader in splitReaders)
                                {
                                    splitReader.Close();
                                    splitReader.Dispose();
                                }

                                foreach (string split in splitFileList)
                                {
                                    File.Delete(split);
                                }
                            }
                            else
                            {
                                statusDisplay(StringLiterals.Exception + "Merge Split: No split files found");
                            }
                        }
                        catch (Exception ex)
                        {
                            statusDisplay(StringLiterals.Exception + "Merge Split: " + writeSplit);
                            statusDisplay(StringLiterals.Exception + "Merge Split: " + ex.Message);
                        }
                        #endregion

                        #region count duplicates
                        statusDisplay("Count duplicates");
                        dupDict.Clear();
                        string lastLine = String.Empty;
                        string lastWritten = String.Empty;
                        try
                        {
                            if (files.ExistsByType(FileTypes.Merge))
                            {
                                files.ReadFrom(FileTypes.Merge, (reader2) =>
                                {
                                    while ((line = reader2.ReadLine()) != null)
                                    {
                                        if (line == lastLine && line != lastWritten)
                                        {
                                            dupDict.Add(line, 2);
                                            lastWritten = line;
                                        }
                                        lastLine = line;
                                    }
                                });
                                files.DeleteByType(FileTypes.Merge);
                            }
                            else
                            {
                                statusDisplay(StringLiterals.Exception + "Count Duplicates: No Merge File Found");
                            }
                        }
                        catch (Exception ex)
                        {
                            statusDisplay(StringLiterals.Exception + "Count Duplicate: " + lastLine);
                            statusDisplay(StringLiterals.Exception + "Count Duplicate: " + ex.Message);
                        }
                        #endregion
                    });


                    if (dupDict.Count >= 1)
                    {
                        #region write filtered duplicate objects
                        statusDisplay("Write filtered duplicate objects");
                        files.ReadFrom(FileTypes.Duplicate, (reader) =>
                        {
                            try
                            {
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (line.Length < 3)
                                    {
                                        statusDisplay(StringLiterals.Exception + "Duplicate File - Possible Escape Character in object: " + dn + "|" + ob + "|" + at + "|" + vl + "|" + up);
                                    }
                                    else
                                    {
                                        switch (line.Substring(0, 3).ToString())
                                        {
                                            case "DN:":
                                                dn = line.Substring(3);
                                                break;
                                            case "OB:":
                                                ob = line.Substring(3);
                                                break;
                                            case "AT:":
                                                at = line.Substring(3);
                                                break;
                                            case "VL:":
                                                vl = line.Substring(3);
                                                break;
                                            case "UP:":
                                                up = line.Substring(3);
                                                break;
                                            case "---":
                                                dupDictKey = at.ToUpperInvariant() + "|" + (!String.IsNullOrEmpty(vl) ? vl.ToUpperInvariant() : "BLANK");

                                                if (dupDict.TryGetValue(dupDictKey, out dupDictValue))
                                                {
                                                    if (dupDictValue > 1)
                                                    {
                                                        files.AppendTo(FileTypes.Filtered, (writer) =>
                                                        {
                                                            writer.WriteLine("DN:" + dn);
                                                            writer.WriteLine("OB:" + ob);
                                                            writer.WriteLine("AT:" + at);
                                                            writer.WriteLine("VL:" + vl);
                                                            writer.WriteLine("UP:" + up);
                                                            writer.WriteLine("---");
                                                        });
                                                    }
                                                }
                                                break;
                                        }
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                statusDisplay(StringLiterals.Exception + "Write Filtered: " + dn + "|" + ob + "|" + at + "|" + vl + "|" + up);
                                statusDisplay(StringLiterals.Exception + "Write Filtered: " + ex.Message);
                            }
                        });
                        
                        dupDict.Clear();
                        dupObjDict.Clear();
                        #endregion

                        #region read filtered duplicate objects
                        if (files.ExistsByType(FileTypes.Filtered))
                        {
                            statusDisplay("Read filtered duplicate objects");
                            files.ReadFrom(FileTypes.Filtered, (reader) =>
                            {
                                try
                                {
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        if (line.Length < 3)
                                        {
                                            statusDisplay(StringLiterals.Exception + "Filtered File - Possible Escape Character in object: " + dn + "|" + ob + "|" + at + "|" + vl + "|" + up);
                                        }
                                        else
                                        {
                                            switch (line.Substring(0, 3).ToString())
                                            {
                                                case "DN:":
                                                    dn = line.Substring(3);
                                                    break;
                                                case "OB:":
                                                    ob = line.Substring(3);
                                                    break;
                                                case "AT:":
                                                    at = line.Substring(3);
                                                    break;
                                                case "VL:":
                                                    vl = line.Substring(3);
                                                    break;
                                                case "UP:":
                                                    up = line.Substring(3);
                                                    break;
                                                case "---":
                                                    dupObjDictKey = dn.ToUpperInvariant() + "|" +
                                                       at.ToUpperInvariant() + "|" +
                                                       (!String.IsNullOrEmpty(vl) ? vl.ToUpperInvariant() : "BLANK");
                                                    dupObjDictValue = new DuplicateClass(dn,
                                                        ob,
                                                        at,
                                                        vl,
                                                        up);
                                                    dupObjDict.Add(dupObjDictKey, dupObjDictValue);
                                                    break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    statusDisplay(StringLiterals.Exception + "Read Filtered: " + dn + "|" + ob + "|" + at + "|" + vl + "|" + up);
                                    statusDisplay(StringLiterals.Exception + "Read Filtered: " + ex.Message);
                                }
                            });
                            files.DeleteByType(FileTypes.Filtered);
                        }
                        #endregion
                    }
                    else
                    {
                        statusDisplay("No duplicate values in file");
                    }

                    files.DeleteByType(FileTypes.Duplicate);
                }
                else
                {
                    statusDisplay(StringLiterals.Exception + "No Duplicate File Found");
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.ReadDuplicateFile + "  " + ex.Message);
                throw;
            }
        }

        private void readErrorFile()
        {
            try
            {
                if (files.ExistsByType(FileTypes.Error))
                {
                    statusDisplay("Read error file");

                    files.ReadFrom(FileTypes.Error, (reader) =>
                    {
                        string line;
                        string dn = String.Empty;
                        string ob = String.Empty;
                        string at = String.Empty;
                        string er = String.Empty;
                        string up = String.Empty;
                        string vl = String.Empty;
                        string errorDictKey;
                        ErrorClass errorDictValue;
                        try
                        {
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.Length < 3)
                                {
                                    statusDisplay(StringLiterals.Exception + "Error File - Possible Escape Character in object: " + dn + "|" + ob + "|" + at + "|" + er + "|" + vl + "|" + up);
                                }
                                else
                                {
                                    switch (line.Substring(0, 3).ToString())
                                    {
                                        case "DN:":
                                            dn = line.Substring(3);
                                            break;
                                        case "OB:":
                                            ob = line.Substring(3);
                                            break;
                                        case "AT:":
                                            at = line.Substring(3);
                                            break;
                                        case "ER:":
                                            er = line.Substring(3);
                                            break;
                                        case "VL:":
                                            vl = line.Substring(3);
                                            break;
                                        case "UP:":
                                            up = line.Substring(3);
                                            break;
                                        case "---":
                                            errorDictKey = dn.ToUpperInvariant() + "|" +
                                               at.ToUpperInvariant() + "|" +
                                               (!String.IsNullOrEmpty(vl) ? vl.ToUpperInvariant() : "BLANK");
                                            errorDictValue = new ErrorClass(dn,
                                                ob,
                                                at,
                                                er,
                                                vl,
                                                up);
                                            errDict.Add(errorDictKey, errorDictValue);
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            statusDisplay(StringLiterals.Exception + "Error File: " + dn + "|" + ob + "|" + at + "|" + er + "|" + vl + "|" + up);
                            statusDisplay(StringLiterals.Exception + "Error File: " + ex.Message);
                        }
                    });

                    files.DeleteByType(FileTypes.Error);
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.ReadErrorFile + "  " + ex.Message);
                throw;
            }
        }

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
                foreach (DataGridViewRow rowSelected in dataGridView1.Rows)
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
