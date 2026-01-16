using System;
using System.Collections.Generic;
using TongbaoSwitchCalc.DataModel.Simulation;

namespace TongbaoSwitchCalc.Impl.View
{
    public class Logger : ILogger
    {
        private Action<string> mLogCallback;

        public void SetLogFunc(Action<string> logCallback)
        {
            mLogCallback = logCallback;
        }

        public void Log(string msg)
        {
            Helper.Log(msg);
            mLogCallback?.Invoke(msg);
        }
    }
}
