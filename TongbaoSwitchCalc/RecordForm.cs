using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
                textBox1.Text = value ?? string.Empty;
                if (textBox1.Text.Length > 0)
                {
                    textBox1.SelectionLength = textBox1.Text.Length - 1;
                    textBox1.ScrollToCaret();
                }
            }
        }

        private Action mOnClearClick;

        public RecordForm()
        {
            InitializeComponent();
        }

        public void SetClearCallback(Action callback)
        {
            mOnClearClick = callback;
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

        private void btnClear_Click(object sender, EventArgs e)
        {
            mOnClearClick?.Invoke();
        }
    }
}
