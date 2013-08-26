using Microsoft.Shell;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using TurbofilmVpn.ViewModels;

namespace TurbofilmVpn
{
    using ContextMenu = System.Windows.Forms.ContextMenu;
    using Icon = System.Drawing.Icon;
    using MenuItem = System.Windows.Forms.MenuItem;
    using MouseButtons = System.Windows.Forms.MouseButtons;
    using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
    using NotifyIcon = System.Windows.Forms.NotifyIcon;
    using ToolTipIcon = System.Windows.Forms.ToolTipIcon;

    /// <summary>Interaction logic for App.xaml</summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private NotifyIcon _notifyIcon;
        private bool _shutdown = false;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown; // закрывать только после вызова Shutdown()

            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location)
            };

            _notifyIcon.Click += (s, o) =>
            {
                if (!_shutdown && ((MouseEventArgs)o).Button == MouseButtons.Left)
                    Show();
            };

            MainWindow = new Views.MainWindow();
            MainWindow.Closing += (s, o) =>
            {
                if (!_shutdown)
                {
                    Hide();
                    o.Cancel = true;
                }
            };

            // определить контекстное меню
            _notifyIcon.ContextMenu = new ContextMenu(new[]
            {
                new MenuItem("Exit", (o, args) =>
                {
                    if (!_shutdown)
                    {
                        _shutdown = true;
                        Shutdown();
                    }
                })
            });

            _notifyIcon.ShowBalloonTip(1000, null, MainWindow.Title, ToolTipIcon.None);
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (!_shutdown)
                Show();
            return true;
        }

        private void Show()
        {
            MainWindow.Show();
            MainWindow.Activate();
            _notifyIcon.Visible = false;
        }

        private void Hide()
        {
            MainWindow.Hide();
            _notifyIcon.Visible = true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ViewModelLocator.Cleanup();
            _notifyIcon.Visible = false;
            _notifyIcon.Icon = null;
            _notifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}
