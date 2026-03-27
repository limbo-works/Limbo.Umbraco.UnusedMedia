namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListLayoutItem {

    public string ContentUdi { get; set; }

    public string? SettingsUdi { get; set; }

    public UnusedMediaBlockListLayoutItem(string contentUdi, string? settingsUdi) {
        ContentUdi = contentUdi;
        SettingsUdi = settingsUdi;
    }

}