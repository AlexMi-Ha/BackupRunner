using System.Text.RegularExpressions;
using BackupRunner.Archiving;
using BackupRunner.Logs;
using BackupRunner.Models;

namespace BackupRunner.Processors;

public class CleanupProcessor(Unit unit) {
    private readonly int _numKeepNewest = unit.KeepLastBackups;
    private readonly string _backupFolder = unit.Destination;
    private readonly string _unitName = unit.UnitName;
    private readonly TarSearcher _tarSearcher = new TarSearcher(unit);

    private readonly Logger _logger = Logger.GetLogger();
    
    public void Cleanup() {
        _logger.Log($"Cleaning up older backups of Unit {_unitName}", LogLevel.INFO);
        var matchingFiles = _tarSearcher.FindBackupsFromNewestToOldest();
        
        _logger.Log($"Found {matchingFiles.Count} backups of Unit {_unitName}. Keeping the newest {_numKeepNewest}...", LogLevel.INFO);

        if (matchingFiles.Count <= _numKeepNewest) {
            _logger.Log("No deletions.", LogLevel.INFO);
            return;
        }
        
        var filesToDelete = matchingFiles.Skip(_numKeepNewest);
        foreach (var file in filesToDelete) {
            try {
                _logger.Log($"Deleting {file.FullName}.", LogLevel.INFO);
                file.Delete();
            } catch (Exception ex) {
                _logger.Log($"Failed deleting {file.FullName}. {ex.Message}", LogLevel.ERROR);
                _logger.Log(ex.ToString(), LogLevel.DEBUG);
            }
        }
    }
}