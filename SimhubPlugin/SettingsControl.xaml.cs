using System.Windows.Controls;

namespace APR.DashSupport
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public DashPlugin Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();           

        }

        public SettingsControl(DashPlugin plugin) : this()
        {
            this.Plugin = plugin;
        }
    }
}
