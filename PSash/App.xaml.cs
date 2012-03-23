using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PSash
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <remarks>
        /// Implementation of a single application instance, see:
        /// http://sanity-free.org/143/csharp_dotnet_single_instance_application.html
        /// </remarks>
        static Mutex mutex = new Mutex(true, AssemblyInfo.Guid.ToString());
        protected override void OnStartup(StartupEventArgs e)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                base.OnStartup(e); 
            }
            else
            {
                NativeMethods.PostMessage(
                    (IntPtr)NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SHOWME,
                    IntPtr.Zero,
                    IntPtr.Zero);
                Environment.Exit(0);
            }
        }

        internal class NativeMethods
        {
            public const int HWND_BROADCAST = 0xffff;
            public static readonly int WM_SHOWME = RegisterWindowMessage("WM_SHOWME");
            [DllImport("user32")]
            public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
            [DllImport("user32")]
            public static extern int RegisterWindowMessage(string message);
        }
    }
}
