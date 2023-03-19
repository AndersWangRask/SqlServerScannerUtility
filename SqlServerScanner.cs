using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Data.SqlClient;

namespace SqlServerScannerUtility
{
    /// <summary>
    /// Represents information about a SQL Server instance.
    /// </summary>
    public class SqlServerInfo
    {
        public string IPAddress { get; set; }
        public string InstanceName { get; set; }
        public string Version { get; set; }
        public bool CanLogOn { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"IP: {IPAddress}, Instance: {InstanceName}, Can log on: {CanLogOn}, Version: {Version}";
        }
    }

    /// <summary>
    /// Scans for active SQL Server instances listening on a specified port and attempts to connect using Integrated Security.
    /// </summary>
    public class SqlServerScanner
    {
        private const int Port = 1433;

        /// <summary>
        /// Asynchronously scans the local network for active SQL Server instances.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains the list of found SQL Servers.</returns>
        public async Task<List<SqlServerInfo>> ScanAsync()
        {
            List<IPAddress> ipAddresses = LocalNetworkInfo.GetPossibleLocalNetworkAddresses();
            return await ScanAsync(ipAddresses);
        }

        /// <summary>
        /// Asynchronously scans the given list of IP addresses for active SQL Server instances.
        /// </summary>
        /// <param name="ipAddresses">A list of IP addresses to scan.</param>
        /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains the list of found SQL Servers.</returns>
        public async Task<List<SqlServerInfo>> ScanAsync(List<IPAddress> ipAddresses)
        {
            var tasks = new List<Task<SqlServerInfo>>();
            foreach (var ip in ipAddresses)
            {
                tasks.Add(CheckSqlServerAsync(ip));
            }

            var results = await Task.WhenAll(tasks);

            var sqlServers = new List<SqlServerInfo>();
            foreach (var result in results)
            {
                if (result != null)
                {
                    sqlServers.Add(result);
                }
            }

            return sqlServers;
        }

        private async Task<SqlServerInfo> CheckSqlServerAsync(IPAddress ipAddress)
        {
            using var client = new TcpClient();
            try
            {
                await client.ConnectAsync(ipAddress, Port);

                using var connection = new SqlConnection($"Server={ipAddress}, {Port}; Integrated Security=True; TrustServerCertificate=True");
                await connection.OpenAsync();

                //-->
                return new SqlServerInfo
                {
                    IPAddress = ipAddress.ToString(),
                    InstanceName = connection.DataSource,
                    Version = connection.ServerVersion,
                    CanLogOn = true
                };
            }
            catch (SocketException)
            {
                // Connection failed, not a SQL Server
                return null;
            }
            catch (SqlException)
            {
                // Connection failed, but it's a SQL Server
                return new SqlServerInfo
                {
                    IPAddress = ipAddress.ToString(),
                    InstanceName = string.Empty,
                    Version = string.Empty,
                    CanLogOn = false
                };
            }
        }
    }
}
