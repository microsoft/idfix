using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;

namespace IdFix
{
    public partial class Form1 : Form
    {
        #region members
        Dictionary<string, string> tldDict = new Dictionary<string, string>();
        Dictionary<string, long> dupDict = new Dictionary<string, long>();
        Dictionary<string, DuplicateClass> dupObjDict = new Dictionary<string, DuplicateClass>();
        Dictionary<string, ErrorClass> errDict = new Dictionary<string, ErrorClass>();
        int blockSize = 50000;
        long entryCount;
        long errorCount;
        long duplicateCount;
        long displayCount;
        string fileName = (new Regex(@"[/:]")).Replace(DateTime.Now.ToString(), "-") + ".txt";
        string verboseFile;
        string applyFile;
        string errorFile;
        string duplicateFile;
        string filteredFile;
        string countFile;
        string mergeFile;
        public List<string> forestList = new List<string>();
        string forest = String.Empty;
        public string serverName = System.Environment.MachineName;
        public string ldapPort = "3268";
        public string searchBase;
        public string ldapFilter = "(|(objectCategory=Person)(objectCategory=Group))";
        public string user;
        public string password;
        public string ruleString = string.Empty;
        public bool settingsMT = true;
        public bool settingsAD = true;
        public bool settingsCU = true;
        ModifyRequest modifyRequest;
        DirectoryResponse directoryResponse;
        public string[] attributesToReturn = new string[] { StringLiterals.Cn, 
                    StringLiterals.DisplayName, //Added in 1.11
                    StringLiterals.DistinguishedName, 
                    StringLiterals.GivenName, //Added in 1.11
                    StringLiterals.GroupType, StringLiterals.HomeMdb, StringLiterals.IsCriticalSystemObject, StringLiterals.Mail, 
                    StringLiterals.MailNickName, StringLiterals.MsExchHideFromAddressLists,
                    StringLiterals.MsExchRecipientTypeDetails, StringLiterals.ObjectClass, StringLiterals.ProxyAddresses, 
                    StringLiterals.SamAccountName, StringLiterals.TargetAddress, StringLiterals.UserPrincipalName } ;
        public string targetSearch = String.Empty;
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
        private static string[] wellKnownExclusions = { "Admini", "CAS_{", "DiscoverySearchMailbox", "FederatedEmail", "Guest", "HTTPConnector", "krbtgt", 
                                                          "iusr_",  "iwam", "msol", "support_", "SystemMailbox",  "WWIOadmini", 
                                                          "HealthMailbox", "Exchange Online-ApplicationAccount"};

        internal bool searchBaseEnabled = false;
        internal bool firstRun = false;

        internal const int maxUserNameLength = 64;
        internal const int maxDomainLength = 48;
        #endregion

