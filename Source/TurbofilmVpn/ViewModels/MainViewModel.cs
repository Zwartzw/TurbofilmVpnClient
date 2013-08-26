using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using DotRas;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.Net;
using TurbofilmVpn.Utils;

namespace TurbofilmVpn.ViewModels
{
    // ReSharper disable ReturnTypeCanBeEnumerable.Global
    public class MainViewModel : ViewModelBase
    {
        private const string Unique = "vpn.vemeo.com_30ca10d8-4e74-410f-a3cf-162c6f189713";

        private readonly RasDialer _dialer = new RasDialer();
        private readonly ObservableCollection<DnsEntryViewModel> _hosts = new ObservableCollection<DnsEntryViewModel>();

        private const string HostsPath = "hosts.txt";
        private const string PhoneBookPath = "vemeo.pbk";
        private const string VpnName = "Vemeo";
        private const string VpnServer = "vpn.vemeo.com";

        private CredentialDialog _dialog;
        private NetworkCredential _credentials;
        private RasHandle _handle;
        private bool _isConnected = false;

        /// <summary>The <see cref="Hosts" /> property's name.</summary>
        public const string HostsPropertyName = "Hosts";
        /// <summary>The <see cref="IsConnected" /> property's name.</summary>
        public const string IsConnectedPropertyName = "IsConnected";
        /// <summary>The <see cref="ConnectCommand" /> property's name.</summary>
        public const string ConnectCommandPropertyName = "ConnectCommand";
        /// <summary>The <see cref="DisconnectCommand" /> property's name.</summary>
        public const string DisconnectCommandPropertyName = "DisconnectCommand";

        /// <summary>
        /// Gets the Hosts property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<DnsEntryViewModel> Hosts
        {
            get { return _hosts; }
        }

        /// <summary>
        /// Gets and sets the IsConnected property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected == value)
                    return;

