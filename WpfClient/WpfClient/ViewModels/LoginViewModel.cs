using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XData.Client.Services;
using XData.Windows.Components;
using XData.Windows.Models;

namespace XData.Windows.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged("Password");
                }
            }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get { return _dialogResult; }
            protected set
            {
                _dialogResult = value;
                OnPropertyChanged("DialogResult");
            }
        }

        public ICommand LoginCommand
        {
            get;
            protected set;
        }

        public ICommand ExitCommand
        {
            get;
            protected set;
        }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand((_) =>
            {
                if (string.IsNullOrWhiteSpace(UserName))
                {
                    ShowMessage("The user name is required and cannot be empty");
                    return;
                }
                if (string.IsNullOrWhiteSpace(Password))
                {
                    ShowMessage("The password is required and cannot be empty");
                    return;
                }

                ValidationStatus result = ClientSecurity.Login(UserName, Password);
                switch (result)
                {
                    case ValidationStatus.Success:
                        DialogResult = true;
                        break;
                    case ValidationStatus.Invalidation:
                        ShowMessage("The user name or password is incorrect");
                        break;
                    case ValidationStatus.Disabled:
                        ShowMessage("Your account has been disabled, Please contact the administrator");
                        break;
                    case ValidationStatus.LockedOut:
                        ShowMessage("Your account has been locked, Please contact the administrator");
                        break;
                    default:
                        break;
                }               
            });

            ExitCommand = new RelayCommand((_) =>
            {
                DialogResult = false;
            });
        }


    }
}
