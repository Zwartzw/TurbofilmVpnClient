using System;
using System.Linq;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace TurbofilmVpn.Utils
{
    internal static class NativeMethods
    {
        // todo: MIB_IPFORWARD_ROW2 for ipv6 support
        // todo: CreateIpForwardEntry2 for ipv6 support
        // todo: DeleteIpForwardEntry2 for ipv6 support
        // todo: SetIpForwardEntry2 for ipv6 support
        
        #region ERROR_CODES

        /// <summary>Gets error name by integer result code.</summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static string GetErrorName(int result)
        {
            var fields = typeof(NativeMethods).GetFields();
            foreach (var fi in fields.Where(fi => (int)fi.GetValue(null) == result))
                return fi.Name;
            return string.Format("RET_CODE {0} UNKNOWN", result);
        }

        /// <summary>The operation completed successfully.</summary>
        public const int ERROR_SUCCESS = 0;

        /// <summary>The system cannot find the file specified.</summary>
        public const int ERROR_FILE_NOT_FOUND = 2;

        /// <summary>The system cannot find the path specified.</summary>
        public const int ERROR_PATH_NOT_FOUND = 3;

        /// <summary>Access is denied.</summary>
        public const int ERROR_ACCESS_DENIED = 5;

        /// <summary>The request is not supported.</summary>
        public const int ERROR_NOT_SUPPORTED = 50;

        /// <summary>The parameter is incorrect.</summary>
        public const int ERROR_INVALID_PARAMETER = 87;

        /// <summary>Element not found.</summary>
        /// <remarks>Cannot delete or modify a route that does not exist.</remarks>
        public const int ERROR_NOT_FOUND = 1168;

        /// <summary>The object already exists.</summary>
        /// <remarks>The route already exists.</remarks>
        public const int ERROR_OBJECT_ALREADY_EXISTS = 5010;

        #endregion // ERROR_CODES

        public enum ForwardType
        {
            Other = 1,
            Invalid = 2,
            Direct = 3,
            Indirect = 4
        }

        public enum ForwardProtocol
        {
            Other = 1,
            Local = 2,
            NetMGMT = 3,
            ICMP = 4,
            EGP = 5,
            GGP = 6,
            Hello = 7,
            RIP = 8,
            IS_IS = 9,
            ES_IS = 10,
            CISCO = 11,
            BBN = 12,
            OSPF = 13,
            BGP = 14,
            NT_AUTOSTATIC = 10002,
            NT_STATIC = 10006,
            NT_STATIC_NON_DOD = 10007
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_IPFORWARDROW
        {
            public uint dwForwardDest;
            public uint dwForwardMask;
            public int dwForwardPolicy;
            public uint dwForwardNextHop;
            public uint dwForwardIfIndex;
            public ForwardType dwForwardType;
            public ForwardProtocol dwForwardProto;
            public uint dwForwardAge;
            public uint dwForwardNextHopAS;
            public uint dwForwardMetric1;
            public uint dwForwardMetric2;
            public uint dwForwardMetric3;
            public uint dwForwardMetric4;
            public uint dwForwardMetric5;
        }

        /// <summary>Creates a route in the local computer's IPv4 routing table.</summary>
        /// <remarks><see cref="http://msdn.microsoft.com/en-us/library/aa365860.aspx"/></remarks>
        /// <param name="pRoute"></param>
        /// <returns></returns>
        [DllImport("Iphlpapi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int CreateIpForwardEntry(ref MIB_IPFORWARDROW pRoute);

        /// <summary>Deletes an existing route in the local computer's IPv4 routing table.</summary>
        /// <remarks><see cref="http://msdn.microsoft.com/en-us/library/aa365878.aspx"/></remarks>
        /// <param name="pRoute"></param>
        /// <returns></returns>
        [DllImport("Iphlpapi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int DeleteIpForwardEntry(ref MIB_IPFORWARDROW pRoute);

        /// <summary>Modifies an existing route in the local computer's IPv4 routing table.</summary>
        /// <remarks><see cref="http://msdn.microsoft.com/en-us/library/aa366363.aspx"/></remarks>
        /// <param name="pRoute"></param>
        /// <returns></returns>
        [DllImport("Iphlpapi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int SetIpForwardEntry(ref MIB_IPFORWARDROW pRoute);

        /// <summary>Clears DNS cache.</summary>
        /// <returns>1 if success.</returns>
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern uint DnsFlushResolverCache();

        /// <summary>Clears DNS cache.</summary>
        public static bool FlushMyCache()
        {
            return DnsFlushResolverCache() == 1;
        }

        /// <summary>Creates or deletes a route in the local computer's IPv4 routing table.</summary>
        /// <param name="add"></param>
        /// <param name="destination"></param>
        /// <param name="mask"></param>
        /// <param name="gateway"></param>
        /// <param name="index"></param>
        /// <param name="metric"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool ModifyIpForwardEntry(bool add, string destination, string mask, string gateway, uint index, uint metric, out string message)
        {
            var route = new MIB_IPFORWARDROW();
            if (index == 0)
            {
                message = "Network interface index not found.";
                return false;
            }
            if (metric == 1)
            {
                message = "Metric value not found.";
                return false;
            }

            route.dwForwardDest = GetIPFromString(destination);
            route.dwForwardMask = GetIPFromString(mask);
            route.dwForwardNextHop = GetIPFromString(gateway);
            route.dwForwardIfIndex = index;
            route.dwForwardMetric1 = metric;
            route.dwForwardProto = ForwardProtocol.NetMGMT;

            int result = add ? CreateIpForwardEntry(ref route) : DeleteIpForwardEntry(ref route);

            message = string.Empty;

            if (result == ERROR_SUCCESS)
                return true;

            if (result == ERROR_INVALID_PARAMETER)
            {
                route.dwForwardNextHop = 0;
                result = add ? CreateIpForwardEntry(ref route) : DeleteIpForwardEntry(ref route);
            }

            if (result == ERROR_SUCCESS)
                return true;

            if (result == ERROR_OBJECT_ALREADY_EXISTS)
            {
                message = "Route already exist.";
                return true;
            }
            if (result == ERROR_NOT_FOUND)
            {
                message = "Cannot delete or modify a route that does not exist.";
                return true;
            }

            message = new System.ComponentModel.Win32Exception(result).Message;

            return false;
        }

        private static uint GetIPFromString(string address)
        {
            if (string.IsNullOrEmpty(address))
                return 0;

            var parts = address.Split('.');
            return ((uint.Parse(parts[3]) << 24) + ((uint.Parse(parts[2])) << 16) + ((uint.Parse(parts[1])) << 8) + uint.Parse(parts[0]));
        }

        /*public static ulong IP2Int(string address)
        {
            if (string.IsNullOrEmpty(address))
                return 0;

            var parts = address.Split('.');
            return ((uint.Parse(parts[0]) << 24) + ((uint.Parse(parts[1])) << 16) + ((uint.Parse(parts[2])) << 8) + uint.Parse(parts[3]));
        }*/
    }
}
// ReSharper enable FieldCanBeMadeReadOnly.Local
// ReSharper enable MemberCanBePrivate.Local
// ReSharper enable UnusedMember.Global
// ReSharper enable InconsistentNaming
