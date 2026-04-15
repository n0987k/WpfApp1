using System.Windows;
using WpfApp1.Models;
using WpfApp1.ViewModels;
using WpfApp1.Views;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(AuthenticatedUser user)
        {
            InitializeComponent();

            var vm = new ShellViewModel(user);
            DataContext = vm;
            vm.PartnersViewModel.LoadPartners();
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }
    }
}
