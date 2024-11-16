using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace AiLogAnalyzer.UI.Components;

public sealed partial class WelcomeMessagePage : Page
{
    private const int AnimationDurationMs = 1000;

    public WelcomeMessagePage()
    {
        this.InitializeComponent();
        StartAnimation(AnimationDurationMs);
    }
    
    private async void StartAnimation(int durationMs)
    {
        const int stepIntervalMs = 300;
        var dotCount = 0;
        
        while (durationMs > 0)
        {
            dotCount = (dotCount % 3) + 1;

            TimerTextBlock.Text = $"Starting{new string('.', dotCount)}";

            await Task.Delay(stepIntervalMs);

            durationMs -= stepIntervalMs;
        }
    }
}