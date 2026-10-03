using System.Windows;

namespace CEFRSubtitleMasker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LevelSelectionWindow selectionWindow = new LevelSelectionWindow();
            bool? result = selectionWindow.ShowDialog();

            if (result == true)
            {
                MainWindow mainWindow = new MainWindow(selectionWindow.SelectedLevel);
                
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                this.MainWindow = mainWindow;
                
                mainWindow.Show();
            }
            else
            {
                this.Shutdown();
            }
        }
    }
}