using System.Windows.Forms;

namespace Log2Console.Settings
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();

            // UI Settings
            settingsPropertyGrid.SelectedObject = UserSettings.Instance;
        }
    }
}