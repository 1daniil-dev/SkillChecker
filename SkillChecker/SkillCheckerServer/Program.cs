using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using SkillCheckerServer;

int port = 9000;

if (args.Length > 0 && int.TryParse(args[0], out int customPort))
{
    port = customPort;
}

Server server = new Server(port);

Thread serverThread = new Thread(() => server.Start());
serverThread.IsBackground = true;
serverThread.Start();

NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
List<string> ipList = new List<string>();
for (int i = 0; i < interfaces.Length; i++)
{
    NetworkInterface ni = interfaces[i];
    if (ni.OperationalStatus != OperationalStatus.Up) continue;
    if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
        ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;

    IPInterfaceProperties props = ni.GetIPProperties();
    UnicastIPAddressInformationCollection addresses = props.UnicastAddresses;
    foreach (UnicastIPAddressInformation addr in addresses)
    {
        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
        {
            ipList.Add(addr.Address.ToString());
        }
    }
}

Console.WriteLine("=== SkillChecker Server ===");
if (ipList.Count > 0)
{
    for (int i = 0; i < ipList.Count; i++)
    {
        Console.WriteLine("IP: " + ipList[i] + ":" + port);
    }
}
else
{
    Console.WriteLine("IP: 127.0.0.1:" + port);
}
Console.WriteLine("Команды:");
Console.WriteLine("  results     — показать результаты");
Console.WriteLine("  schedule ИМЯ_ТЕСТА HH:mm — запланировать начало");
Console.WriteLine("  reload      — перезагрузить тесты");
Console.WriteLine("  exit        — остановить сервер");
Console.WriteLine();

while (true)
{
    string? input = Console.ReadLine();
    if (input == null) continue;

    string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) continue;

    string cmd = parts[0].ToLower();

    if (cmd == "exit")
    {
        server.Stop();
        break;
    }
    else if (cmd == "results")
    {
        server.ShowResults();
    }
    else if (cmd == "schedule" && parts.Length >= 3)
    {
        server.SetSchedule(parts[1], parts[2]);
    }
    else if (cmd == "reload")
    {
        server.LoadAllTests();
        Console.WriteLine("Тесты перезагружены");
    }
    else
    {
        Console.WriteLine("Неизвестная команда");
    }
}
