using BackupRunner.Logs;
using BackupRunner.Models;

namespace BackupRunner.Processors;

public class ConfigProcessor(Config config) {

    private readonly Config _config = config;
    private readonly Logger _logger = Logger.GetLogger();
    
    public void ProcessConfig() {
        _logger.Log($"Processing config! Found {_config.Units.Length} Units.", LogLevel.INFO);
        foreach(var unit in _config.Units) {
            _logger.Log($"Processing Unit {unit.UnitName}.", LogLevel.INFO);
            var processor = new UnitProcessor(unit);
            try {
                processor.ProcessUnit();
            } catch (Exception ex) {
                _logger.Log($"Failed processing Unit {unit.UnitName}. Skipping!", LogLevel.ERROR);
                _logger.Log(ex.ToString(), LogLevel.DEBUG);
            }
        }
    }
}