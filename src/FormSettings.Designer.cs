// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IdFix
{
    partial class FormSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSettings));
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.Rul = new System.Windows.Forms.Label();
            this.radioButtonMT = new System.Windows.Forms.RadioButton();
            this.radioButtonD = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.radioButtonAD = new System.Windows.Forms.RadioButton();
            this.radioButtonLDAP = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxAD = new System.Windows.Forms.TextBox();
            this.forestButton = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.textBoxDomain = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textBoxServer = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.comboBoxPort = new System.Windows.Forms.ComboBox();
            this.textBoxFilter = new System.Windows.Forms.TextBox();
            this.textBoxSearchBase = new System.Windows.Forms.TextBox();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxUser = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.radioButtonCurrent = new System.Windows.Forms.RadioButton();
            this.radioButtonOther = new System.Windows.Forms.RadioButton();
            this.label3 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.checkedListBoxAD = new System.Windows.Forms.CheckedListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.searchBaseLabel = new System.Windows.Forms.Label();
            this.searchBaseCheckBox = new System.Windows.Forms.CheckBox();
            this.chk_alternateloginid = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.Location = new System.Drawing.Point(650, 1108);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(6);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(150, 48);
            this.cancelButton.TabIndex = 42;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.okButton.Location = new System.Drawing.Point(476, 1108);
            this.okButton.Margin = new System.Windows.Forms.Padding(6);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(150, 48);
            this.okButton.TabIndex = 41;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // Rul
            // 
            this.Rul.AutoSize = true;
            this.Rul.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Rul.Location = new System.Drawing.Point(18, 21);
            this.Rul.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.Rul.Name = "Rul";
            this.Rul.Size = new System.Drawing.Size(77, 36);
            this.Rul.TabIndex = 0;
            this.Rul.Text = "Rules";
            // 
            // radioButtonMT
            // 
            this.radioButtonMT.AutoSize = true;
            this.radioButtonMT.Checked = true;
            this.radioButtonMT.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonMT.Location = new System.Drawing.Point(172, 21);
            this.radioButtonMT.Margin = new System.Windows.Forms.Padding(6);
            this.radioButtonMT.Name = "radioButtonMT";
            this.radioButtonMT.Size = new System.Drawing.Size(192, 40);
            this.radioButtonMT.TabIndex = 1;
            this.radioButtonMT.TabStop = true;
            this.radioButtonMT.Text = "Multi-Tenant";
            this.radioButtonMT.UseVisualStyleBackColor = true;
            this.radioButtonMT.CheckedChanged += new System.EventHandler(this.radioButtonMT_CheckedChanged);
            // 
            // radioButtonD
            // 
            this.radioButtonD.AutoSize = true;
            this.radioButtonD.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonD.Location = new System.Drawing.Point(172, 73);
            this.radioButtonD.Margin = new System.Windows.Forms.Padding(6);
            this.radioButtonD.Name = "radioButtonD";
            this.radioButtonD.Size = new System.Drawing.Size(162, 40);
            this.radioButtonD.TabIndex = 2;
            this.radioButtonD.Text = "Dedicated";
            this.radioButtonD.UseVisualStyleBackColor = true;
            this.radioButtonD.CheckedChanged += new System.EventHandler(this.radioButtonD_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label2.Location = new System.Drawing.Point(18, 888);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(143, 36);
            this.label2.TabIndex = 0;
            this.label2.Text = "Credentials";
            // 
            // radioButtonAD
            // 
            this.radioButtonAD.AutoSize = true;
            this.radioButtonAD.Checked = true;
            this.radioButtonAD.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonAD.Location = new System.Drawing.Point(172, 31);
            this.radioButtonAD.Margin = new System.Windows.Forms.Padding(6);
            this.radioButtonAD.Name = "radioButtonAD";
            this.radioButtonAD.Size = new System.Drawing.Size(228, 40);
            this.radioButtonAD.TabIndex = 11;
            this.radioButtonAD.TabStop = true;
            this.radioButtonAD.Text = "Active Directory";
            this.radioButtonAD.UseVisualStyleBackColor = true;
            this.radioButtonAD.CheckedChanged += new System.EventHandler(this.radioButtonAD_CheckedChanged);
            // 
            // radioButtonLDAP
            // 
            this.radioButtonLDAP.AutoSize = true;
            this.radioButtonLDAP.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonLDAP.Location = new System.Drawing.Point(172, 319);
            this.radioButtonLDAP.Margin = new System.Windows.Forms.Padding(6);
            this.radioButtonLDAP.Name = "radioButtonLDAP";
            this.radioButtonLDAP.Size = new System.Drawing.Size(107, 40);
            this.radioButtonLDAP.TabIndex = 16;
            this.radioButtonLDAP.Text = "LDAP";
            this.radioButtonLDAP.UseVisualStyleBackColor = true;
            this.radioButtonLDAP.CheckedChanged += new System.EventHandler(this.radioButtonLDAP_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label6.Location = new System.Drawing.Point(22, 398);
            this.label6.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(120, 36);
            this.label6.TabIndex = 0;
            this.label6.Text = "Directory";
            // 
            // textBoxAD
            // 
            this.textBoxAD.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxAD.Location = new System.Drawing.Point(176, 621);
            this.textBoxAD.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.textBoxAD.Name = "textBoxAD";
            this.textBoxAD.Size = new System.Drawing.Size(446, 42);
            this.textBoxAD.TabIndex = 13;
            this.toolTip1.SetToolTip(this.textBoxAD, "Format: contoso.com");
            // 
            // forestButton
            // 
            this.forestButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.forestButton.Location = new System.Drawing.Point(650, 621);
            this.forestButton.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.forestButton.Name = "forestButton";
            this.forestButton.Size = new System.Drawing.Size(150, 48);
            this.forestButton.TabIndex = 14;
            this.forestButton.Text = "Add";
            this.forestButton.UseVisualStyleBackColor = true;
            this.forestButton.Click += new System.EventHandler(this.forestButton_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(644, 800);
            this.label9.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(104, 36);
            this.label9.TabIndex = 0;
            this.label9.Text = "Domain";
            // 
            // textBoxDomain
            // 
            this.textBoxDomain.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxDomain.Location = new System.Drawing.Point(176, 794);
            this.textBoxDomain.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxDomain.Name = "textBoxDomain";
            this.textBoxDomain.Size = new System.Drawing.Size(446, 42);
            this.textBoxDomain.TabIndex = 18;
            this.toolTip1.SetToolTip(this.textBoxDomain, "Format: DC=contoso,DC=com");
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(644, 746);
            this.label10.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(87, 36);
            this.label10.TabIndex = 0;
            this.label10.Text = "Server";
            // 
            // textBoxServer
            // 
            this.textBoxServer.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxServer.Location = new System.Drawing.Point(176, 737);
            this.textBoxServer.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxServer.Name = "textBoxServer";
            this.textBoxServer.Size = new System.Drawing.Size(446, 42);
            this.textBoxServer.TabIndex = 17;
            this.toolTip1.SetToolTip(this.textBoxServer, "FQDN or IP");
            // 
            // comboBoxPort
            // 
            this.comboBoxPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPort.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxPort.FormattingEnabled = true;
            this.comboBoxPort.Items.AddRange(new object[] {
            "3268",
            "389",
            "636"});
            this.comboBoxPort.Location = new System.Drawing.Point(176, 212);
            this.comboBoxPort.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.comboBoxPort.Name = "comboBoxPort";
            this.comboBoxPort.Size = new System.Drawing.Size(570, 44);
            this.comboBoxPort.TabIndex = 4;
            this.toolTip1.SetToolTip(this.comboBoxPort, "Use 3268 for AD");
            // 
            // textBoxFilter
            // 
            this.textBoxFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxFilter.Location = new System.Drawing.Point(176, 146);
            this.textBoxFilter.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxFilter.Name = "textBoxFilter";
            this.textBoxFilter.Size = new System.Drawing.Size(570, 42);
            this.textBoxFilter.TabIndex = 3;
            this.toolTip1.SetToolTip(this.textBoxFilter, "LDAP syntax");
            // 
            // textBoxSearchBase
            // 
            this.textBoxSearchBase.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxSearchBase.Location = new System.Drawing.Point(216, 279);
            this.textBoxSearchBase.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxSearchBase.Name = "textBoxSearchBase";
            this.textBoxSearchBase.Size = new System.Drawing.Size(530, 42);
            this.textBoxSearchBase.TabIndex = 51;
            this.toolTip1.SetToolTip(this.textBoxSearchBase, "LDAP syntax");
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPassword.Location = new System.Drawing.Point(176, 1033);
            this.textBoxPassword.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.PasswordChar = '*';
            this.textBoxPassword.Size = new System.Drawing.Size(446, 42);
            this.textBoxPassword.TabIndex = 34;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(644, 1038);
            this.label4.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(122, 36);
            this.label4.TabIndex = 0;
            this.label4.Text = "Password";
            // 
            // textBoxUser
            // 
            this.textBoxUser.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxUser.Location = new System.Drawing.Point(176, 975);
            this.textBoxUser.Margin = new System.Windows.Forms.Padding(6);
            this.textBoxUser.Name = "textBoxUser";
            this.textBoxUser.Size = new System.Drawing.Size(446, 42);
            this.textBoxUser.TabIndex = 33;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(648, 985);
            this.label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 36);
            this.label5.TabIndex = 0;
            this.label5.Text = "User";
            // 
            // radioButtonCurrent
            // 
            this.radioButtonCurrent.AutoSize = true;
            this.radioButtonCurrent.Checked = true;
            this.radioButtonCurrent.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonCurrent.Location = new System.Drawing.Point(172, 25);
            this.radioButtonCurrent.Margin = new System.Windows.Forms.Padding(6);
            this.radioButtonCurrent.Name = "radioButtonCurrent";
            this.radioButtonCurrent.Size = new System.Drawing.Size(133, 40);
            this.radioButtonCurrent.TabIndex = 31;
            this.radioButtonCurrent.TabStop = true;
            this.radioButtonCurrent.Text = "Current";
            this.radioButtonCurrent.UseVisualStyleBackColor = true;
            this.radioButtonCurrent.CheckedChanged += new System.EventHandler(this.radioButtonCurrent_CheckedChanged);
            // 
            // radioButtonOther
            // 
            this.radioButtonOther.AutoSize = true;
            this.radioButtonOther.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonOther.Location = new System.Drawing.Point(172, 69);
            this.radioButtonOther.Margin = new System.Windows.Forms.Padding(6);
            this.radioButtonOther.Name = "radioButtonOther";
            this.radioButtonOther.Size = new System.Drawing.Size(113, 40);
            this.radioButtonOther.TabIndex = 32;
            this.radioButtonOther.Text = "Other";
            this.radioButtonOther.UseVisualStyleBackColor = true;
            this.radioButtonOther.CheckedChanged += new System.EventHandler(this.radioButtonOther_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(22, 215);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(62, 36);
            this.label3.TabIndex = 0;
            this.label3.Text = "Port";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(18, 152);
            this.label8.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(72, 36);
            this.label8.TabIndex = 44;
            this.label8.Text = "Filter";
            // 
            // checkedListBoxAD
            // 
            this.checkedListBoxAD.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkedListBoxAD.FormattingEnabled = true;
            this.checkedListBoxAD.Location = new System.Drawing.Point(176, 446);
            this.checkedListBoxAD.Margin = new System.Windows.Forms.Padding(6);
            this.checkedListBoxAD.Name = "checkedListBoxAD";
            this.checkedListBoxAD.Size = new System.Drawing.Size(446, 121);
            this.checkedListBoxAD.TabIndex = 45;
            this.checkedListBoxAD.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxAD_ItemCheck);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButtonMT);
            this.groupBox1.Controls.Add(this.radioButtonD);
            this.groupBox1.Location = new System.Drawing.Point(4, 0);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox1.Size = new System.Drawing.Size(824, 127);
            this.groupBox1.TabIndex = 46;
            this.groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButtonAD);
            this.groupBox2.Controls.Add(this.radioButtonLDAP);
            this.groupBox2.Location = new System.Drawing.Point(4, 363);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox2.Size = new System.Drawing.Size(824, 504);
            this.groupBox2.TabIndex = 47;
            this.groupBox2.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radioButtonCurrent);
            this.groupBox3.Controls.Add(this.radioButtonOther);
            this.groupBox3.Location = new System.Drawing.Point(4, 863);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox3.Size = new System.Drawing.Size(824, 233);
            this.groupBox3.TabIndex = 48;
            this.groupBox3.TabStop = false;
            // 
            // searchBaseLabel
            // 
            this.searchBaseLabel.AutoSize = true;
            this.searchBaseLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.searchBaseLabel.Location = new System.Drawing.Point(18, 283);
            this.searchBaseLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.searchBaseLabel.Name = "searchBaseLabel";
            this.searchBaseLabel.Size = new System.Drawing.Size(152, 36);
            this.searchBaseLabel.TabIndex = 49;
            this.searchBaseLabel.Text = "Search Base";
            // 
            // searchBaseCheckBox
            // 
            this.searchBaseCheckBox.AutoSize = true;
            this.searchBaseCheckBox.Location = new System.Drawing.Point(176, 287);
            this.searchBaseCheckBox.Margin = new System.Windows.Forms.Padding(6);
            this.searchBaseCheckBox.Name = "searchBaseCheckBox";
            this.searchBaseCheckBox.Size = new System.Drawing.Size(28, 27);
            this.searchBaseCheckBox.TabIndex = 50;
            this.searchBaseCheckBox.UseVisualStyleBackColor = true;
            this.searchBaseCheckBox.CheckedChanged += new System.EventHandler(this.searchBaseCheckBox_CheckedChanged);
            // 
            // chk_alternateloginid
            // 
            this.chk_alternateloginid.AutoSize = true;
            this.chk_alternateloginid.Location = new System.Drawing.Point(176, 330);
            this.chk_alternateloginid.Name = "chk_alternateloginid";
            this.chk_alternateloginid.Size = new System.Drawing.Size(499, 29);
            this.chk_alternateloginid.TabIndex = 52;
            this.chk_alternateloginid.Text = "Consider Alternate Login ID (Ignore UPN errors)";
            this.chk_alternateloginid.UseVisualStyleBackColor = true;
            this.chk_alternateloginid.CheckedChanged += new System.EventHandler(this.chk_alternateloginid_CheckedChanged);
            // 
            // FormSettings
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(832, 1181);
            this.Controls.Add(this.chk_alternateloginid);
            this.Controls.Add(this.textBoxSearchBase);
            this.Controls.Add(this.searchBaseCheckBox);
            this.Controls.Add(this.searchBaseLabel);
            this.Controls.Add(this.checkedListBoxAD);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textBoxFilter);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxDomain);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxServer);
            this.Controls.Add(this.textBoxAD);
            this.Controls.Add(this.textBoxUser);
            this.Controls.Add(this.forestButton);
            this.Controls.Add(this.comboBoxPort);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.Rul);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox3);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "FormSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label Rul;
        private System.Windows.Forms.RadioButton radioButtonMT;
        private System.Windows.Forms.RadioButton radioButtonD;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton radioButtonAD;
        private System.Windows.Forms.RadioButton radioButtonLDAP;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxAD;
        private System.Windows.Forms.Button forestButton;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBoxDomain;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxServer;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox comboBoxPort;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxUser;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton radioButtonCurrent;
        private System.Windows.Forms.RadioButton radioButtonOther;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBoxFilter;
        private System.Windows.Forms.CheckedListBox checkedListBoxAD;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label searchBaseLabel;
        private System.Windows.Forms.CheckBox searchBaseCheckBox;
        private System.Windows.Forms.TextBox textBoxSearchBase;
        private System.Windows.Forms.CheckBox chk_alternateloginid;
    }
}