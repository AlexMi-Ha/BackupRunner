using System.Text.RegularExpressions;
using BackupRunner.Models;

namespace BackupRunner.Guards;

public class UnitValidator(Unit unit) {
    
    private const string UnitNameRegex = @"^[a-zA-Z][a-zA-Z0-9_\-]*$";
    private readonly Unit _unit = unit;

    public List<ValidationError> Validate() {
        var errors = new List<ValidationError>();
        CheckUnitName(errors);
        CheckSources(errors);
        CheckExcludes(errors);
        CheckDestination(errors);
        CheckLastBackups(errors);
        
        return errors;
    }

    private void CheckLastBackups(List<ValidationError> errors) {
        if (_unit.KeepLastBackups < 1) {
            errors.Add(new ValidationError($"{nameof(_unit.KeepLastBackups)} must be greater than zero."));
        }
    }

    private void CheckUnitName(List<ValidationError> errors) {
        var match = Regex.Match(_unit.UnitName, UnitNameRegex);
        if (!match.Success) {
            errors.Add(new ValidationError("Unit name must contain only alphanumeric characters, underscores and minuses."));
        }
        if (_unit.UnitName.Length > 30) {
            errors.Add(new ValidationError("Unit name is too long. It must be shorter than 30 characters."));
        }
    }

    private void CheckSources(List<ValidationError> errors) {
        if (_unit.Sources.Length == 0) {
            errors.Add(new ValidationError("Sources cannot be empty."));
            return;
        }
        for (int i = 0; i < _unit.Sources.Length; i++) {
            if (!Path.IsPathRooted(_unit.Sources[i])) {
                errors.Add(new ValidationError($"Source path is not rooted: {_unit.Sources[i]}"));
            }

            if (!Path.Exists(_unit.Sources[i])) {
                errors.Add(new ValidationError($"Source path does not exist: {_unit.Sources[i]}"));
            }
            var basePath = Path.GetFullPath(_unit.Sources[i].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            for (int j = 0; j < _unit.Sources.Length; j++) {
                if (i == j) {
                    continue;
                }
                var otherPath = Path.GetFullPath(_unit.Sources[j].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (IsSubPath(basePath, otherPath)) {
                    errors.Add(new ValidationError($"Sources must not be direct children of each other: {basePath} ; {otherPath}"));
                }
            }
        }
    }

    private void CheckExcludes(List<ValidationError> errors) {
        foreach (var exclude in _unit.Excludes) {
            if(!exclude.StartsWith('.')) {
                errors.Add(new ValidationError($"Extension must start with a '.' {exclude}"));
            }
        }
    }

    private void CheckDestination(List<ValidationError> errors) {
        if (!Path.IsPathRooted(_unit.Destination)) {
            errors.Add(new ValidationError($"Destination path is not rooted: {_unit.Destination}"));
        }

        if (!Path.Exists(_unit.Destination)) {
            errors.Add(new ValidationError($"Destination path does not exist: {_unit.Destination}"));
        }
    }

    private bool IsSubPath(string basePath, string otherPath) {
        return otherPath.StartsWith(basePath + Path.DirectorySeparatorChar);
    }
}