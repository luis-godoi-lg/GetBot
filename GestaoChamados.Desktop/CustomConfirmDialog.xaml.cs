using System.Windows;

namespace GestaoChamados.Desktop
{
    public partial class CustomConfirmDialog : Window
    {
        public bool Result { get; private set; }

        public CustomConfirmDialog(string message, string title = "Confirmar")
        {
            InitializeComponent();
            MessageText.Text = message;
            TitleText.Text = title;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        public static bool Show(string message, string title = "Confirmar")
        {
            var dialog = new CustomConfirmDialog(message, title);
            var result = dialog.ShowDialog();
            return result == true;
        }
    }
}
