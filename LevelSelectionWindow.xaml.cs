using System.Windows;
using System.Windows.Controls;

namespace CEFRSubtitleMasker
{
    public partial class LevelSelectionWindow : Window
    {
        public int SelectedLevel { get; private set; } = 3; 

        public LevelSelectionWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (LevelComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (int.TryParse(selectedItem.Tag.ToString(), out int level))
                {
                    SelectedLevel = level;
                }
            }
            
            this.DialogResult = true;
            this.Close();
        }
    }
}