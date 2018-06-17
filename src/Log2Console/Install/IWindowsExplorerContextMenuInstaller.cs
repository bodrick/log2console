//Source: https://github.com/trondr/NCmdLiner.SolutionTemplates/tree/8f553ae5d850ced2dfa754d933f42ae6ee1f673b/Console%20Application/src/_S_LibraryProjectName_S_/Module/Common/Install

namespace Log2Console.Install
{
    public interface IWindowsExplorerContextMenuInstaller
    {
        void Install(string commandId, string commandName, string command, string arguments);

        void UnInstall(string commandId);
    }
}