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
    public partial class FormFeedback : Form
    {
        private Form1 myParent;

        public FormFeedback(Form1 frm1)
        {
            InitializeComponent();
            myParent = frm1;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                linkLabel1.LinkVisited = true;
                System.Diagnostics.Process.Start("https://github.com/microsoft/idfix");
                this.Close();
            }
            catch (Exception ex)
            {
                myParent.statusDisplay(StringLiterals.Exception + "No web client - " + ex.Message);
            }
        }

    private void label1_Click(object sender, EventArgs e)
    {

    }
  }
}
