namespace BackupRunner.Logs;

public class ConsoleLogger : Logger {
    
    private protected override void HandleLog(string message, LogLevel level) {
        Console.WriteLine(AddLogHeader(message, level, DateTime.Now));
    }
}