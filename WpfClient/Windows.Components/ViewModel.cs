using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XData.Windows.Components;

namespace XData.Windows.ViewModels
{
    public abstract class ViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public FrameworkElement View
        {
            get;
            set;
        }

        protected void ShowMessage(string message)
        {
            ShowBox.Show(message, View);
        }

        protected void ShowException(Exception exception)
        {
            ShowBox.Show(exception, View);
        }

        public void Dispose()
        {
            if (View != null) // as unmanaged object
            {
                View = null;
                GC.SuppressFinalize(this);
            }
        }

        ~ViewModel()
        {
            Dispose();
        }


    }
}
