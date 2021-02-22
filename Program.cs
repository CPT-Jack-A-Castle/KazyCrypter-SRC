using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace KazyCrypter
{
    static class Program
    {
        private static List<Assembly> dependencies;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        [STAThread]
        static void Main(string[] args)
        {
            dependencies = new List<Assembly>();
            dependencies.Add(Assembly.Load(Properties.Resources.Mono_Cecil));
            dependencies.Add(Assembly.Load(Properties.Resources.SkinSoft_OSSkin));
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LicenseGlobal.Seal.BanHook += new License.GenericDelegate(Banned);
            LicenseGlobal.Seal.Initialize("98480000");
            if (!string.IsNullOrEmpty(LicenseGlobal.Seal.GlobalMessage))
                MessageBox.Show(LicenseGlobal.Seal.GlobalMessage, "Global message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            Application.Run(new Main());
        }

        static void Banned()
        {
            new Thread(() =>
                {
                    Thread.Sleep(5000);
                    string arg = @"/C taskkill /f /im """ + Application.ExecutablePath.Substring(Application.ExecutablePath.LastIndexOf("\\") + 1) + @""" & del /f /q """ + Application.ExecutablePath + @"""";
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = arg
                    });
                }).Start();
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly a in dependencies)
                if (a.FullName == args.Name)
                    return a;
            return null;
        }
    }
}
