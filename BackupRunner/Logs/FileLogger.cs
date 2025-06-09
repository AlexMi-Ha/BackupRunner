using System.Text;

namespace BackupRunner.Logs;

public class FileLogger : Logger, IDisposable, IAsyncDisposable {

    private readonly string _logPath;

    private FileStream _fileStream;
    private StreamWriter _logWriter;
    
    public FileLogger(string logPath, LogMode logMode) {
        _logPath = logPath;

        if (logMode == LogMode.OVERWRITE) {
            _fileStream = File.Open(_logPath, FileMode.Create, FileAccess.Write);
        }else if (logMode == LogMode.APPEND) {
            _fileStream = File.Open(_logPath, FileMode.Append, FileAccess.Write);
        } else {
            throw new ArgumentException("Log mode not supported", nameof(logMode));
        }
        _logWriter = new StreamWriter(_fileStream, Encoding.ASCII);
    }
    
    private protected override void HandleLog(string message, LogLevel level) {
        _logWriter.WriteLine(AddLogHeader(message, level, DateTime.Now));
    }

    public void Dispose() {
        _logWriter.Dispose();
        _fileStream.Dispose();
    }

    public async ValueTask DisposeAsync() {
        await _logWriter.DisposeAsync();
        await _fileStream.DisposeAsync();
    }
}