                RaisePropertyChanging(IsConnectedPropertyName);
                _isConnected = value;
                RaisePropertyChanged(IsConnectedPropertyName);
                RaisePropertyChanged(ConnectCommandPropertyName);
                RaisePropertyChanged(DisconnectCommandPropertyName);
            }
        }

        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand DisconnectCommand { get; private set; }

        /// <summary>Initializes a new instance of the MainViewModel class.</summary>
        public MainViewModel()
        {
            ReloadHosts();
            ConnectCommand = new RelayCommand(ConnectAction, () => !IsConnected && !_dialer.IsBusy);
            DisconnectCommand = new RelayCommand(DisconnectAction, () => IsConnected || _dialer.IsBusy);
        }

        private void ConnectAction()
        {
            if (IsConnected || _dialer.IsBusy)
                return;

            _dialer.DialCompleted += DialCompleted;
            _dialer.StateChanged += StateChanged;
            _dialer.Error += DialError;

#if !DEBUG
            _credentials = CredentialDialog.RetrieveCredential(Unique);
#endif
            if (_credentials == null)
            {
                _dialog = new CredentialDialog();
                _dialog.WindowTitle = "Vemeo VPN Authentication"; // XP Only
                _dialog.MainInstruction = "Please enter your vemeo.com username and password.";
                _dialog.ShowSaveCheckBox = true;
                _dialog.IsSaveChecked = true;
#if DEBUG
                _dialog.ShowUIForSavedCredentials = true;
#else
                _dialog.ShowUIForSavedCredentials = false;
#endif
                _dialog.Target = Unique;
                if (_dialog.ShowDialog())
                    _credentials = _dialog.Credentials;
            }

            if (_credentials != null)
                Connect();
        }

        private void DisconnectAction()
        {
            if (!IsConnected && !_dialer.IsBusy)
                return;

            Disconnect();
            // todo: remove routes
        }

        private void ReloadHosts()
        {
            // так делать не хорошо:
            var hosts = File.ReadAllLines(HostsPath, Encoding.UTF8).Select(str => str.Trim()).Where(str => str.Length > 0);
            foreach (var host in hosts)
            {
                try
                {
                    var hostEntry = Dns.GetHostEntry(host);
                    var ips = hostEntry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                    if (ips.Any())
                    {
                        var dnsvm = new DnsEntryViewModel { Hostname = host };
                        foreach (var ipAddress in ips)
                            dnsvm.IPAddresses.Add(ipAddress);
                        Hosts.Add(dnsvm);
                    }
                }
                catch (SocketException)
                {
                }
            }
        }

        private void DialCompleted(object sender, DialCompletedEventArgs e)
        {
            if (_dialog != null && !_dialog.IsStoredCredential)
                _dialog.ConfirmCredentials(e.Connected);
            else if (!e.Connected)
                CredentialDialog.DeleteCredential(Unique);

            IsConnected = e.Connected;

            if (IsConnected)
            {
                var ifn = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i=>i.Name == VpnName);
                if (ifn != null)
                {
                    var ifprop = ifn.GetIPProperties();
                    var ifip = ifprop.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address));
                    var prop = ifprop.GetIPv4Properties();
                    
                    var ifindex = ifprop.GetIPv4Properties().Index;

                    // add routes
                    var ips = Hosts.SelectMany(hst => hst.IPAddresses);

                    foreach (var ipAddress in ips)
                    {
                        string message = string.Empty;

                        NativeMethods.ModifyIpForwardEntry(true, ipAddress.ToString(), "255.255.255.255", ifip.Address.ToString(), (uint)ifindex, /* todo: proper metric */(uint)ifindex, out message);
                        Debug.WriteLine(message);
                    }
                }
            }
        }

        private void StateChanged(object sender, StateChangedEventArgs e)
        {
            RaisePropertyChanged(ConnectCommandPropertyName);
            RaisePropertyChanged(DisconnectCommandPropertyName);
        }

        private void DialError(object sender, ErrorEventArgs e)
        {
            IsConnected = false;
        }

        private void Connect()
        {
            var rasPhoneBook = new RasPhoneBook();
            rasPhoneBook.Open(PhoneBookPath);
            if (!rasPhoneBook.Entries.Contains(VpnName))
            {
                var vpnDevice = RasDevice.GetDevices().FirstOrDefault(d => d.DeviceType == RasDeviceType.Vpn && d.Name.ToLower(CultureInfo.CurrentCulture).Contains("(PPTP)".ToLower(CultureInfo.CurrentCulture)));
                var rasEntry = RasEntry.CreateVpnEntry(VpnName, VpnServer, RasVpnStrategy.PptpOnly, vpnDevice, false);
                rasEntry.Options.ShowDialingProgress = false;
                rasPhoneBook.Entries.Add(rasEntry);
            }
            _dialer.EntryName = VpnName;
            _dialer.PhoneBookPath = PhoneBookPath;
            _dialer.Credentials = _credentials;
            _handle = _dialer.DialAsync();
        }

        private void Disconnect()
        {
            if (_dialer.IsBusy)
            {
                _dialer.DialAsyncCancel();
            }
            else if (_handle != null)
            {
                var activeConnectionByHandle = RasConnection.GetActiveConnections().FirstOrDefault(c => c.Handle == _handle);
                if (activeConnectionByHandle != null)
                    activeConnectionByHandle.HangUp();
                _handle = null;
                IsConnected = false;
            }
        }

        ///// <summary> 
        ///// This utility function displays all the IP (v4, not v6) addresses of the local computer. 
        ///// </summary> 
        //public static void DisplayIPAddresses()
        //{
        //    var sb = new StringBuilder();

        //    // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection) 
        //    var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        //    foreach (NetworkInterface network in networkInterfaces)
        //    {
        //        // Read the IP configuration for each network 
        //        IPInterfaceProperties properties = network.GetIPProperties();

        //        // Each network interface may have multiple IP addresses 
        //        foreach (IPAddressInformation address in properties.UnicastAddresses)
        //        {
        //            // We're only interested in IPv4 addresses for now 
        //            if (address.Address.AddressFamily != AddressFamily.InterNetwork)
        //                continue;

        //            // Ignore loopback addresses (e.g., 127.0.0.1) 
        //            if (IPAddress.IsLoopback(address.Address))
        //                continue;

        //            sb.AppendLine(address.Address.ToString() + " (" + network.Name + ")");
        //        }
        //    }

        //    MessageBox.Show(sb.ToString());
        //}

        public override void Cleanup()
        {
            Disconnect();

            _dialer.DialCompleted -= DialCompleted;
            _dialer.StateChanged -= StateChanged;
            _dialer.Error -= DialError;
            _dialer.Dispose();

            if (_dialog != null)
                _dialog.Dispose();

            // todo: remove routes 
            base.Cleanup();
        }
    }
    // ReSharper enable ReturnTypeCanBeEnumerable.Global
}