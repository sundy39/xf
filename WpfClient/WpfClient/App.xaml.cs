using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using XData.Windows.Components;
using XData.Windows.Models;

namespace XData.Windows.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            ShowException(ex);
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            ClientSecurity.Logout();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            ShowException(ex);

            e.Handled = true;
        }

        private void ShowException(Exception exception)
        {
            ShowBox.Show(exception, this.MainWindow);
        }


    }
}
