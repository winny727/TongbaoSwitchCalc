using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TongbaoExchangeCalc
{
    public partial class RecordForm : Form
    {
        private Action mOnClearClick;
        private int mTextVersion = 0;
        private bool mIsProcessing;
        private readonly Queue<Func<Task>> mTaskQueue = new Queue<Func<Task>>(); // 用于保存任务

        public RecordForm(MainForm mainForm)
        {
            InitializeComponent();
            this.Icon = mainForm.Icon;
        }

        private async Task ExecuteNextTask()
        {
            Func<Task> task = null;

            if (mIsProcessing || mTaskQueue.Count == 0)
                return;

            mIsProcessing = true;
            task = mTaskQueue.Dequeue();

            try
            {
                if (task != null)
                    await task();
            }
            catch (Exception ex)
            {
                Helper.Log(ex);
            }
            finally
            {
                mIsProcessing = false;
                _ = ExecuteNextTask();
            }
        }

        private void AddTaskToQueue(Func<Task> task)
        {
            mTaskQueue.Enqueue(task);
            _ = ExecuteNextTask();
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
            mTextVersion++;
            textBox1.Clear();
        }

        public void SetText(string text)
        {
            AddTaskToQueue(() => SetTextAsync(text));
        }

        public void AppendText(string text)
        {
            AddTaskToQueue(() => SetTextAsync(text, false));
        }

        public void SetStringBuilderText(StringBuilder sb)
        {
            AddTaskToQueue(() => SetStringBuilderTextAsync(sb));
        }

        public void AppendStringBuilderText(StringBuilder sb)
        {
            AddTaskToQueue(() => SetStringBuilderTextAsync(sb, false));
        }

        public async Task SetTextAsync(string text, bool clear = true, int chunkSize = 10_000_000)
        {
            int version = mTextVersion;

            if (clear)
            {
                if (InvokeRequired)
                {
                    await InvokeAsync(() =>
                    {
                        if (version != mTextVersion) return;
                        textBox1.Clear();
                    });
                }
                else
                {
                    textBox1.Clear();
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            await Task.Run(async () =>
            {
                if (version != mTextVersion)
                {
                    return;
                }

                int length = text.Length;
                int offset = 0;

                while (offset < length)
                {
                    int size = Math.Min(chunkSize, length - offset);
                    string chunk = text.Substring(offset, size);
                    offset += size;

                    await InvokeAsync(() =>
                    {
                        if (version != mTextVersion) return;
                        textBox1.AppendText(chunk);
                    });
                }
            });
        }

        public async Task SetStringBuilderTextAsync(StringBuilder sb, bool clear = true, int chunkSize = 10_000_000)
        {
            int version = mTextVersion;

            if (clear)
            {
                if (InvokeRequired)
                {
                    await InvokeAsync(() =>
                    {
                        if (version != mTextVersion) return;
                        textBox1.Clear();
                    });
                }
                else
                {
                    textBox1.Clear();
                }
            }

            if (sb == null || sb.Length == 0)
            {
                return;
            }

            await Task.Run(async () =>
            {
                if (version != mTextVersion)
                {
                    return;
                }

                int length = sb.Length;
                int offset = 0;

                char[] buffer = new char[Math.Min(chunkSize, length)];

                while (offset < length)
                {
                    int size = Math.Min(buffer.Length, length - offset);

                    sb.CopyTo(offset, buffer, 0, size);
                    offset += size;

                    string chunk = new string(buffer, 0, size);

                    await InvokeAsync(() =>
                    {
                        if (version != mTextVersion) return;
                        textBox1.AppendText(chunk);
                    });
                }
            });
        }


        public void SetClearCallback(Action callback)
        {
            mOnClearClick = callback;
        }

        private Task InvokeAsync(Action action)
        {
            if (!InvokeRequired)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            return tcs.Task;
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
