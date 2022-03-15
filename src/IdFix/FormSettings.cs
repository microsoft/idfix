﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IdFix.Settings;

namespace IdFix
{
    public partial class FormSettings : Form
    {
        private SetDisplayDelegate _setDisplay;

        public FormSettings(SetDisplayDelegate setDisplay)
        {
            this._setDisplay = setDisplay;

            try
            {
                InitializeComponent();

                // need to merge in the active list and the found list
                var forests = SettingsManager.Instance.ForestList;
                foreach (string forest in forests.Concat(SettingsManager.Instance.ActiveForestList).Distinct().OrderBy(t => t))
                {
                    checkedListBoxAD.Items.Add(forest, SettingsManager.Instance.ActiveForestList.Length == 0 || SettingsManager.Instance.ActiveForestList.Contains(forest));
                }

                // fill in domain and server
                textBoxDomain.Text = SettingsManager.Instance.DistinguishedName;
                textBoxServer.Text = SettingsManager.Instance.Server;

                // fill in port and filter
                comboBoxPort.Text = SettingsManager.Instance.Port.ToString();
                textBoxFilter.Text = SettingsManager.Instance.Filter;

                if (SettingsManager.Instance.CurrentRuleMode == RuleMode.MultiTenant)
                {
                    radioButtonMT.Checked = true;
                }
                else
                {
                    radioButtonD.Checked = true;
                }

                if (SettingsManager.Instance.UseAlternateLogin)
                {
                    chk_alternateloginid.Checked = true;
                }
                else
                {
                    chk_alternateloginid.Checked = false;
                }

                if (SettingsManager.Instance.SearchBaseEnabled) //re-entrant case.
                {
                    searchBaseCheckBox.Checked = true;
                    textBoxSearchBase.Enabled = true;
                    textBoxSearchBase.Text = SettingsManager.Instance.SearchBase;
                }
                else
                {
                    searchBaseCheckBox.Checked = false;
                    textBoxSearchBase.Enabled = false;
                }

                if (SettingsManager.Instance.CurrentDirectoryType == DirectoryType.ActiveDirectory)
                {
                    radioButtonAD.Checked = true;
                    checkedListBoxAD.Enabled = true;
                    textBoxAD.Enabled = true;
                    forestButton.Enabled = true;
                    textBoxDomain.Enabled = false;
                    textBoxServer.Enabled = false;
                }
                else
                {
                    radioButtonLDAP.Checked = true;
                    checkedListBoxAD.Enabled = false;
                    textBoxAD.Enabled = false;
                    forestButton.Enabled = false;
                    textBoxDomain.Enabled = true;
                    textBoxServer.Enabled = true;
                }

                if (SettingsManager.Instance.CurrentCredentialMode == CredentialMode.CurrentUser)
                {
                    radioButtonCurrent.Checked = true;
                    textBoxUser.Enabled = false;
                    textBoxPassword.Enabled = false;
                }
                else
                {
                    radioButtonOther.Checked = true;
                    textBoxUser.Enabled = true;
                    textBoxPassword.Enabled = true;
                }

                // Populate auth type combobox
                comboBoxAuthType.Items.AddRange(Enum.GetNames(typeof(AuthType)));
                comboBoxAuthType.SelectedItem = SettingsManager.Instance.AuthType.ToString();
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Settings - " + ex.Message);
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            try
            {
                int port = string.IsNullOrEmpty(comboBoxPort.Text) ? 0 : int.Parse(comboBoxPort.Text);

                if (port != Constants.GlobalCatalogPort)
                {
                    if (MessageBox.Show(this, StringLiterals.LdapPortWarningMessage, StringLiterals.LdapPortWarningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return;
                    }
                }

                SettingsManager.Instance.Filter = textBoxFilter.Text;
                SettingsManager.Instance.Port = port;

                if (SettingsManager.Instance.CurrentDirectoryType == DirectoryType.ActiveDirectory)
                {
                    var tempList = new List<string>();
                    foreach (object item in checkedListBoxAD.CheckedItems)
                    {
                        tempList.Add(item.ToString());
                    }
                    SettingsManager.Instance.ActiveForestList = tempList.ToArray();

                    //If the checkbox is checked, we set the searchBase
                    if (searchBaseCheckBox.Checked)
                    {
                        if (String.IsNullOrEmpty(textBoxSearchBase.Text) || checkedListBoxAD.CheckedItems.Count > 1)
                        {
                            //If they checked it and nothing is set, then we clear the check box and set state.
                            searchBaseCheckBox.Checked = false;
                            SettingsManager.Instance.SearchBaseEnabled = false;
                            SettingsManager.Instance.SearchBase = String.Empty;
                        }
                        else
                        {
                            SettingsManager.Instance.SearchBaseEnabled = true;
                            SettingsManager.Instance.SearchBase = textBoxSearchBase.Text;
                        }
                    }
                }
                else
                {
                    SettingsManager.Instance.Server = textBoxServer.Text;
                    SettingsManager.Instance.DistinguishedName = textBoxDomain.Text;
                }

                if (SettingsManager.Instance.CurrentCredentialMode != CredentialMode.CurrentUser)
                {
                    SettingsManager.Instance.Username = textBoxUser.Text;
                    SettingsManager.Instance.Password = textBoxPassword.Text;
                }

                SettingsManager.Instance.AuthType = (AuthType)Enum.Parse(typeof(AuthType), comboBoxAuthType.SelectedItem.ToString());

                this.Close();
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Settings - " + ex.Message);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Cancel - " + ex.Message);
            }
        }

        private void radioButtonMT_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Instance.CurrentRuleMode = RuleMode.MultiTenant;
                textBoxFilter.Text = SettingsManager.DefaultMTFilter;
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Rules Multi-Tenant - " + ex.Message);
            }
        }
               
