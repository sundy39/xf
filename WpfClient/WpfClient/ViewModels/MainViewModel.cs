using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using XData.Windows.Client;
using XData.Windows.Components;
using XData.Windows.Models;

namespace XData.Windows.ViewModels
{
    public class MainViewModel : ViewModel
    {
        public string Name
        {
            get
            {
                return ClientSecurity.User.Element("Employee.Name").Value;
            }
        }

        public ICommand ChangePasswordCommand
        {
            get;
            protected set;
        }

        public ICommand ExitCommand
        {
            get;
            protected set;
        }

        public MainViewModel()
        {
            ChangePasswordCommand = new RelayCommand((_) =>
            {
                Window window = new ChangePasswordWindow();
                ViewModel viewModel = new ChangePasswordViewModel();
                viewModel.View = window;
                window.DataContext = viewModel;
                window.ShowDialog();
            });

            ExitCommand = new RelayCommand((_) =>
            {
                Application.Current.Shutdown();
            });
        }
    }
}
