using BackupRunner.Archiving;
using BackupRunner.Guards;
using BackupRunner.Logs;
using BackupRunner.Models;

namespace BackupRunner.Processors;

public class UnitProcessor(Unit unit) {
    
    private readonly Unit _unit = unit;
    private readonly Logger _logger = Logger.GetLogger();

    private readonly UnitValidator _unitValidator = new UnitValidator(unit);

    private readonly CleanupProcessor _cleanup = new CleanupProcessor(unit);
    
    public void ProcessUnit() {
        _logger.Log($"Processing {_unit.ToString()}", LogLevel.DEBUG);
        
        if (!_unit.Enabled) {
            _logger.Log($"Unit {_unit.UnitName} is disabled. Skipping!", LogLevel.WARN);
            return;
        }
        
        if (!ValidateUnit()) {
            _logger.Log($"Unit Validation failed for {_unit.UnitName}", LogLevel.ERROR);
            return;
        }

        if (_unit.FollowSymlinks) {
            _logger.Log("FollowSymlinks is not yet supported! Ignoring symlinks!", LogLevel.WARN);
            _unit.FollowSymlinks = false;
        }
        
        var builder = TarBuilder.Construct()
            .ExcludeExtensions(_unit.Excludes)
            .FollowSymlinks(_unit.FollowSymlinks);

        foreach (var source in _unit.Sources) {
            var archiveName = _unit.UseAbsolutePaths ? source : Path.GetFileName(source);
            builder.AddDirectory(source, archiveName);
        }

        var outputName = Path.Join(_unit.Destination, $"{_unit.UnitName}-{DateTime.Now:yyyyMMddHHmmss}.tar.gz");
        builder.Save(outputName);
        _logger.Log($"Unit {_unit.UnitName} saved to {outputName}", LogLevel.INFO);
        
        _cleanup.Cleanup();
    }

    private bool ValidateUnit() {
        _logger.Log($"Validating Unit {_unit.UnitName}...", LogLevel.INFO);
        var errors = _unitValidator.Validate();
        if (errors.Count == 0) {
            _logger.Log($"No errors found! Unit {_unit.UnitName} is valid.", LogLevel.INFO);
            return true;
        }
        foreach (var error in errors) {
            _logger.Log(error.Message, LogLevel.ERROR);
        }

        return false;
    }
}