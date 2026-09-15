using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace LiarUtil.Gui.Services;

public sealed class FilePickerService(SnackbarService snackbar, LocalizationService loc)
{
    private TopLevel? _topLevel;

    public void Attach(TopLevel topLevel) => _topLevel = topLevel;

    public async Task<string?> PickFileAsync()
    {
        if (_topLevel is null)
        {
            return null;
        }

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = loc.SelectFile,
            AllowMultiple = false,
        });
        return files.Count > 0 ? await ResolveFileAsync(files[0]) : null;
    }

    public async Task<string?> PickFolderAsync()
    {
        if (_topLevel is null)
        {
            return null;
        }

        var folders = await _topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = loc.SelectFolder,
            AllowMultiple = false,
        });
        return folders.Count > 0 ? ResolveFolderPath(folders[0]) : null;
    }

    public async Task<string?> SaveFileAsync()
    {
        if (_topLevel is null)
        {
            return null;
        }

        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = loc.SelectOutput,
        });
        if (file is null)
        {
            return null;
        }

        var path = file.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            return path;
        }

        var fallback = Path.Combine(PrivateDirectory, "output", file.Name);
        snackbar.Show(string.Format(loc.OutputRedirectedToPrivate, Path.GetDirectoryName(fallback)));
        return fallback;
    }

    private static string PrivateDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiarUtil");

    private async Task<string> ResolveFileAsync(IStorageFile file)
    {
        var path = file.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            return path;
        }

        var target = Path.Combine(PrivateDirectory, "picked", file.Name);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await using (var source = await file.OpenReadAsync())
        await using (var destination = File.Create(target))
        {
            await source.CopyToAsync(destination);
        }

        snackbar.Show(loc.PickedCopiedToPrivate);
        return target;
    }

    private string ResolveFolderPath(IStorageFolder folder)
    {
        var path = folder.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            return path;
        }

        var fallback = Path.Combine(PrivateDirectory, "folder", folder.Name);
        Directory.CreateDirectory(fallback);
        snackbar.Show(string.Format(loc.OutputRedirectedToPrivate, fallback));
        return fallback;
    }
}