        #region Form1
        public Form1()
        {
            try
            {
                this.firstRun = true; //Only the first time.
                InitializeComponent();
                verboseFile = "Verbose " + fileName;
                targetSearch = String.Empty;

                this.Text = StringLiterals.IdFixVersion;
                this.searchBaseEnabled = false;
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
                using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("IdFix.domains.txt")))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!tldDict.ContainsKey(line.Trim().ToLowerInvariant()))
                        {
                            tldDict.Add(line.Trim().ToLowerInvariant(), line.Trim().ToLowerInvariant());
                        }
                    }
                }
                forestList.Add(Forest.GetCurrentForest().Name);
                if (String.IsNullOrEmpty(targetSearch))
                {
                    targetSearch = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, Forest.GetCurrentForest().Name)).Name.Replace(".", ",dc=");
                }
                statusDisplay("Ready");
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
            if(firstRun)
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
                backgroundWorker1.RunWorkerAsync();
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
                applyFile = "Update " + (new Regex(@"[/:]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Replace(DateTime.Now.ToString(), "-") + ".ldf";
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
                        string domainModName = domainMod.ToLowerInvariant().Replace(",dc=",".").Replace("dc=","");

                        if (settingsAD)
                        {
                            serverName = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, domainModName)).FindDomainController().Name;
                            statusDisplay(String.Format("Using server {0} for updating", serverName));
                        }

                        if (ldapPort == "3268")
                        {
                            ldapPort = "389";
                        }
                        #endregion

                        #region connection
                        using (LdapConnection connection = new LdapConnection(serverName + ":" + ldapPort))
                        {
                            #region connection parameters
                            if (ldapPort == "636")
                            {
                                connection.SessionOptions.ProtocolVersion = 3;
                                connection.SessionOptions.SecureSocketLayer = true;
                                connection.AuthType = AuthType.Negotiate;
                            }
                            if (!settingsCU)
                            {
                                NetworkCredential credential = new NetworkCredential(user, password);
                                connection.Credential = credential;
                            }
                            connection.Timeout = TimeSpan.FromSeconds(120);
                            #endregion

                            #region get the object
                            SearchRequest findme = new SearchRequest();
                            findme.DistinguishedName = dnMod;
                            findme.Filter = ldapFilter;
                            findme.Scope = System.DirectoryServices.Protocols.SearchScope.Base;
                            SearchResponse results = (SearchResponse)connection.SendRequest(findme);
                            SearchResultEntryCollection entries = results.Entries;
                            SearchResultEntry entry;
                            if (results.Entries.Count != 1)
                            {
                                statusDisplay(StringLiterals.Exception + "Found " + results.Entries.Count.ToString() + " entries when searching for " + dnMod );
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
                targetSearch = String.Empty;
                if (settingsMT)
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
                FormSettings formSettings = new FormSettings(this);
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

        #region threading
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                #region prepare for query
                BeginInvoke((MethodInvoker)delegate
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
                });

                entryCount = 0;
                errorCount = 0;
                duplicateCount = 0;
                stopwatch = DateTime.Now;
                errDict.Clear();
                dupDict.Clear();
                dupObjDict.Clear();
                e.Result = StringLiterals.Complete;
                #endregion

                #region clean up temporary files
                errorFile = "Error " + fileName;
                if (File.Exists(errorFile))
                {
                    File.Delete(errorFile);
                }

                duplicateFile = "Duplicate " + fileName;
                if (File.Exists(duplicateFile))
                {
                    File.Delete(duplicateFile);
                }

                filteredFile = "Filtered " + fileName;
                if (File.Exists(filteredFile))
                {
                    File.Delete(filteredFile);
                }

                countFile = "Count " + fileName;
                if (File.Exists(countFile))
                {
                    File.Delete(countFile);
                }

                mergeFile = "Merge " + fileName;
                if (File.Exists(mergeFile))
                {
                    File.Delete(mergeFile);
                }
                #endregion

                # region query
                int forestListIndex = 0;
                while (true)
                {
                    # region server, target, port
                    if (settingsAD)
                    {
                        if (ldapPort == "3268")
                        {
                            serverName = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, forestList[forestListIndex])).Name;
                            if (!String.IsNullOrEmpty(searchBase))
                                targetSearch = searchBase;
                            else
                                targetSearch = String.Empty;
                        }
                        else
                        {
                            serverName = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, forestList[forestListIndex])).Domains[0].FindDomainController().Name;
                            //targetSearch = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, forestList[forestListIndex])).Name.Replace(".", ",dc=");
                            if(!String.IsNullOrEmpty(searchBase))
                                targetSearch = searchBase; 
                            else
                                targetSearch = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, forestList[forestListIndex])).Name.Replace(".", ",dc=");
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(targetSearch))
                        {
                            targetSearch = "dc=" + Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, forestList[forestListIndex])).Name.Replace(".", ",dc=");
                        }
                    }

                    ruleString = (settingsMT) ? "Multi-Tenant" : "Dedicated";
                    BeginInvoke((MethodInvoker)delegate
                    {
                        statusDisplay("RULES:" + ruleString + " SERVER:" + serverName + " PORT:" + ldapPort + " FILTER:" + ldapFilter);
                    });
                    #endregion

                    #region connection
                    using (LdapConnection connection = new LdapConnection(serverName + ":" + ldapPort))
                    {
                        #region search request
                        if (ldapPort == "636")
                        {
                            connection.SessionOptions.ProtocolVersion = 3;
                            connection.SessionOptions.SecureSocketLayer = true;
                            connection.AuthType = AuthType.Negotiate;
                        }

                        if (!settingsCU)
                        {
                            NetworkCredential credential = new NetworkCredential(user, password);
                            connection.Credential = credential;
                        }

                        int pageSize = 1000;
                        displayCount = 0;
                        connection.Timeout = TimeSpan.FromSeconds(120);

                        PageResultRequestControl pageRequest = new PageResultRequestControl(pageSize);
                        SearchRequest searchRequest = new SearchRequest(
                            targetSearch,
                            ldapFilter,
                            System.DirectoryServices.Protocols.SearchScope.Subtree,
                            attributesToReturn);
                        searchRequest.Controls.Add(pageRequest);
                        SearchResponse searchResponse;
                        BeginInvoke((MethodInvoker)delegate
                        {
                            statusDisplay("Please wait while the LDAP Connection is established.");
                        });
                        #endregion

                        while (true)
                        {
                            #region get a page
                            searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

                            // verify support for paged results
                            if (searchResponse.Controls.Length != 1 || !(searchResponse.Controls[0] is PageResultResponseControl))
                            {
                                BeginInvoke((MethodInvoker)delegate
                                {
                                    statusDisplay("The server cannot page the result set");
                                });
                                throw new InvalidOperationException("The server cannot page the result set");
                            }

                            PageResultResponseControl pageResponse = (PageResultResponseControl)searchResponse.Controls[0];
                            #endregion

                            foreach (SearchResultEntry entry in searchResponse.Entries)
                            {

                                #region check for cancel
                                if (backgroundWorker1.CancellationPending)
                                {
                                    e.Cancel = true;
                                    e.Result = StringLiterals.CancelQuery;
                                    BeginInvoke((MethodInvoker)delegate
                                    {
                                        cancelToolStripMenuItem.Enabled = false;
                                    });

                                    errorFile = "Error " + fileName;
                                    if (File.Exists(errorFile))
                                    {
                                        File.Delete(errorFile);
                                    }

                                    duplicateFile = "Duplicate " + fileName;
                                    if (File.Exists(duplicateFile))
                                    {
                                        File.Delete(duplicateFile);
                                    }
                                    return;
                                }
                                #endregion

                                entryCount++;

                                #region perform checks
                                try
                                {
                                    string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();
                                    if (settingsMT)
                                    {
                                        #region do MT checks
                                        if (mtFilter(entry))
                                        {
                                            continue;
                                        }

                                        //Additional check for DisplayName. 
                                        //  Value is non-blank if present
                                        //  Max Length = 255. 
                                        //The attribute being present check is done in mtChecks, however, the errorBlank check is done only if the attribute
                                        //is missing, so that has been updated to check for blanks if attribute present.
                                        mtChecks(entry, StringLiterals.DisplayName, 0, 255, null, null, false, false, true);

                                        //Additional check for GivenName. 
                                        //  Max Length = 63.
                                        mtChecks(entry, StringLiterals.GivenName, 0, 63, null, null, false, false, false);

                                        //New documentation doesn't say anything about mail not being whitespace nor rfc822 format, so pulling that out.
                                        //It should just be unique.
                                        //  Max Length = 256 -- See XL sheet
                                        mtChecks(entry, StringLiterals.Mail, 0, 256, null, null, true, false, false);
                                        //Updated check for MailNickName
                                        //  Cannot start with period (.)
                                        //  Max Length = 64, document doesn't restrict, schema says 64
                                        mtChecks(entry, StringLiterals.MailNickName, 0, 64, new Regex(@"^[.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), null, true, false, false);

                                        //ProxyAddresses have additional requirements. 
                                        //  Cannot contain space < > () ; , [ ] 
                                        //  There should be no ":" in the suffix part of ProxyAddresses.
                                        //  SMTP addresses should conform to valid email formats
                                        //
                                        // Considered and discarded. 
                                        // One option is that we do a format check for ProxyAddresses & TargetAddresses to be
                                        // <prefix>:<suffix>. In which case, we could pass in RegEx for the else part of smtp.IsMatch()
                                        // Instead, we'll just special case the check in mtChecks and get rid of the ":" in the suffix. 
                                        // That I think will benefit more customers, than giving a format error which they have to go and fix.
                                        //
                                        if (entry.Attributes.Contains(StringLiterals.ProxyAddresses))
                                        {
                                            Regex invalidProxyAddressSMTPRegEx = 
                                                new Regex(@"[\s<>()\;\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                            Regex invalidProxyAddressRegEx =
                                                new Regex(@"[\s<>()\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                            for (int i = 0; i <= entry.Attributes[StringLiterals.ProxyAddresses].Count - 1; i++)
                                            {
                                                bool isSmtp = smtp.IsMatch(entry.Attributes[StringLiterals.ProxyAddresses][i].ToString());
                                                mtChecks(entry, StringLiterals.ProxyAddresses, i, 256, 
                                                    isSmtp? invalidProxyAddressSMTPRegEx : invalidProxyAddressRegEx,
                                                    isSmtp? rfc2822 : null, 
                                                    true, false, false);
                                            }
                                        }

                                        //If UPN is valid, and samAccountName is Invalid, sync still works, so we check for invalid
                                        //SamAccountName only if UPN isn't valid
                                        //Max Length = 20
                                        //Invalid Characters [ \ " | , \ : <  > + = ? * ]
                                        if (entry.Attributes.Contains(StringLiterals.SamAccountName) && !IsValidUpn(entry))
                                        {
                                                mtChecks(entry, StringLiterals.SamAccountName, 0,
                                                    objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase) ? 20 : 256,
                                                    new Regex(@"[\\""|,/\[\]:<>+=;?*]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), null,
                                                    entry.Attributes.Contains(StringLiterals.UserPrincipalName) ? false : true, false, false);
                                        }

                                        //Checks for TargetAddress
                                        if (entry.Attributes.Contains(StringLiterals.TargetAddress))
                                        {
                                            //  Max Length = 255
                                            //TargetAddress cannot contain space \ < > ( ) ; , [ ] " 
                                            // There should be no ":" in the suffix part of TargetAddress
                                            //TargetAddress must be unique
                                            //SMTP should follow rfc2822
                                            Regex invalidTargetAddressSMTPRegEx =
                                                new Regex(@"[\s\\<>()\;\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                            Regex invalidTargetAddressRegEx = 
                                                new Regex(@"[\s\\<>()\,\[\]""]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                            bool isSmtp = smtp.IsMatch(entry.Attributes[StringLiterals.TargetAddress][0].ToString());
                                            mtChecks(entry, StringLiterals.TargetAddress, 0, 255, 
                                                isSmtp ? invalidTargetAddressSMTPRegEx : invalidTargetAddressRegEx,
                                                isSmtp ? rfc2822 : null, 
                                                true, false, false);
                                        }

                                        //Updated UPN 
                                        //  Should confirm to rfc2822 -- checked in mtChecks
                                        //  @ needs to be present -- checked in mtChecks as part of rfc2822
                                        //  Length before @ = 48 -- checked in mtChecks Length
                                        //  Length after  @ = 64 -- checked in mtChecks Length
                                        //  Cannot contain space \ % & * + / = ?  { } | < > ( ) ; : , [ ] “ umlaut -- RegEx
                                        //  @ cannot be first character -- RegEx
                                        //  period (.), ampersand (&), space, or at sign (@) cannot be the last character -- RegEx 
                                        //  No duplicates -- checked in mtChecks
                                        mtChecks(entry, StringLiterals.UserPrincipalName, 0, 113, invalidUpnRegEx, rfc2822, true, false, false);
                                        #endregion
                                    }
                                    else
                                    {
                                        #region do D checks
                                        if (dFilter(entry))
                                        {
                                            continue;
                                        }

                                        dChecks(entry, StringLiterals.DisplayName, 0, 256, new Regex(@"^[\s]+|[\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), null, false,
                                            objectType.Equals("group", StringComparison.CurrentCultureIgnoreCase) ? true : false);
                                        dChecks(entry, StringLiterals.Mail, 0, 256, new Regex(@"[\s]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), rfc2822, true, false);
                                        dChecks(entry, StringLiterals.MailNickName, 0, 64, new Regex(@"[\s\\!#$%&*+/=?^`{}|~<>()'\;\:\,\[\]""@]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), periods, true, true);
                                        if (entry.Attributes.Contains(StringLiterals.ProxyAddresses))
                                        {
                                            for (int i = 0; i <= entry.Attributes[StringLiterals.ProxyAddresses].Count - 1; i++)
                                            {
                                                dChecks(entry, StringLiterals.ProxyAddresses, i, 256, null,
                                                    smtp.IsMatch(entry.Attributes[StringLiterals.ProxyAddresses][i].ToString()) ? rfc2822 : null, true, false);
                                            }
                                        }
                                        // ### add check for sip proxies

                                        if (entry.Attributes.Contains(StringLiterals.TargetAddress))
                                        {
                                            mtChecks(entry, StringLiterals.TargetAddress, 0, 256, null,
                                                smtp.IsMatch(entry.Attributes[StringLiterals.TargetAddress][0].ToString()) ? rfc2822 : null, false, false, false);
                                        }
                                        else
                                        {
                                            dChecks(entry, StringLiterals.TargetAddress, 0, 256, null, null, false, true);
                                        }
                                        #endregion
                                    }
                                }
                                catch (Exception ex)
                                {
                                    BeginInvoke((MethodInvoker)delegate
                                    {
                                        statusDisplay(StringLiterals.Exception + StringLiterals.DistinguishedName + ": "
                                            + entry.Attributes[StringLiterals.DistinguishedName][0].ToString() + "  "
                                            + ex.Message);
                                    });
                                }
                                #endregion
                            }

                            #region another cookie?
                            BeginInvoke((MethodInvoker)delegate
                            {
                                statusDisplay("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
                                    + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
                                    + "  Duplicate Check Count: " + duplicateCount.ToString(CultureInfo.CurrentCulture));
                            });

                            // if this is true, there are no more pages to request
                            if (pageResponse.Cookie.Length == 0)
                                break;

                            pageRequest.Cookie = pageResponse.Cookie;
                            #endregion
                        }
                    }
                    #endregion

                    #region exit query
                    if (settingsAD)
                    {
                        // check to see if all forests have been queried
                        if ((forestListIndex + 1) == forestList.Count)
                        {
                            break;
                        }
                        else
                        {
                            forestListIndex++;
                        }
                    }
                    else
                    {
                        break;
                    }
                    #endregion
                }

                statusDisplay(StringLiterals.ElapsedTimeAdQuery + (DateTime.Now - stopwatch).ToString());
                // ###
                //BeginInvoke((MethodInvoker)delegate
                //{
                //    statusDisplay(StringLiterals.ElapsedTimeAdQuery + (DateTime.Now - stopwatch).ToString());
                //});  
                stopwatch = DateTime.Now;
                #endregion

                # region duplicate check
                if (e.Cancel)
                {
                    # region stop query
                    errorFile = "Error " + fileName;
                    if (File.Exists(errorFile))
                    {
                        File.Delete(errorFile);
                    }

                    duplicateFile = "Duplicate " + fileName;
                    if (File.Exists(duplicateFile))
                    {
                        File.Delete(duplicateFile);
                    }
                    #endregion
                }
                else
                {
                    # region check for an existing error on this attribute
                    string dupDn;
                    string dupAttribute;
                    string dupValue;
                    string errDictKey;
                    string dupUpdate;
                    ErrorClass errDictValue;
                    try
                    {
                        readDuplicateFile();
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            statusDisplay(StringLiterals.Exception + "readDuplicateFile() FAIL: " + ex.Message);
                        });
                        throw;
                    }

                    try
                    {
                        readErrorFile();
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            statusDisplay(StringLiterals.Exception + "readErrorFile() FAIL: " + ex.Message);
                        });
                        throw;
                    }

                    foreach (KeyValuePair<string, DuplicateClass> duplicatePair in dupObjDict)
                    {
                        dupDn = duplicatePair.Value.distinguishedName.ToUpperInvariant();
                        dupAttribute = duplicatePair.Value.attribute.ToUpperInvariant();
                        dupValue = duplicatePair.Value.value.ToUpperInvariant();
                        dupUpdate = String.Empty;

                        errDictKey = dupDn + "|" + dupAttribute + "|" + dupValue;

                        if (errDict.TryGetValue(errDictKey, out errDictValue))
                        {
                            errDictValue.type = errDictValue.type + StringLiterals.Duplicate;
                        }
                        else
                        {
                            errDictValue = new ErrorClass(duplicatePair.Value.distinguishedName,
                                duplicatePair.Value.objectClass,
                                duplicatePair.Value.attribute,
                                StringLiterals.Duplicate,
                                duplicatePair.Value.value,
                                duplicatePair.Value.update);
                            errDict.Add(errDictKey, errDictValue);
                            errorCount++;
                        }
                    }
                    dupDict.Clear();
                    dupObjDict.Clear();
                    BeginInvoke((MethodInvoker)delegate
                    {
                        statusDisplay(StringLiterals.ElapsedTimeDuplicateChecks + (DateTime.Now - stopwatch).ToString());
                    });
                    stopwatch = DateTime.Now;
                    #endregion
                }
                #endregion
            }
            #region Exceptions
            catch (DirectoryServicesCOMException ex)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show(StringLiterals.DirectoryServicesCOMException + ex.Message,
                        StringLiterals.AdQueryFail,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                    statusDisplay(StringLiterals.Exception + ex.Message);
                });
                e.Result = StringLiterals.Fail;
            }
            catch (DirectoryNotFoundException ex)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show(StringLiterals.DirectoryNotFoundException + ex.Message,
                        StringLiterals.AdQueryFail,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                    statusDisplay(StringLiterals.Exception + ex.Message);
                });
                e.Result = StringLiterals.Fail;
            }
            catch (DirectoryOperationException ex)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show(StringLiterals.DirectoryOperationException + ex.Message,
                        StringLiterals.AdQueryFail,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                    statusDisplay(StringLiterals.Exception + ex.Message);
                });
                e.Result = StringLiterals.Fail;
            }
            catch (DirectoryException ex)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show(StringLiterals.DirectoryException + ex.Message,
                        StringLiterals.AdQueryFail,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                    statusDisplay(StringLiterals.Exception + ex.Message);
                });
                e.Result = StringLiterals.Fail;
            }
            catch (Exception ex)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    MessageBox.Show(StringLiterals.Exception + ex.Message,
                        StringLiterals.AdQueryFail,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                    statusDisplay(StringLiterals.Exception + ex.Message);
                });
                e.Result = StringLiterals.Fail;
            }
            #endregion
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                firstRun = false; 
                //Oh cool, we don't have to enable anything.
                queryToolStripMenuItem.Enabled = true;
                cancelToolStripMenuItem.Enabled = false;
                acceptToolStripMenuItem.Enabled = true;
                applyToolStripMenuItem.Enabled = true;
                exportToolStripMenuItem.Enabled = true;
                importToolStripMenuItem.Enabled = true;
                undoToolStripMenuItem.Enabled = true;

                if (e.Error != null)
                {
                    MessageBox.Show(e.Error.Message);
                }
                else if (e.Cancelled)
                {
                    statusDisplay(StringLiterals.CancelQuery);
                }
                else
                {
                    string argumentTest = e.Result as string;
                    if (argumentTest.Equals(StringLiterals.Complete, StringComparison.CurrentCultureIgnoreCase))
                    {
                        statusDisplay("Populating DataGrid");
                        int newRow = 0;
                        foreach (KeyValuePair<string, ErrorClass> errorPair in errDict)
                        {
                            dataGridView1.Rows.Add();
                            dataGridView1.Rows[newRow].Cells[StringLiterals.DistinguishedName].Value = errorPair.Value.distinguishedName;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.ObjectClass].Value = errorPair.Value.objectClass;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Attribute].Value = errorPair.Value.attribute;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Error].Value = errorPair.Value.type.Substring(0, errorPair.Value.type.Length - 1);
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Value].Value = errorPair.Value.value;
                            dataGridView1.Rows[newRow].Cells[StringLiterals.Update].Value = errorPair.Value.update;
                            newRow++;
                            if (newRow >= blockSize)
                            {
                                displayCount = newRow;
                                nextToolStripMenuItem.Visible = true;
                                previousToolStripMenuItem.Visible = true;
                                break;
                            }
                        }

                        dataGridView1.Sort(dataGridView1.Columns[StringLiterals.DistinguishedName], ListSortDirection.Ascending);
                        if (dataGridView1.RowCount >= 1)
                        {
                            dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[StringLiterals.DistinguishedName];
                        }
                        statusDisplay(StringLiterals.ElapsedTimePopulateDataGridView + (DateTime.Now - stopwatch).ToString());
                        if (errorCount > blockSize)
                        {
                            statusDisplay("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
                                + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture)
                                + "  Displayed Count: " + dataGridView1.Rows.Count.ToString(CultureInfo.CurrentCulture));
                        }
                        else
                        {
                            statusDisplay("Query Count: " + entryCount.ToString(CultureInfo.CurrentCulture)
                                + "  Error Count: " + errorCount.ToString(CultureInfo.CurrentCulture));
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                this.Invalidate();
                statusDisplay(StringLiterals.Exception + StringLiterals.Threadsafe + "  " + ex.Message);
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
                        if(errorAttribute.Equals(StringLiterals.ProxyAddresses, StringComparison.CurrentCultureIgnoreCase) ||
                            errorAttribute.Equals(StringLiterals.TargetAddress, StringComparison.CurrentCultureIgnoreCase)
                          )
                        {
                            int colonPosn = errorUpdate.IndexOf(":");
                            if(colonPosn > 0)
                            {
                                //In case the suffix has a colon, we need to show a character error.
                                //And replace it.
                                string suffix = errorUpdate.Substring(colonPosn+1);
                                if(suffix.Contains(":")) 
                                {
                                    errorAdd = true;
                                    errorString += StringLiterals.Character;
                                     
                                    errorUpdate = 
                                        errorUpdate.Substring(0, colonPosn)  + ":"  + 
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
                                    if (!tldDict.ContainsKey(tldDomain))
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
                    if(errorAttribute.Equals(StringLiterals.UserPrincipalName, StringComparison.CurrentCultureIgnoreCase))
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

                        using (StreamWriter duplicate = new StreamWriter(duplicateFile, true))
                        {
                            duplicate.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                            duplicate.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                            duplicate.WriteLine("AT:" + errorAttribute);
                            duplicate.WriteLine("VL:" + entry.Attributes[errorAttribute][errorIndex].ToString());
                            duplicate.WriteLine("UP:" + dupUpdate);
                            duplicate.WriteLine("---");
                        }
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
                    using (StreamWriter swError = new StreamWriter(errorFile, true))
                    {
                        swError.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                        swError.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                        swError.WriteLine("AT:" + errorAttribute);
                        swError.WriteLine("ER:" + errorString);
                        swError.WriteLine("VL:" + (entry.Attributes.Contains(errorAttribute) ? entry.Attributes[errorAttribute][errorIndex].ToString() : String.Empty));
                        swError.WriteLine("UP:" + errorUpdate);
                        swError.WriteLine("---");
                    }
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
                                    if (!tldDict.ContainsKey(tldDomain))
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

                        using (StreamWriter duplicate = new StreamWriter(duplicateFile, true))
                        {
                            duplicate.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                            duplicate.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                            duplicate.WriteLine("AT:" + errorAttribute);
                            duplicate.WriteLine("VL:" + entry.Attributes[errorAttribute][errorIndex].ToString());
                            duplicate.WriteLine("UP:" + dupUpdate);
                            duplicate.WriteLine("---");
                        }
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
                    using (StreamWriter swError = new StreamWriter(errorFile, true))
                    {
                        swError.WriteLine("DN:" + entry.Attributes[StringLiterals.DistinguishedName][0].ToString());
                        swError.WriteLine("OB:" + entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString());
                        swError.WriteLine("AT:" + errorAttribute);
                        swError.WriteLine("ER:" + errorString);
                        swError.WriteLine("VL:" + (entry.Attributes.Contains(errorAttribute) ? entry.Attributes[errorAttribute][errorIndex].ToString() : String.Empty));
                        swError.WriteLine("UP:" + errorUpdate);
                        swError.WriteLine("---");
                    }
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
                     if (!tldDict.ContainsKey(tldDomain))
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

                if (File.Exists(duplicateFile))
                {
                    using (StreamReader duplicate = new StreamReader(duplicateFile))
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
                            while ((line = duplicate.ReadLine()) != null)
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
                                                using (StreamWriter merge = new StreamWriter(mergeFile, true))
                                                {
                                                    merge.WriteLine(splitValue[i]);
                                                }
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
                            if (File.Exists(mergeFile))
                            {
                                using (StreamReader merge = new StreamReader(mergeFile))
                                {
                                    while ((line = merge.ReadLine()) != null)
                                    {
                                        if (line == lastLine && line != lastWritten)
                                        {
                                            dupDict.Add(line, 2);
                                            lastWritten = line;
                                        }
                                        lastLine = line;
                                    }
                                }
                                File.Delete(mergeFile);
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
                    }

                    if (dupDict.Count >= 1)
                    {
                        #region write filtered duplicate objects
                        statusDisplay("Write filtered duplicate objects");
                        using (StreamReader duplicate = new StreamReader(duplicateFile))
                        {
                            try
                            {
                                while ((line = duplicate.ReadLine()) != null)
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
                                                        using (StreamWriter filtered = new StreamWriter(filteredFile, true))
                                                        {
                                                            filtered.WriteLine("DN:" + dn);
                                                            filtered.WriteLine("OB:" + ob);
                                                            filtered.WriteLine("AT:" + at);
                                                            filtered.WriteLine("VL:" + vl);
                                                            filtered.WriteLine("UP:" + up);
                                                            filtered.WriteLine("---");
                                                        }
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
                        }
                        dupDict.Clear();
                        dupObjDict.Clear();
                        #endregion

                        #region read filtered duplicate objects
                        if (File.Exists(filteredFile))
                        {
                            statusDisplay("Read filtered duplicate objects");
                            using (StreamReader filtered = new StreamReader(filteredFile))
                            {
                                try
                                {
                                    while ((line = filtered.ReadLine()) != null)
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
                            }
                            File.Delete(filteredFile);
                        }
                        #endregion
                    }
                    else
                    {
                        statusDisplay("No duplicate values in file");
                    }
                    File.Delete(duplicateFile);
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
                if (File.Exists(errorFile))
                {
                    statusDisplay("Read error file");
                    using (StreamReader srError = new StreamReader(errorFile))
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
                            while ((line = srError.ReadLine()) != null)
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
                    }
                    File.Delete(errorFile);
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
                using (StreamWriter verbose = new StreamWriter(verboseFile, true))
                {
                    verbose.WriteLine(DateTime.Now.ToString() + " " + toolStripStatusLabel1.Text);
                }
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
                    using (StreamWriter apply = new StreamWriter(applyFile, true))
                    {
                        apply.WriteLine("distinguishedName: " + wuDistinguishedName);
                        apply.WriteLine("objectClass: " + wuObjectClass);
                        apply.WriteLine("attribute: " + wuAttribute);
                        apply.WriteLine("error: " + wuError);
                        apply.WriteLine("value: " + wuValue);
                        apply.WriteLine("update: " + wuUpdate);
                        apply.WriteLine("action: " + wuAction);
                        apply.WriteLine("-");
                        apply.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + StringLiterals.WriteUpdate + "  " + ex.Message);
                throw;
            }
        }
        #endregion

        #region filtering
        private Boolean mtFilter(SearchResultEntry entry)
        {
            string objectDn = String.Empty;
            try
            {
                objectDn = entry.Attributes[StringLiterals.DistinguishedName][0].ToString();

                // Active Directory Synchronization in Office 365
                // http://technet.microsoft.com/en-us/library/hh852469.aspx#bkmk_adcleanup
                // Remove duplicate proxyAddress and userPrincipalName attributes.
                // Update blank and invalid userPrincipalName attributes with a valid userPrincipalName.
                // Remove invalid and questionable characters in the givenName, surname (sn), sAMAccountName, displayName, 
                // mail, proxyAddresses, mailNickname, and userPrincipalName attributes. 

                // Appendix F Directory Object Preparation
                // http://technet.microsoft.com/en-us/library/hh852533.aspx

                // Any object is filtered if:
                #region all object filter
                // match well known exclusion pattern
                if (entry.Attributes[StringLiterals.Cn][0].ToString().EndsWith("$", StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }

                foreach (string exclusion in wellKnownExclusions)
                {
                    if (entry.Attributes[StringLiterals.Cn][0].ToString().ToUpperInvariant().StartsWith(exclusion.ToUpperInvariant(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                // •Object is a conflict object (DN contains \0ACNF: )
                if (entry.Attributes[StringLiterals.DistinguishedName][0].ToString().IndexOf("\0ACNF:", StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    return true;
                }

                //  •isCriticalSystemObject is present
                if (entry.Attributes.Contains(StringLiterals.IsCriticalSystemObject))
                {
                    if (Convert.ToBoolean(entry.Attributes[StringLiterals.IsCriticalSystemObject][0].ToString(), CultureInfo.CurrentCulture) == true)
                    {
                        return true;
                    }
                }

                // determine objectClass
                string objectType = entry.Attributes[StringLiterals.ObjectClass][entry.Attributes[StringLiterals.ObjectClass].Count - 1].ToString();
                #endregion

                // User objects are filtered if:
                # region user filter
                if (objectType.Equals("user", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (entry.Attributes.Contains(StringLiterals.SamAccountName))
                    {
                        // SamAccountName exclusions
                        if (entry.Attributes[StringLiterals.SamAccountName][0].ToString().ToUpperInvariant().StartsWith("CAS_", StringComparison.CurrentCultureIgnoreCase)
                            || entry.Attributes[StringLiterals.SamAccountName][0].ToString().ToUpperInvariant().StartsWith("SUPPORT_", StringComparison.CurrentCultureIgnoreCase)
                            || entry.Attributes[StringLiterals.SamAccountName][0].ToString().ToUpperInvariant().StartsWith("MSOL_", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        //  •sAMAccountName is not present
                        //return true;
                    }

                    if (entry.Attributes.Contains(StringLiterals.MailNickName))
                    {
                        // mailNickName exclusions
                        if (entry.Attributes[StringLiterals.MailNickName][0].ToString().ToUpperInvariant().StartsWith("CAS_", StringComparison.CurrentCultureIgnoreCase)
                            || entry.Attributes[StringLiterals.MailNickName][0].ToString().ToUpperInvariant().StartsWith("SYSTEMMAILBOX", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                    }

                    if (entry.Attributes.Contains(StringLiterals.DisplayName)
                        && entry.Attributes.Contains(StringLiterals.MsExchHideFromAddressLists))
                    {
                        if (Convert.ToBoolean(entry.Attributes[StringLiterals.MsExchHideFromAddressLists][0].ToString(), CultureInfo.CurrentCulture) == true
                            && entry.Attributes[StringLiterals.DisplayName][0].ToString().ToUpperInvariant().StartsWith("MSOL", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                    }

                    //  •msExchRecipientTypeDetails == (0x1000 OR 0x2000 OR 0x4000 OR 0x400000 OR 0x800000 OR 0x1000000 OR 0x20000000)
                    if (entry.Attributes.Contains(StringLiterals.MsExchRecipientTypeDetails))
                    {
                        switch (entry.Attributes[StringLiterals.MsExchRecipientTypeDetails][0].ToString())
                        {
                            case "0x1000":
                                return true;
                            case "0x2000":
                                return true;
                            case "0x4000":
                                return true;
                            case "0x400000":
                                return true;
                            case "0x800000":
                                return true;
                            case "0x1000000":
                                return true;
                            case "0x20000000":
                                return true;
                        }
                    }
                }
                # endregion

                // Group objects are filter if:
                # region group filter
                // List of attributes that are synchronized to Office 365 and attributes that are written back to the on-premises Active Directory Domain Services
                // http://support.microsoft.com/kb/2256198
                // NOTE:  these are listed as group filters in the article, but they appear to be an inaccurate mix of filters and errors.
                // SecurityEnabledGroup objects are filtered if: 
                //  •isCriticalSystemObject = TRUE
                //  •mail is present AND DisplayName is not present
                //  •Group has more than 15,000 immediate members
                // MailEnabledGroup objects are filtered if: 
                //  •DisplayName is empty
                //  •(ProxyAddress does not have a primary SMTP address) AND (mail attribute is not present/invalid - i.e. indexof ('@') <= 0)
                //  •Group has more than 15,000 immediate members

                if (objectType.Equals("group", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Convert.ToInt64(entry.Attributes[StringLiterals.GroupType][0].ToString(), CultureInfo.CurrentCulture) < 0)
                    // security group
                    {
                        if (!entry.Attributes.Contains(StringLiterals.Mail))
                        {
                            return true;
                        }
                    }
                    else
                    // distribution group
                    {
                        if (!entry.Attributes.Contains(StringLiterals.ProxyAddresses))
                        {
                            return true;
                        }
                    }
                }
                return false;
                #endregion
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + "Result Filter: " + objectDn + "  " + ex.Message);
            }

            return false;
        }

        private Boolean dFilter(SearchResultEntry entry)
        {
            string objectDn = String.Empty;
            try
            {
                objectDn = entry.Attributes[StringLiterals.DistinguishedName][0].ToString();

                // Any object is filtered if:
                #region all object filter
                // match well known exclusion pattern
                if (entry.Attributes[StringLiterals.Cn][0].ToString().EndsWith("$", StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }

                foreach (string exclusion in wellKnownExclusions)
                {
                    if (entry.Attributes[StringLiterals.Cn][0].ToString().ToUpperInvariant().StartsWith(exclusion.ToUpperInvariant(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                // •Object is a conflict object (DN contains \0ACNF: )
                if (entry.Attributes[StringLiterals.DistinguishedName][0].ToString().IndexOf("\0ACNF:", StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    return true;
                }

                //  •isCriticalSystemObject is present
                if (entry.Attributes.Contains(StringLiterals.IsCriticalSystemObject))
                {
                    if (Convert.ToBoolean(entry.Attributes[StringLiterals.IsCriticalSystemObject][0].ToString(), CultureInfo.CurrentCulture) == true)
                    {
                        return true;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                statusDisplay(StringLiterals.Exception + "Result Filter: " + objectDn + "  " + ex.Message);
            }

            return false;
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

        public string GetScope(string selectedForest, string user, string password)
        {
            DirectoryContext dcF = null;
            try
            {
                if (!String.IsNullOrEmpty(user) && !String.IsNullOrEmpty(password))
                    dcF = new DirectoryContext(DirectoryContextType.Forest, selectedForest, user, password);
                else
                    dcF = new DirectoryContext(DirectoryContextType.Forest, selectedForest);
                //We get the currently selected forest and use that to construct the scope.

                Forest f = Forest.GetForest(dcF);
                return "dc=" + f.Name.Replace(".", ",dc=");
            }
            catch(Exception)
            {
                return String.Empty;
            }
        }

    }
}
