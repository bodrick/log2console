//Source: https://github.com/trondr/NCmdLiner.SolutionTemplates/tree/8f553ae5d850ced2dfa754d933f42ae6ee1f673b/Console%20Application/src/_S_ConsoleProjectName_S_/Module

using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;

namespace Log2Console.Install
{
    [RunInstaller(true)]
    public partial class CustomInstaller : Installer
    {
        public CustomInstaller()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            //Adding a command to windows explorer contect menu
            Context.LogMessage("Adding Log2Console to File Explorer context menu...");
            new WindowsExplorerContextMenuInstaller().Install("Log2Console", "Open in Log2Console...",
                Assembly.GetExecutingAssembly().Location, "\"%1\"");
            Context.LogMessage("Finnished adding Log2Console to File Explorer context menu.");
            base.Install(stateSaver);
        }

        public override void Uninstall(IDictionary savedState)
        {
            //Removing previously installed command from windows explorer contect menu
            Context.LogMessage("Removing Log2Console from File Explorer context menu...");
            new WindowsExplorerContextMenuInstaller().UnInstall("Log2Console");
            Context.LogMessage("Finished removing Log2Console from File Explorer context menu.");

            base.Uninstall(savedState);
        }
    }
}