using BackupRunner.Logs;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace BackupRunner.Archiving;

public class TarExtractor {

    private readonly string _archivePath;
    private readonly Dictionary<string, string> _folderMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private bool _forceOverwrite;
    
    private Logger _logger = Logger.GetLogger();

    public static TarExtractor Construct(string archivePath) {
        return new TarExtractor(archivePath);
    }
    
    private TarExtractor(string archivePath) {
        if (!File.Exists(archivePath)) {
            throw new FileNotFoundException($"Could not find archive",archivePath);
        }
        _archivePath = archivePath;
    }

    public TarExtractor MapFolder(string archiveFolder, string destinationPath) {
        archiveFolder = archiveFolder.Replace("\\", "/");
        destinationPath = destinationPath.Replace("\\", "/");
        
        archiveFolder = archiveFolder.Trim('/');
        if (!Directory.Exists(destinationPath)) {
            Directory.CreateDirectory(destinationPath);
        }

        _folderMappings[archiveFolder] = Path.GetFullPath(destinationPath);
        return this;
    }

    public TarExtractor ForceOverwrite(bool force) {
        _forceOverwrite = force;
        return this;
    }

    public void Extract() {
        using var fileStream = File.OpenRead(_archivePath);
        using var gzipStream = new GZipInputStream(fileStream);
        using var tarStream = new TarInputStream(gzipStream);
        
        TarEntry entry;
        while ((entry = tarStream.GetNextEntry()) != null) {
            if (entry.IsDirectory) {
                continue;
            }

            var archivePath = entry.Name.Replace('\\', '/');
            var mapping = _folderMappings.FirstOrDefault(m => archivePath.StartsWith($"{m.Key}/", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(mapping.Key)) {
                _logger.Log($"Skipping unmapped archive path: {archivePath}", LogLevel.WARN);
                continue;
            }

            string relativePath = archivePath.Substring(mapping.Key.Length + 1);
            string destinationFile = Path.Combine(mapping.Value, relativePath);
            
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            if (File.Exists(destinationFile) && !_forceOverwrite) {
                _logger.Log($"Skipping already existing file: {destinationFile}", LogLevel.INFO);
                continue;
            }

            using var outStream = File.Create(destinationFile);
            tarStream.CopyEntryContents(outStream);
            
            _logger.Log($"Extracted {archivePath} to {destinationFile}", LogLevel.INFO);
        }
    }
}