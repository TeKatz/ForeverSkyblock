using System.IO;

namespace ForeverGuiDesigner.Services;

public sealed class SourceFilesChangedEventArgs(IReadOnlyList<string> paths) : EventArgs
{
    public IReadOnlyList<string> Paths { get; } = paths;
}

public sealed class SourceLiveReloadService : IDisposable
{
    private readonly object _gate = new();
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly HashSet<string> _pendingPaths = new(StringComparer.OrdinalIgnoreCase);
    private Timer? _debounceTimer;
    private bool _disposed;

    public event EventHandler<SourceFilesChangedEventArgs>? Changed;

    public string? RepositoryRoot { get; private set; }

    public void Start(string repositoryRoot)
    {
        ThrowIfDisposed();
        Stop();

        RepositoryRoot = Path.GetFullPath(repositoryRoot);
        var javaRoot = Path.Combine(RepositoryRoot, "mod", "src", "main", "java");
        var assetRoot = Path.Combine(RepositoryRoot, "mod", "src", "main", "resources", "assets", "forever_skyblock");

        AddWatcher(javaRoot, "*.java");
        AddWatcher(assetRoot, "*.*");
    }

    public void Stop()
    {
        lock (_gate)
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnChanged;
                watcher.Created -= OnChanged;
                watcher.Deleted -= OnChanged;
                watcher.Renamed -= OnRenamed;
                watcher.Dispose();
            }
            _watchers.Clear();
            _pendingPaths.Clear();
            _debounceTimer?.Dispose();
            _debounceTimer = null;
            RepositoryRoot = null;
        }
    }

    private void AddWatcher(string root, string filter)
    {
        if (!Directory.Exists(root))
            return;

        var watcher = new FileSystemWatcher(root, filter)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName |
                           NotifyFilters.DirectoryName |
                           NotifyFilters.LastWrite |
                           NotifyFilters.Size,
            InternalBufferSize = 32 * 1024
        };
        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (!ShouldTrack(e.FullPath))
            return;
        Queue(e.FullPath);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldTrack(e.OldFullPath))
            Queue(e.OldFullPath);
        if (ShouldTrack(e.FullPath))
            Queue(e.FullPath);
    }

    private static bool ShouldTrack(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".java", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".mcmeta", StringComparison.OrdinalIgnoreCase);
    }

    private void Queue(string path)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _pendingPaths.Add(path);
            _debounceTimer ??= new Timer(Flush, null, Timeout.Infinite, Timeout.Infinite);
            _debounceTimer.Change(300, Timeout.Infinite);
        }
    }

    private void Flush(object? state)
    {
        string[] paths;
        lock (_gate)
        {
            if (_disposed || _pendingPaths.Count == 0)
                return;
            paths = _pendingPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
            _pendingPaths.Clear();
        }

        Changed?.Invoke(this, new SourceFilesChangedEventArgs(paths));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
