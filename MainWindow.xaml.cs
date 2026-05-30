using System.Windows;
using System.Windows.Controls;
using NodeTracker.ViewModels;

namespace NodeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && sender is PasswordBox pb)
            {
                vm.LoginPassword = pb.Password;
            }
        }
    }
}