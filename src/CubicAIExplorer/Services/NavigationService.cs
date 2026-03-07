namespace CubicAIExplorer.Services;

public sealed class NavigationService
{
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private string? _currentPath;

    public string? CurrentPath => _currentPath;
    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;

    public event EventHandler<string>? Navigated;

    public void NavigateTo(string path)
    {
        if (_currentPath == path) return;

        if (_currentPath != null)
            _backStack.Push(_currentPath);

        _forwardStack.Clear();
        _currentPath = path;
        Navigated?.Invoke(this, path);
    }

    public string? GoBack()
    {
        if (!CanGoBack) return null;

        if (_currentPath != null)
            _forwardStack.Push(_currentPath);

        _currentPath = _backStack.Pop();
        Navigated?.Invoke(this, _currentPath);
        return _currentPath;
    }

    public string? GoForward()
    {
        if (!CanGoForward) return null;

        if (_currentPath != null)
            _backStack.Push(_currentPath);

        _currentPath = _forwardStack.Pop();
        Navigated?.Invoke(this, _currentPath);
        return _currentPath;
    }
}
