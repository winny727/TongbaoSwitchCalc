using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TongbaoExchangeCalc
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
                SetText(value);
            }
        }

        private Action mOnClearClick;

        public RecordForm(MainForm mainForm)
        {
            InitializeComponent();
            this.Icon = mainForm.Icon;
        }

        public void UpdateScroll()
        {
            if (textBox1.Text.Length > 0)
            {
                textBox1.SelectionLength = textBox1.Text.Length - 1;
                textBox1.ScrollToCaret();
            }
        }

        public void ClearText()
        {
            textBox1.Clear();
        }

        public void SetText(string text)
        {
            textBox1.Text = text ?? string.Empty;
            //textBox1.Clear();
            //FastAppendText(textBox1, text);
            UpdateScroll();
        }

        public void AppendText(string text)
        {
            textBox1.AppendText(text ?? string.Empty);
            UpdateScroll();
        }

        public void SetClearCallback(Action callback)
        {
            mOnClearClick = callback;
        }

        private unsafe void FastAppendText(TextBox textBox, string text, int chunkSize = 5000)
        {
            // 禁用UI更新
            textBox.Visible = false;
            textBox.SuspendLayout();

            try
            {
                // 使用指针操作提升性能
                unsafe
                {
                    fixed (char* pText = text)
                    {
                        int totalLength = text.Length;
                        int processed = 0;

                        while (processed < totalLength)
                        {
                            int currentChunkSize = Math.Min(chunkSize, totalLength - processed);

                            // 直接构建字符串片段
                            string chunk = new string(pText, processed, currentChunkSize);
                            textBox.AppendText(chunk);

                            processed += currentChunkSize;

                            // 每处理5个区块更新一次UI
                            if ((processed / chunkSize) % 5 == 0)
                            {
                                Application.DoEvents();
                            }
                        }
                    }
                }
            }
            finally
            {
                textBox.ResumeLayout();
                textBox.Visible = true;
            }
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
