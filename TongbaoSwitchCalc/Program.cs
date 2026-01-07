using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TongbaoSwitchCalc
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            BindEvent();
            Application.Run(new MainForm());
        }

        private static void BindEvent()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        private static string GetAssemblyName(string assemblyInfo)
        {
            if (string.IsNullOrEmpty(assemblyInfo))
            {
                return assemblyInfo;
            }

            int index = assemblyInfo.IndexOf(',');

            if (index == -1)
            {
                return assemblyInfo;
            }

            string assemblyName = assemblyInfo.Substring(0, index);

            return assemblyName;
        }

        private static string GetAssemblyPath(string assemblyName)
        {
            string path = null;
            string pluginDirectoryName = Application.StartupPath + "/Lib";
            if (!Directory.Exists(pluginDirectoryName))
            {
                pluginDirectoryName = Application.StartupPath + "/Lib";
                if (!Directory.Exists(pluginDirectoryName))
                {
                    return null;
                }
            }

            DirectoryInfo pluginDi = new DirectoryInfo(pluginDirectoryName);

            UpdateAssemblyPath(assemblyName, pluginDi, ref path);

            return path;
        }

        private static void UpdateAssemblyPath(string assemblyName, DirectoryInfo di, ref string path)
        {
            string fileShortName = $"{assemblyName}.dll";

            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Name == fileShortName)
                {
                    path = fi.FullName;
                    return;
                }
            }

            foreach (DirectoryInfo subDir in di.GetDirectories())
            {
                if ((subDir.Attributes & FileAttributes.Hidden) != 0)
                {
                    continue;
                }

                UpdateAssemblyPath(assemblyName, subDir, ref path);

                if (!string.IsNullOrEmpty(path))
                {
                    return;
                }
            }
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs e)
        {
            string assemblyName = GetAssemblyName(e.Name);

            if (string.IsNullOrEmpty(assemblyName))
            {
                return null;
            }

            string path = GetAssemblyPath(assemblyName);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            Assembly assembly = Assembly.LoadFrom(path);

            return assembly;
        }
    }
}