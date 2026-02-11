using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TongbaoExchangeCalc.Undo;
using TongbaoExchangeCalc.Undo.Commands;

namespace TongbaoExchangeCalc
{
    public static class UndoHelper
    {
        private static readonly Dictionary<Control, EventHandler> mHandlers = new Dictionary<Control, EventHandler>();

        private class ControlTempData<T>
        {
            public Control Control;
            public Func<T> Getter;
            public Action<T> Setter;
            public T LastValue;
            public bool mIsUpdating;

            public ControlTempData(Control control, Func<T> getter, Action<T> setter)
            {
                Control = control ?? throw new ArgumentNullException(nameof(control));
                Getter = getter ?? throw new ArgumentNullException(nameof(getter));
                Setter = setter ?? throw new ArgumentNullException(nameof(setter));
                LastValue = getter();
                mIsUpdating = false;
            }

            public void BeginUpdate()
            {
                mIsUpdating = true;
            }

            public void EndUpdate()
            {
                UpdateLastValue(); // Undo/Redo的时候也要更新LastValue
                mIsUpdating = false;
            }

            public bool IsUpdating()
            {
                return mIsUpdating;
            }

            public void UpdateLastValue()
            {
                LastValue = Getter();
            }
        }

        private static void SetupControlUndo<TControl, TValue>(
            TControl control,
            Func<TValue> getValue,
            Action<TValue> setValue,
            Action<TControl, EventHandler> addHandler,
            Action<TControl, EventHandler> removeHandler)
            where TControl : Control
        {
            if (control == null)
            {
                return;
            }

            if (mHandlers.TryGetValue(control, out var oldHandler))
            {
                removeHandler(control, oldHandler);
            }

            var tempData = new ControlTempData<TValue>(control, getValue, setValue);

            void EventHandler(object s, EventArgs e)
            {
                OnControlSetValue(tempData);
            }

            addHandler(control, EventHandler);
            mHandlers[control] = EventHandler;
        }


        private static void OnControlSetValue<T>(ControlTempData<T> tempData)
        {
            if (tempData == null)
            {
                return;
            }

            if (tempData.IsUpdating())
            {
                return;
            }

            var newValue = tempData.Getter();
            if (EqualityComparer<T>.Default.Equals(tempData.LastValue, newValue))
            {
                return;
            }

            var command = new OnControlSetValueCommand<T>(
                value =>
                {
                    tempData.BeginUpdate();
                    tempData.Setter(value);
                    tempData.EndUpdate();
                    SwitchToTabPage(tempData.Control);
                },
                tempData.LastValue,
                tempData.Getter(),
                tempData.Control.Name // debugInfo
            );

            UndoCommandMgr.Instance.ExecuteCommand(command);
            tempData.UpdateLastValue();
        }

        private static void SwitchToTabPage(Control control)
        {
            while (control != null)
            {
                if (control is TabPage tabPage)
                {
                    if (tabPage?.Parent is TabControl tabControl && tabControl.SelectedTab != tabPage)
                    {
                        tabControl.SelectedTab = tabPage;
                    }
                    return;
                }
                control = control.Parent;
            }
        }

        public static void SetupNumericUndo(NumericUpDown numeric)
        {
            SetupControlUndo(
                numeric,
                () => numeric.Value,
                v => numeric.Value = v,
                (c, h) => c.ValueChanged += h,
                (c, h) => c.ValueChanged -= h
            );
        }

        public static void SetupComboBoxUndo(ComboBox comboBox)
        {
            SetupControlUndo(
                comboBox,
                () => comboBox.SelectedIndex,
                v => comboBox.SelectedIndex = v,
                (c, h) => c.SelectedIndexChanged += h,
                (c, h) => c.SelectedIndexChanged -= h
            );
        }

        public static void SetupCheckBoxUndo(CheckBox checkBox)
        {
            SetupControlUndo(
                checkBox,
                () => checkBox.Checked,
                v => checkBox.Checked = v,
                (c, h) => c.CheckedChanged += h,
                (c, h) => c.CheckedChanged -= h
            );
        }
    }
}
