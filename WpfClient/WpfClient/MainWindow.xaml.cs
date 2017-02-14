using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using XData.Windows.ViewModels;

namespace XData.Windows.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
           
            Window loginWindow = new LoginWindow();
            ViewModel loginViewModel = new LoginViewModel();
            loginViewModel.View = loginWindow;
            loginWindow.DataContext = loginViewModel;
            if (loginWindow.ShowDialog() != true)
            {
                Application.Current.Shutdown();
                return;
            }

            ViewModel viewModel = new MainViewModel();
            this.DataContext = viewModel;
        }

    }
}
