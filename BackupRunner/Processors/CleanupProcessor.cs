using System.Text.RegularExpressions;
using BackupRunner.Logs;
using BackupRunner.Models;

namespace BackupRunner.Processors;

public class CleanupProcessor(Unit unit) {
    private readonly int _numKeepNewest = unit.KeepLastBackups;
    private readonly string _backupFolder = unit.Destination;
    private readonly string _unitName = unit.UnitName;

    private readonly Logger _logger = Logger.GetLogger();
    
    public void Cleanup() {
        _logger.Log($"Cleaning up older backups of Unit {_unitName}", LogLevel.INFO);
        var fileMatcher = $@"^{Regex.Escape(_unitName)}-(\d{{14}})\.tar\.gz$";
        var regex = new Regex(fileMatcher);

        var matchingFiles = new DirectoryInfo(_backupFolder)
            .GetFiles("*.tar.gz", SearchOption.TopDirectoryOnly)
            .Select(f => new {
                FileInfo = f,
                Match = regex.Match(f.Name)
            })
            .Where(f => f.Match.Success)
            .Select(f => new {
                FileInfo = f.FileInfo,
                Timestamp = DateTime.ParseExact(f.Match.Groups[1].Value, "yyyyMMddHHmmss", null),
            })
            .OrderByDescending(f => f.Timestamp)
            .ToList();
        
        _logger.Log($"Found {matchingFiles.Count} backups of Unit {_unitName}. Keeping the newest {_numKeepNewest}...", LogLevel.INFO);

        if (matchingFiles.Count <= _numKeepNewest) {
            _logger.Log("No deletions.", LogLevel.INFO);
            return;
        }
        
        var filesToDelete = matchingFiles.Skip(_numKeepNewest);
        foreach (var file in filesToDelete) {
            try {
                _logger.Log($"Deleting {file.FileInfo.FullName}.", LogLevel.INFO);
                file.FileInfo.Delete();
            } catch (Exception ex) {
                _logger.Log($"Failed deleting {file.FileInfo.FullName}. {ex.Message}", LogLevel.ERROR);
                _logger.Log(ex.ToString(), LogLevel.DEBUG);
            }
        }
    }
}