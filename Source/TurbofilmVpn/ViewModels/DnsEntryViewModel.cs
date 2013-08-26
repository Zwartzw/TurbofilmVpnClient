using System.Collections.ObjectModel;
using System.Net;
using GalaSoft.MvvmLight;

namespace TurbofilmVpn.ViewModels
{
    public class DnsEntryViewModel : ViewModelBase
    {
        private string _hostname = string.Empty;
        private ObservableCollection<IPAddress> _ipAddresses = new ObservableCollection<IPAddress>();

        /// <summary>The <see cref="Hostname" /> property's name.</summary>
        public const string HostnamePropertyName = "Hostname";

        /// <summary>The <see cref="IPAddresses" /> property's name.</summary>
        public const string IPAddressesPropertyName = "IPAddresses";

        /// <summary>
        /// Gets and sets the Hostname property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                if (_hostname == value)
                    return;

                RaisePropertyChanging(HostnamePropertyName);
                _hostname = value;
                RaisePropertyChanged(HostnamePropertyName);
            }
        }

        /// <summary>
        /// Gets the IPAdresses property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<IPAddress> IPAddresses
        {
            get { return _ipAddresses; }
        }

        public override string ToString()
        {
            return Hostname;
        }
    }
}