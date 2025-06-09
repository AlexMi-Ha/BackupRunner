using BackupRunner.Logs;
using BackupRunner.Models;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace BackupRunner.Archiving;

public class TarBuilder {

    private readonly List<(string filepath, string entryName)> _entries = new();
    private readonly HashSet<string> _excludedExtensions = new(StringComparer.OrdinalIgnoreCase);
    private bool _followSymlinks = false;
    
    private Logger _logger = Logger.GetLogger();

    private TarBuilder() {}
    
    public static TarBuilder Construct() {
        return new TarBuilder();
    }

    public TarBuilder AddFile(string filePath, string? entryName = null) {
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException("File does not exist", filePath);
        }
        if (IsExcluded(filePath)) {
            return this;
        }
        string archivePath = entryName ?? Path.GetFileName(filePath);
        _entries.Add((filePath, archivePath.Replace('\\', '/')));
        return this;
    }

    public TarBuilder AddDirectory(string directoryPath, string? baseArchivePath = null) {
        if (!Directory.Exists(directoryPath)) {
            throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
        }

        var baseDirLength = directoryPath.TrimEnd(Path.DirectorySeparatorChar).Length + 1;
        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)) {
            if (IsExcluded(filePath)) {
                continue;
            }
            var relativePath = filePath.Substring(baseDirLength).Replace('\\', '/');
            var archivePath = baseArchivePath != null ?
                $"{baseArchivePath.TrimEnd('/')}/{relativePath}" :
                filePath;
            AddFile(filePath, archivePath);
        }

        return this;
    }

    public TarBuilder ExcludeExtensions(params string[] extensions) {
        foreach (var ext in extensions) {
            if (!ext.StartsWith('.')) {
                throw new ArgumentException($"Extension must start with a '.'", nameof(extensions));
            }

            _excludedExtensions.Add(ext.ToLowerInvariant());
        }

        return this;
    }

    public TarBuilder FollowSymlinks(bool followSymlinks) {
        _followSymlinks = followSymlinks;
        return this;
    }

    public void Save(string outputTarGzPath) {
        using var outStream = File.Create(outputTarGzPath);
        using var gzipStream = new GZipOutputStream(outStream);
        using var tarArchive = TarArchive.CreateOutputTarArchive(gzipStream);

        foreach (var (filePath, entryName) in _entries) {
            var fileInfo = new FileInfo(filePath);

            if (!_followSymlinks && IsSymlink(fileInfo)) {
                _logger.Log($"Skipping symlink: {filePath}", LogLevel.INFO);
                continue;
            }
            
            TarEntry entry;
            if (IsSymlink(fileInfo)) {
                // TODO: Funktioniert noch nicht richtig
                entry = TarEntry.CreateTarEntry(fileInfo.FullName);
                entry.TarHeader.LinkName = fileInfo.LinkTarget;
                entry.TarHeader.TypeFlag = TarHeader.LF_SYMLINK;
                entry.GetFileTarHeader(entry.TarHeader, fileInfo.FullName);
            } else {
                entry = TarEntry.CreateEntryFromFile(filePath);
            }
            entry.Name = entryName;
            tarArchive.WriteEntry(entry, true);
        }
    }

    private bool IsSymlink(FileInfo file) {
        return file.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    private bool IsExcluded(string filePath) {
        var ext = Path.GetExtension(filePath);
        return _excludedExtensions.Contains(ext);
    }
    
}