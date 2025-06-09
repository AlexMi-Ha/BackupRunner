namespace BackupRunner.Logs;

public abstract class Logger {

    private static readonly List<Logger> CurrentLoggers = [new ConsoleLogger()];

    private static LogLevel _currentLogLevel = LogLevel.INFO;
    
    public void Log(string message, LogLevel level) {
        if (level > _currentLogLevel) {
            return;
        }
        foreach (var logger in CurrentLoggers) {
            logger.HandleLog(message, level);
        }
    }
    
    private protected abstract void HandleLog(string message, LogLevel level);

    private protected string AddLogHeader(string message, LogLevel level, DateTime time) {
        return $"[{level}] ({time:yy-MM-dd HH:mm:ss}) {message}";
    }

    public static void RegisterLogger(Logger logger) {
        CurrentLoggers.Add(logger);
    }

    public static Logger GetLogger() {
        if (CurrentLoggers is null || CurrentLoggers.Count == 0) {
            throw new ApplicationException("No logger is registered");
        }

        return CurrentLoggers[0];
    }

    public static void SetLogLevel(LogLevel level) {
        _currentLogLevel = level;
    }
}