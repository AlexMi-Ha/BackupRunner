namespace BackupRunner.Models;

public class Unit {
    
    public required string UnitName { get; set; }

    public required string[] Sources { get; set; }
    public required string Destination { get; set; }

    public string[] Excludes { get; set; } = [];

    public bool UseAbsolutePaths { get; set; } = true;
    public bool FollowSymlinks { get; set; } = false;
    public bool IgnoreGitRepositories { get; set; } = true;
    
    public int KeepLastBackups { get; set; } = 3;

    public bool Enabled { get; set; } = true;

    public override string ToString() {
        return
            $"{nameof(UnitName)}: {UnitName}, {nameof(Sources)}: {Sources}, {nameof(Destination)}: {Destination}, {nameof(Excludes)}: {Excludes}, {nameof(UseAbsolutePaths)}: {UseAbsolutePaths}, {nameof(FollowSymlinks)}: {FollowSymlinks}, {nameof(Enabled)}: {Enabled}";
    }
}