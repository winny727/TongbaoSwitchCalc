using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ArknightsRoguelikeRec
{
    public partial class RecordForm : Form
    {
        //public string Title
        //{
        //    get
        //    {
        //        return this.Text;
        //    }
        //    set
        //    {
        //        this.Text = value;
        //    }
        //}

        public string Content
        {
            get
            {
                return textBox1.Text;
            }
            set
            {
                textBox1.Text = value;
            }
        }

        public RecordForm()
        {
            InitializeComponent();
        }

        private void RecordForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
