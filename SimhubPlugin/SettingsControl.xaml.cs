using System.Windows.Controls;
using System.Windows.Markup;

namespace APR.DashSupport
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControl : UserControl, IComponentConnector {
        public APRDashPlugin Plugin { get; }
        public DashPluginSettings Settings { get; }


        public SettingsControl()
        {
            InitializeComponent();           

        }

        public SettingsControl(APRDashPlugin plugin) : this()
        {
            this.Plugin = plugin;
            this.Settings = plugin.Settings;
 
        }

        public void SettingsUpdated_Click(object sender, System.Windows.RoutedEventArgs e) =>  Settings.SettingsUpdated = true;
    }
}
