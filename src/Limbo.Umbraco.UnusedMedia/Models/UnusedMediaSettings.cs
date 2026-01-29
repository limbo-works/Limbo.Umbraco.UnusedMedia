namespace Limbo.Umbraco.UnusedMedia.Models;

public class UnusedMediaSettings {

    private string[]? _adminGroups;

    public string[] AdminGroups {
        get => _adminGroups ?? Array.Empty<string>();
        set => _adminGroups = value;
    }

}
