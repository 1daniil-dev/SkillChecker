using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using SkillChecker.Common.Security;
using SkillCheckerServer;

int port = 9000;

if (args.Length > 0 && int.TryParse(args[0], out int customPort))
{
    port = customPort;
}

Server server = new Server(port);

string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
string authFile;
if (Directory.Exists(Path.Combine(solutionDir, "SkillCheckerServer", "Tests")))
{
    authFile = Path.Combine(solutionDir, "SkillCheckerServer", "Data", "auth.json");
}
else
{
    authFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "auth.json");
}

Thread serverThread = new Thread(() => server.Start());
serverThread.IsBackground = true;
serverThread.Start();

Thread.Sleep(500);

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

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("  SkillChecker");
Console.ResetColor();
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.Green;
Console.Write("  Сервер запущен: ");
Console.ResetColor();
if (ipList.Count > 0)
{
    for (int i = 0; i < ipList.Count; i++)
    {
        Console.WriteLine(ipList[i] + ":" + port);
    }
}
else
{
    Console.WriteLine("127.0.0.1:" + port);
}

List<string> testNames = server.GetTestNames();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.Write("  Загружено тестов: ");
Console.ResetColor();
Console.WriteLine(testNames.Count.ToString());
if (testNames.Count > 0)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  Тесты: ");
    Console.ResetColor();
    for (int i = 0; i < testNames.Count; i++)
    {
        if (i > 0) Console.Write(", ");
        Console.Write(testNames[i]);
    }
    Console.WriteLine();
}

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.Write("  Порт: ");
Console.ResetColor();
Console.WriteLine(port.ToString());
Console.WriteLine();

ShowMenu();

while (true)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("> ");
    Console.ResetColor();
    string? input = Console.ReadLine();
    if (input == null) continue;

    string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
    {
        ShowMenu();
        continue;
    }

    string cmd = parts[0].ToLower();

    if (cmd == "1" || cmd == "results")
    {
        server.ShowResults();
    }
    else if (cmd == "2" || cmd == "reload")
    {
        server.LoadAllTests();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  Тесты перезагружены");
        Console.ResetColor();
    }
    else if (cmd == "3" || cmd == "schedule")
    {
        if (parts.Length >= 3)
        {
            server.SetSchedule(parts[1], parts[2]);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Формат: schedule ИМЯ_ТЕСТА HH:mm");
            Console.ResetColor();
        }
    }
    else if (cmd == "4" || cmd == "tests")
    {
        List<string> names = server.GetTestNames();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  Тесты (" + names.Count + "):");
        Console.ResetColor();
        for (int i = 0; i < names.Count; i++)
        {
            Console.WriteLine("    " + (i + 1) + ". " + names[i]);
        }
    }
    else if (cmd == "5" || cmd == "exit")
    {
        server.Stop();
        break;
    }
    else if (cmd == "6" || cmd == "password")
    {
        ResetPassword();
    }
    else if (cmd == "?" || cmd == "help" || cmd == "меню")
    {
        ShowMenu();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  Неизвестная команда. Введите ? для меню.");
        Console.ResetColor();
    }
}

void ShowMenu()
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  Команды:");
    Console.WriteLine("    1. Результаты");
    Console.WriteLine("    2. Перезагрузить тесты");
    Console.WriteLine("    3. Запланировать тест");
    Console.WriteLine("    4. Список тестов");
    Console.WriteLine("    5. Выход");
    Console.WriteLine("    6. Сброс пароля");
    Console.WriteLine("  Введите номер или команду (? — меню)");
    Console.ResetColor();
}

void ResetPassword()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  Сброс / смена пароля веб-панели");
    Console.ResetColor();
    Console.Write("  Введите новый пароль (или Enter для полного сброса): ");
    string? newPass = Console.ReadLine();

    if (string.IsNullOrEmpty(newPass))
    {
        if (File.Exists(authFile))
        {
            File.Delete(authFile);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  Пароль сброшен. При входе в веб-панель потребуется создать новый.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Пароль и так не установлен.");
            Console.ResetColor();
        }
    }
    else
    {
        if (newPass.Length < 4)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Пароль должен быть не менее 4 символов.");
            Console.ResetColor();
            return;
        }
        string hash = PasswordHasher.Hash(newPass);
        string? dir = Path.GetDirectoryName(authFile);
        if (dir != null) Directory.CreateDirectory(dir);
        string json = "{\n  \"PasswordHash\": \"" + hash + "\"\n}";
        File.WriteAllText(authFile, json, Encoding.UTF8);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  Пароль изменён.");
        Console.ResetColor();
    }
}