        private void radioButtonD_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Instance.CurrentRuleMode = RuleMode.Dedicated;
                textBoxFilter.Text = SettingsManager.DefaultDedicatedFilter;
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Rules Multi-Tenant - " + ex.Message);
            }
        }

        private void forestButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(textBoxAD.Text))
                {
                    checkedListBoxAD.Items.Add(textBoxAD.Text, true);
                    textBoxAD.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Add Forest - " + ex.Message);
            }
        }

        private void radioButtonAD_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Instance.CurrentDirectoryType = DirectoryType.ActiveDirectory;
                checkedListBoxAD.Enabled = true;
                textBoxAD.Enabled = true;
                forestButton.Enabled = true;
                textBoxDomain.Enabled = false;
                textBoxServer.Enabled = false;
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Active Directory - " + ex.Message);
            }
        }

        private void radioButtonLDAP_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Instance.CurrentDirectoryType = DirectoryType.LDAP;
                checkedListBoxAD.Enabled = false;
                textBoxAD.Enabled = false;
                forestButton.Enabled = false;
                textBoxDomain.Enabled = true;
                textBoxServer.Enabled = true;
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "LDAP - " + ex.Message);
            }
        }

        private void radioButtonCurrent_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Instance.CurrentCredentialMode = CredentialMode.CurrentUser;
                textBoxUser.Enabled = false;
                textBoxPassword.Enabled = false;
                textBoxUser.Text = string.Empty;
                textBoxPassword.Text = string.Empty;
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Credentials Current - " + ex.Message);
            }
        }

        private void radioButtonOther_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Instance.CurrentCredentialMode = CredentialMode.Specified;
                textBoxUser.Enabled = true;
                textBoxPassword.Enabled = true;
                textBoxUser.Text = SettingsManager.Instance.Username;
                textBoxPassword.Text = SettingsManager.Instance.Password;
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Credentials Other - " + ex.Message);
            }
        }

        private void searchBaseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (searchBaseCheckBox.Checked)
            {
                //First we check if there is any forest selected
                if (checkedListBoxAD.CheckedItems.Count == 1)
                {
                    SettingsManager.Instance.SearchBaseEnabled = true;
                    textBoxSearchBase.Enabled = true;
                    // We need to put in default value
                    ResetSearchBase(0);
                }
                else // This covers 0 or more than 1
                {
                    //If we so we disable the checkbox and set the SearchBase to empty
                    SettingsManager.Instance.SearchBaseEnabled = false;
                    textBoxSearchBase.Enabled = false;
                    textBoxSearchBase.Text = String.Empty;
                    searchBaseCheckBox.Checked = false;
                }
            }
            else
            {
                SettingsManager.Instance.SearchBaseEnabled = false;
                textBoxSearchBase.Enabled = false;
                textBoxSearchBase.Text = String.Empty;
            }
        }

        private void checkedListBoxAD_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //If nothing is selected, reset the boxes.
            switch (checkedListBoxAD.CheckedItems.Count)
            {
                case 0:
                    if (e.NewValue == CheckState.Checked)
                    {
                        //A new value is added
                        if (searchBaseCheckBox.Checked)
                        {
                            //The problem here is that nothing is marked as checked at this point.
                            //So we have to assume that this happened at that index.
                            ResetSearchBase(e.Index);
                            //Enable the textbox too
                            textBoxSearchBase.Enabled = true;
                        }
                    }
                    break;
                case 1:
                    //If we go from 1->0 or 1->2, we have to uncheck.
                    if (searchBaseCheckBox.Checked)
                    {
                        searchBaseCheckBox.Checked = false;
                        textBoxSearchBase.Enabled = false;
                        textBoxSearchBase.Text = String.Empty;
                    }
                    break;
                case 2:
                    if (e.NewValue == CheckState.Unchecked)
                    {
                        if (searchBaseCheckBox.Checked)
                        {
                            //This will end up being one item checked.
                            //Reset searchBase.
                            int index = e.Index == 0 ? 1 : 0;
                            ResetSearchBase(index);
                            textBoxSearchBase.Enabled = true;
                        }
                    }
                    break;
            }
        }

        internal void ResetSearchBase(int index)
        {
            if (!string.IsNullOrEmpty(SettingsManager.Instance.SearchBase))
            {
                textBoxSearchBase.Text = SettingsManager.Instance.SearchBase;
                return;
            }

            string user = SettingsManager.Instance.CurrentCredentialMode == CredentialMode.CurrentUser ? null : SettingsManager.Instance.Username;
            string password = SettingsManager.Instance.CurrentCredentialMode == CredentialMode.CurrentUser ? null : SettingsManager.Instance.Password;
            string selectedForest = null;

            //We need to guard for the case where the item will be checked after the fact.
            if (checkedListBoxAD.CheckedItems.Count == 0)
            {
                selectedForest = checkedListBoxAD.Items[index].ToString();
            }
            else
            {
                selectedForest = checkedListBoxAD.CheckedItems[index].ToString();
            }

            textBoxSearchBase.Text = FormSettings.GetScope(selectedForest, user, password);
        }

        private void chk_alternateloginid_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Instance.UseAlternateLogin = chk_alternateloginid.Checked;
            }
            catch (Exception ex)
            {
                this._setDisplay(StringLiterals.Exception + "Rules AlternateLoginID - " + ex.Message);
            }
        }

        private static string GetScope(string selectedForest, string user, string password)
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
            catch (Exception)
            {
                return String.Empty;
            }
        }
    }
}
