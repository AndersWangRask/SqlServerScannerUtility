using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SqlServerScannerUtility
{
    public static class LocalNetworkInfo
    {
        public static List<IPAddress> GetPossibleLocalNetworkAddresses()
        {
            List<IPAddress> networkAddresses = new List<IPAddress>();

            // Get all network interfaces
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                // Skip network interfaces that are not up, have no IP configuration, or are loopback interfaces
                if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                    networkInterface.GetIPProperties().UnicastAddresses.Count == 0 ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                // Process each IP address assigned to this network interface
                foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        List<IPAddress> subnetAddresses = GetAddressesOnSubnet(unicastAddress.Address, unicastAddress.IPv4Mask);
                        networkAddresses.AddRange(subnetAddresses);
                    }
                }
            }

            return 
                networkAddresses
                    .Distinct()
                    .ToList();
        }

        public static List<IPAddress> GetAddressesOnSubnet(IPAddress ipAddress, IPAddress subnetMask)
        {
            List<IPAddress> addresses = new List<IPAddress>();

            // Calculate network prefix length from subnet mask
            int prefixLength = GetPrefixLength(subnetMask);

            // Calculate the network address
            byte[] ipAddressBytes = ipAddress.GetAddressBytes();
            byte[] ipMaskBytes = subnetMask.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ipAddressBytes);
                Array.Reverse(ipMaskBytes);
            }
            uint ip = BitConverter.ToUInt32(ipAddressBytes, 0);
            uint mask = BitConverter.ToUInt32(ipMaskBytes, 0);
            uint networkAddress = ip & mask;

            // Calculate the number of possible addresses
            uint numAddresses = (uint)Math.Pow(2, 32 - prefixLength) - 1;

            for (uint i = 1; i < numAddresses; i++)
            {
                uint currentAddress = networkAddress + i;
                byte[] addressBytes = BitConverter.GetBytes(currentAddress);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(addressBytes);
                }
                IPAddress address = new IPAddress(addressBytes);
                addresses.Add(address);
            }

            return addresses;
        }

        private static int GetPrefixLength(IPAddress subnetMask)
        {
            byte[] maskBytes = subnetMask.GetAddressBytes();
            int prefixLength = 0;

            for (int i = 0; i < maskBytes.Length; i++)
            {
                byte currentByte = maskBytes[i];

                while (currentByte != 0)
                {
                    currentByte <<= 1;
                    prefixLength++;
                }
            }

            return prefixLength;
        }
    }
}