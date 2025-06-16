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

    private readonly TarSearcher _tarSearcher = new TarSearcher(unit);
    
    public void BackupUnit() {
        _logger.Log($"Processing {_unit.ToString()}", LogLevel.DEBUG);

        if (!ProcessGuards()) {
            return;
        }

        if (_unit.FollowSymlinks) {
            _logger.Log("FollowSymlinks is not yet supported! Ignoring symlinks!", LogLevel.WARN);
            _unit.FollowSymlinks = false;
        }

        var builder = TarBuilder.Construct()
            .ExcludeExtensions(_unit.Excludes)
            .FollowSymlinks(_unit.FollowSymlinks)
            .IgnoreGitRepositories(_unit.IgnoreGitRepositories);

        foreach (var source in _unit.Sources) {
            var archiveName = _unit.UseAbsolutePaths ? source : Path.GetFileName(source);
            builder.AddDirectory(source, archiveName);
        }

        var outputName = Path.Join(_unit.Destination, $"{_unit.UnitName}-{DateTime.Now:yyyyMMddHHmmss}.tar.gz");
        builder.Save(outputName);
        _logger.Log($"Unit {_unit.UnitName} saved to {outputName}", LogLevel.INFO);
        
        _cleanup.Cleanup();
    }

    public void LoadUnit(bool force) {
        _logger.Log($"Processing {_unit.ToString()}", LogLevel.DEBUG);

        if (!ProcessGuards()) {
            return;
        }
        
        if (_unit.FollowSymlinks) {
            _logger.Log("FollowSymlinks is not yet supported! Ignoring symlinks!", LogLevel.WARN);
            _unit.FollowSymlinks = false;
        }

        var archives = _tarSearcher.FindBackupsFromNewestToOldest();
        if (archives.Count == 0) {
            _logger.Log($"No Backups found for {unit.UnitName}!", LogLevel.ERROR);
            return;
        }
        var latestArchive = archives.First();
        _logger.Log($"Loading Backup {latestArchive.Name}...", LogLevel.INFO);
        var extractor = TarExtractor.Construct(latestArchive.FullName)
            .ForceOverwrite(force);
        
        foreach (var source in _unit.Sources) {
            var archivePath = _unit.UseAbsolutePaths ? source : Path.GetFileName(source.TrimEnd('/', '\\'));
            _logger.Log($"Extracting all archived files in {archivePath} to {source}", LogLevel.DEBUG);
            extractor.MapFolder(archivePath, source);
        }
        _logger.Log("Starting extraction...", LogLevel.DEBUG);
        extractor.Extract();
        _logger.Log($"Finished extraction of {latestArchive.Name}", LogLevel.INFO);
    }

    private bool ProcessGuards() {
        if (!_unit.Enabled) {
            _logger.Log($"Unit {_unit.UnitName} is disabled. Skipping!", LogLevel.WARN);
            return false;
        }
        
        if (!ValidateUnit()) {
            _logger.Log($"Unit Validation failed for {_unit.UnitName}", LogLevel.ERROR);
            return false;
        }

        return true;
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