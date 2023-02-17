using System.Windows.Controls;

namespace APR.DashSupport
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public APRDashPlugin Plugin { get; }

        public Settings()
        {
            InitializeComponent();
        }

        public Settings(APRDashPlugin plugin) : this()
        {
            this.Plugin = plugin;
        }


    }
}
