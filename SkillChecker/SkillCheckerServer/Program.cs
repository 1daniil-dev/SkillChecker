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

Console.WriteLine("=== SkillChecker Server ===");
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
        Console.WriteLine("Перезагрузка тестов...");
    }
    else
    {
        Console.WriteLine("Неизвестная команда");
    }
}
