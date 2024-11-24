using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AiLogAnalyzer.UI.Components
{
    public sealed partial class WelcomeMessagePage : Page, INotifyPropertyChanged
    {
        private const int AnimationDurationMs = 1000;
        private string _timerText = "Starting in 1...";
        public string TimerText
        {
            get => _timerText;
            set
            {
                _timerText = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public WelcomeMessagePage()
        {
            InitializeComponent();
            DataContext = this;
            StartAnimation(AnimationDurationMs);
        }

        private async void StartAnimation(int durationMs)
        {
            const int stepIntervalMs = 300;
            var dotCount = 0;

            while (durationMs > 0)
            {
                dotCount = (dotCount % 3) + 1;

                TimerText = $"Starting{new string('.', dotCount)}";

                await Task.Delay(stepIntervalMs);

                durationMs -= stepIntervalMs;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}