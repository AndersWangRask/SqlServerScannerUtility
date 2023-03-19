// See https://aka.ms/new-console-template for more information
using SqlServerScannerUtility;

Console.WriteLine("Scanning for SQL Servers! Please wait!");

var sqlServerScanner = new SqlServerScanner();
var sqlServers = await sqlServerScanner.ScanAsync();

foreach (var sqlServer in sqlServers)
{
    if (sqlServer.CanLogOn)
    {
        Console.WriteLine("");
        Console.WriteLine(sqlServer.ToString().Replace(", Version:", "\nVersion:"));
        Console.WriteLine("");
    }
    else
    {
        Console.WriteLine(sqlServer.ToString());
    }
}

Console.WriteLine($"Scan complete: Found {sqlServers.Count} server(s).");