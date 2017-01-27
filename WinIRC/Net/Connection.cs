using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace WinIRC.Net
{
    public class Connection
    {
        public bool HasInternetAccess { get; private set; }
        public Action<bool> ConnectionChanged { get; set; }

        public Connection()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformationOnNetworkStatusChanged;
            CheckInternetAccess();
        }

        private void NetworkInformationOnNetworkStatusChanged(object sender)
        {
            CheckInternetAccess();
        }

        private void CheckInternetAccess()
        {
            var oldStatus = HasInternetAccess;

            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            HasInternetAccess = (connectionProfile != null &&
                                 connectionProfile.GetNetworkConnectivityLevel() ==
                                 NetworkConnectivityLevel.InternetAccess);

            if (HasInternetAccess != oldStatus)
                ConnectionChanged?.Invoke(HasInternetAccess);
        }
    }
}
