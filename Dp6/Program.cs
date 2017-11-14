using System;
using System.ServiceProcess;
using System.Text;
using System.Threading;
// ReSharper disable LocalizableElement

namespace Dp6
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("Application: Program Startup");

            if ((args.Length == 1) && (args[0].ToLower() == "/?"))
            {
                var sb = new StringBuilder();
                sb.Append("USAGE:\r\n");
                sb.Append("To install windows service:\t\t\t DP6 /Install \t\r\n");
                sb.Append("To un-install windows service:\t\t DP6 /Uninstall \t\r\n");
                sb.Append("To execute immediately:\t DP6 /Now \t\r\n");
                sb.Append("\r\n");

                System.Windows.Forms.MessageBox.Show(sb.ToString(), "DP6 Service",
                     System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

                return;
            }

            if ((args.Length == 1) && ((args[0].ToLower() == "/install") || (args[0].ToLower() == "/uninstall")))
            {
                // (Un)Install this Windows Service using InstallUtil.exe
                AppDomain dom = AppDomain.CreateDomain("execDom");
                string szFrameworkPath = typeof(object).Assembly.Location;
                var sb = new StringBuilder(szFrameworkPath.Substring(0, szFrameworkPath.LastIndexOf('\\')));
                sb.Append("\\InstallUtil.exe");

                // Get Path to current assembly
                string szMyPath = typeof(Program).Assembly.Location;

                if (args[0].ToLower() == "/install")
                {
                    Console.WriteLine("Service: Installing Service");
                    dom.ExecuteAssembly(sb.ToString(), new[] { szMyPath });
                }
                else if (args[0].ToLower() == "/uninstall")
                {
                    Console.WriteLine("Service: Un-Installing Service");
                    dom.ExecuteAssembly(sb.ToString(), new[] { "/u", szMyPath });
                }

                return;
            }

            // start service(s)
            var dp6 = new DpService();
            if ((args.Length > 0) && (args[0].ToLower() == "/now"))
            {
                dp6.Startup(args);

                while (true)
                    Thread.Sleep(1000);
            }
            else
            {
                // Setup services
                ServiceBase.Run(dp6);
            }
        }
    }
}
