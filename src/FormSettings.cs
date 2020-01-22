using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IdFix
{
    public partial class FormSettings : Form
    {
        private Form1 myParent;

        public FormSettings(Form1 frm1)
        {
            try
            {
                InitializeComponent();
                myParent = frm1;

                foreach (string forest in myParent.forestList)
                {
                    checkedListBoxAD.Items.Add(forest, true);
                }
                textBoxDomain.Text = myParent.targetSearch;
                textBoxServer.Text = myParent.serverName;
                comboBoxPort.Text = myParent.ldapPort;
                textBoxFilter.Text = myParent.ldapFilter;

                if (myParent.settingsMT)
                {
                    radioButtonMT.Checked = true;
                }
                else
                {
                    radioButtonD.Checked = true;
                }

                if (myParent.searchBaseEnabled) //re-entrant case.
                {
                    searchBaseCheckBox.Checked = true;
                    textBoxSearchBase.Enabled = true;
                    textBoxSearchBase.Text = myParent.searchBase;
                }
                else
                {
                    searchBaseCheckBox.Checked = false;
                    textBoxSearchBase.Enabled = false;
                }

                if (myParent.settingsAD)
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

                if (myParent.settingsCU)
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
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Settings - " + ex.Message);
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            try
            {
                myParent.ldapFilter = textBoxFilter.Text;
                myParent.ldapPort = comboBoxPort.Text;

                if (myParent.settingsAD)
                {
                    myParent.forestList = new List<string>();
                    foreach (object item in checkedListBoxAD.CheckedItems)
                    {
                        myParent.forestList.Add(item.ToString());
                    }
                    //If the checkbox is checked, we set the searchBase
                    if(searchBaseCheckBox.Checked)
                    {
                        if (String.IsNullOrEmpty(textBoxSearchBase.Text) || checkedListBoxAD.CheckedItems.Count > 1)
                        {
                            //If they checked it and nothing is set, then we clear the check box and set state.
                            searchBaseCheckBox.Checked = false;
                            myParent.searchBaseEnabled = false;
                            myParent.searchBase = String.Empty;
                        }
                        else
                        {
                            myParent.searchBaseEnabled = true;
                            myParent.searchBase = textBoxSearchBase.Text;
                        } 
                    }
                    else //This is for the re-entrant case. UI shows no SearchBase, so we should do that.
                    {
                        myParent.searchBase = String.Empty;
                    }
                }
                else
                {
                    myParent.serverName = textBoxServer.Text;
                    myParent.targetSearch = textBoxDomain.Text;
                }

                if (myParent.settingsMT)
                {
                    myParent.attributesToReturn = new string[] { StringLiterals.Cn, StringLiterals.DistinguishedName, 
                    StringLiterals.GroupType, StringLiterals.HomeMdb, StringLiterals.IsCriticalSystemObject, StringLiterals.Mail,
                    StringLiterals.MailNickName, StringLiterals.MsExchHideFromAddressLists,
                    StringLiterals.MsExchRecipientTypeDetails, StringLiterals.ObjectClass, StringLiterals.ProxyAddresses, 
                    StringLiterals.SamAccountName, StringLiterals.TargetAddress, StringLiterals.UserPrincipalName };
                }
                else
                {
                    myParent.attributesToReturn = new string[] { StringLiterals.Cn, StringLiterals.DisplayName, StringLiterals.DistinguishedName, StringLiterals.GivenName, 
                    StringLiterals.HomeMdb, StringLiterals.Mail, StringLiterals.MailNickName, StringLiterals.ObjectClass, StringLiterals.ProxyAddresses, 
                    StringLiterals.SamAccountName, StringLiterals.Sn, StringLiterals.TargetAddress };
                }

                if (!myParent.settingsCU)
                {
                    myParent.user = textBoxUser.Text;
                    myParent.password = textBoxPassword.Text;
                }

                this.Close();
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Settings - " + ex.Message);
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
                myParent.statusDisplay(StringLiterals.Exception + "Cancel - " + ex.Message);
            }
        }

        private void radioButtonMT_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                myParent.settingsMT = true;
                textBoxFilter.Text = "(|(objectCategory=Person)(objectCategory=Group))";
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Rules Multi-Tenant - " + ex.Message);
            }
        }

        private void radioButtonD_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                myParent.settingsMT = false;
                textBoxFilter.Text = "(&(mail=*)(|(objectCategory=Person)(objectCategory=Group)))";
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Rules Multi-Tenant - " + ex.Message);
            }
        }

        private void forestButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(textBoxAD.Text))
                {
                    checkedListBoxAD.Items.Add(textBoxAD.Text, true);
                }
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Add Forest - " + ex.Message);
            }
        }

        private void radioButtonAD_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                myParent.settingsAD = true;
                checkedListBoxAD.Enabled = true;
                textBoxAD.Enabled = true;
                forestButton.Enabled = true;
                textBoxDomain.Enabled = false;
                textBoxServer.Enabled = false;
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Active Directory - " + ex.Message);
            }
        }

        private void radioButtonLDAP_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                myParent.settingsAD = false;
                checkedListBoxAD.Enabled = false;
                textBoxAD.Enabled = false;
                forestButton.Enabled = false;
                textBoxDomain.Enabled = true;
                textBoxServer.Enabled = true;
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "LDAP - " + ex.Message);
            }
        }

        private void radioButtonCurrent_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                myParent.settingsCU = true;
                textBoxUser.Enabled = false;
                textBoxPassword.Enabled = false;
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Credentials Current - " + ex.Message);
            }
        }

        private void radioButtonOther_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                myParent.settingsCU = false;
                textBoxUser.Enabled = true;
                textBoxPassword.Enabled = true;
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "Credentials Other - " + ex.Message);
            }
        }

        private void searchBaseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if(searchBaseCheckBox.Checked)
            {
                //First we check if there is any forest selected
                if (checkedListBoxAD.CheckedItems.Count == 1) 
                {
                    myParent.searchBaseEnabled = true;
                    textBoxSearchBase.Enabled = true;
                    //We need to put in default value
                    ResetSearchBase(0);
                }
                else //This covers 0 or more than 1
                {
                    //If we so we disable the checkbox and set the SearchBase to empty
                    textBoxSearchBase.Enabled = false;
                    textBoxSearchBase.Text = String.Empty;
                    searchBaseCheckBox.Checked = false;
                }
            }
            else
            {
                textBoxSearchBase.Enabled = false;
                textBoxSearchBase.Text = String.Empty;
            }
        }

        private void checkedListBoxAD_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //If nothing is selected, reset the boxes.
            switch(checkedListBoxAD.CheckedItems.Count)
            {
                case 0:
                    if(e.NewValue == CheckState.Checked)
                    {
                        //A new value is added
                        if(searchBaseCheckBox.Checked)
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
                    if(searchBaseCheckBox.Checked)
                    {
                        searchBaseCheckBox.Checked = false;
                        textBoxSearchBase.Enabled = false;
                        textBoxSearchBase.Text = String.Empty;
                    }
                    break;
                case 2:
                    if(e.NewValue == CheckState.Unchecked)
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
            string user = myParent.settingsCU ? null : textBoxUser.Text;
            string password = myParent.settingsCU ? null : textBoxPassword.Text;
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
            string newForestDN = myParent.GetScope(selectedForest, user, password);
            textBoxSearchBase.Text = newForestDN;
        }
    }
}
