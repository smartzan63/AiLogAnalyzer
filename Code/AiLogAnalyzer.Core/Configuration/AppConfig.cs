namespace AiLogAnalyzer.Core.Configuration;

public class AppConfig
{
    public HotKeySettings HotKeySettings { get; set; } = new ();
    public GeneralSettings GeneralSettings { get; set; } = new ();
}