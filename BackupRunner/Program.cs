using BackupRunner.Logs;
using BackupRunner.Models;
using BackupRunner.Processors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

if (args.Length == 0 || args.Contains("-h") || args.Contains("--help")) {
    PrintHelp();
    return;
}

string configPath = null;
LogLevel logLevel = LogLevel.INFO;
string? logPath = null;
LogMode logMode = LogMode.OVERWRITE;

for (int i = 0; i < args.Length; i++) {
    switch (args[i]) {
        case "-c":
        case "--config":
            if (i + 1 < args.Length) {
                configPath = args[++i];
            } else {
                Console.WriteLine("Error: Missing config path");
                return;
            }
            break;
        case "-v":
        case "--verbose":
            if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) {
                logLevel = ParseLogLevel(args[++i]);
            }

            break;
        case "--log-path":
            if (i + 1 < args.Length) {
                logPath = args[++i];
            } else {
                Console.WriteLine("Error: Missing log path");
                return;
            }
            break;
        case "--log-mode":
            if (i + 1 < args.Length) {
                logMode = ParseLogMode(args[++i]);
            } else {
                Console.WriteLine("Error: Missing log mode");
                return;
            }
            break;
        default:
            PrintHelp();
            return;
    }
}

if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath)) {
    Console.WriteLine("Error: Missing config file");
    return;
}

Logger.SetLogLevel(logLevel);
if (!string.IsNullOrEmpty(logPath)) {
    try {
        Logger.RegisterLogger(new FileLogger(logPath, logMode));
    } catch (Exception ex) {
        Console.WriteLine($"Failed initializing FileLogger {ex.Message}");
        return;
    }
}

Config config;
try {
    var yaml = File.ReadAllText(configPath);
    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    config = deserializer.Deserialize<Config>(yaml);
} catch (Exception ex) {
    Console.WriteLine($"Failed parsing yaml in {configPath}");
    Console.WriteLine(ex.Message);
    return;
}

try {
    var processor = new ConfigProcessor(config);
    processor.ProcessConfig();
} catch (Exception ex) {
    Console.WriteLine("Failed processing config");
    Console.WriteLine(ex.Message);
}

return;

void PrintHelp() {
    Console.WriteLine(@"
Usage: BackupRunner -c <config.yaml> [options]

Options:
  -c, --config <file>     Path to the config YAML file
  -v, --verbose [level]   Enable verbose output. Optional level: Debug, Info, Warn, Error
  --log-path <path>             Optional path to a log file
  --log-mode <append|overwrite> Optional log mode for file output (default: overwrite)
  -h, --help              Show this help message
");
}

LogLevel ParseLogLevel(string arg) {
    return Enum.TryParse<LogLevel>(arg, true, out var level) ? level : LogLevel.INFO;
}

LogMode ParseLogMode(string arg) {
    return Enum.TryParse<LogMode>(arg, true, out var mode) ? mode : LogMode.OVERWRITE;
}

