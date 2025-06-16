using BackupRunner.Logs;
using BackupRunner.Models;

namespace BackupRunner.Processors;

public class ConfigProcessor(Config config) {

    private readonly Config _config = config;
    private readonly Logger _logger = Logger.GetLogger();
    
    public void BackupConfig() {
        _logger.Log("Writing Backup...", LogLevel.INFO);
        ProcessConfig(processor => processor.BackupUnit());
    }

    public void LoadConfig(bool force) {
        _logger.Log("Loading Backup...", LogLevel.INFO);
        ProcessConfig(processor => processor.LoadUnit(force));
    }

    private void ProcessConfig(Action<UnitProcessor> unitProcessor) {
        _logger.Log($"Processing config! Found {_config.Units.Length} Units.", LogLevel.INFO);
        foreach(var unit in _config.Units) {
            _logger.Log($"Processing Unit {unit.UnitName}.", LogLevel.INFO);
            var processor = new UnitProcessor(unit);
            try {
                unitProcessor(processor);
            } catch (Exception ex) {
                _logger.Log($"Failed processing Unit {unit.UnitName}. Skipping!", LogLevel.ERROR);
                _logger.Log(ex.ToString(), LogLevel.DEBUG);
            }
        }
    }
}