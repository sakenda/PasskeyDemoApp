namespace PasskeyHelper.Models;

public class AttestationStateService
{
    private readonly Dictionary<string, string> _state = new();

    public void Set(string key, string value) => _state[key] = value;
    public string? Get(string key) => _state.TryGetValue(key, out var value) ? value : null;
}
     