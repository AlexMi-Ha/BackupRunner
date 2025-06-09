namespace BackupRunner.Guards;

public class ValidationError(string message) {
    
    public string Message { get; init; } = message;
}