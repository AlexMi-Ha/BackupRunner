using System.Text.RegularExpressions;
using BackupRunner.Models;

namespace BackupRunner.Archiving;

public class TarSearcher(Unit unit) {
    private readonly string _backupFolder = unit.Destination;
    private readonly string _unitName = unit.UnitName;

    public List<FileInfo> FindBackupsFromNewestToOldest() {
        var fileMatcher = $@"^{Regex.Escape(_unitName)}-(\d{{14}})\.tar\.gz$";
        var regex = new Regex(fileMatcher);
        return new DirectoryInfo(_backupFolder)
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
            .Select(f => f.FileInfo)
            .ToList();
    }
}