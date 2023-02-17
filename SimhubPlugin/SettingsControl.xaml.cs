using System.Windows.Controls;

namespace APR.DashSupport
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public APRDashPlugin Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();           

        }

        public SettingsControl(APRDashPlugin plugin) : this()
        {
            this.Plugin = plugin;
        }
    }
}
