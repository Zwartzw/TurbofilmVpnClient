using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Windows.Threading;
using Microsoft.Shell;
using TurbofilmVpn.Utils;
using System.Windows;

namespace TurbofilmVpn
{
    public static class Loader
    {
        private const string AppInstanceKey = "Turbofilm_5c143788-0aaa-4fc5-a833-d10ca2f41a96";

        [STAThread]
        public static void Main()
        {
#if !DEBUG
                // exception handling
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
#endif
            // https://gist.github.com/thoemmi/3724333
            // http://www.digitallycreated.net/Blog/61/combining-multiple-assemblies-into-a-single-exe-for-a-wpf-application
            // http://www.aboutmycode.com/net-framework/assemblyresolve-event-tips/

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            
            if (SingleInstance<App>.InitializeAsFirstInstance(AppInstanceKey))
            {
                var app = new App();
                app.InitializeComponent();
#if !DEBUG
                // exception handling
                Application.Current.DispatcherUnhandledException += OnCurrentOnDispatcherUnhandledException;
#endif

                app.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        private static readonly object Lock = new object();

        private static Assembly OnAssemblyResolve(object s, ResolveEventArgs args)
        {
            lock (Lock)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var embeddedResources = new List<string>(assembly.GetManifestResourceNames());
                var assemblyName = new AssemblyName(args.Name);
                var fileName = string.Format("{0}.dll", assemblyName.Name);
                if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
                    fileName = string.Format(@"{0}\{1}", assemblyName.CultureInfo, fileName);

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var loaded = assemblies.FirstOrDefault(a => a.ManifestModule.ScopeName == fileName);
                if (loaded != null)
                    return loaded;

                var resourceName = embeddedResources.FirstOrDefault(ern => ern.EndsWith(fileName));
                if (!string.IsNullOrWhiteSpace(resourceName))
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                            return null;

                        var assemblyData = new byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        var load = Assembly.Load(assemblyData);
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Loaded \"{0}\" from resources", (object)fileName);
#endif
                        return load;
                    }
                }
                return null;
            }
        }

        /// <summary>Non ui-thread exception handler.</summary>
        /// <remarks>This one will not catch exceptions for async code. It has different exception handling algorithm.</remarks>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Exception info.</param>
        [SecurityPermission(SecurityAction.Demand, ControlAppDomain = true)]
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var fileName = string.Format(@"turbofilmvpn_crash_{0}_{1}.mdmp", DateTime.Today.ToShortDateString(), DateTime.Now.Ticks);
            if (e.ExceptionObject != null)
            {
                string stackTrace = ((Exception)e.ExceptionObject).StackTrace;
            }
            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
            {
                MiniDump.Write(stream.SafeFileHandle, MiniDump.Option.WithFullMemory, e.ExceptionObject != null ? MiniDump.ExceptionInfo.Present : MiniDump.ExceptionInfo.None);
            }
        }

        /// <summary>Ui-thread exception handler.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Exception info.</param>
        [SecurityPermission(SecurityAction.Demand, ControlAppDomain = true)]
        private static void OnCurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var fileName = string.Format(@"turbofilmvpn_uicrash_{0}_{1}.mdmp", DateTime.Today.ToShortDateString(), DateTime.Now.Ticks);
            if (e.Exception != null)
            {
                string stackTrace = e.Exception.StackTrace;
            }
            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
            {
                MiniDump.Write(stream.SafeFileHandle, MiniDump.Option.WithFullMemory, e.Exception != null ? MiniDump.ExceptionInfo.Present : MiniDump.ExceptionInfo.None);
            }

            e.Handled = true;
        }
    }
}
