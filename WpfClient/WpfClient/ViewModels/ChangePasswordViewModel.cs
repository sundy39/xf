using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XData.Client.Services;
using XData.Windows.Components;
using XData.Windows.Models;

namespace XData.Windows.ViewModels
{
    public class ChangePasswordViewModel : ViewModel
    {
        private string _oldPassword;
        public string OldPassword
        {
            get { return _oldPassword; }
            set
            {
                if (_oldPassword != value)
                {
                    _oldPassword = value;
                    OnPropertyChanged("OldPassword");
                }
            }
        }

        private string _newPassword;
        public string NewPassword
        {
            get { return _newPassword; }
            set
            {
                if (_newPassword != value)
                {
                    _newPassword = value;
                    OnPropertyChanged("NewPassword");
                }
            }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set
            {
                if (_confirmPassword != value)
                {
                    _confirmPassword = value;
                    OnPropertyChanged("ConfirmPassword");
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

        public ICommand ChangeCommand
        {
            get;
            protected set;
        }

        public ICommand CancelCommand
        {
            get;
            protected set;
        }

        public ChangePasswordViewModel()
        {
            ChangeCommand = new RelayCommand((_) =>
            {
                if (string.IsNullOrWhiteSpace(OldPassword))
                {
                    ShowMessage("The current password is required and cannot be empty");
                    return;
                }
                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    ShowMessage("The new password is required and cannot be empty");
                    return;
                }
                if (string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    ShowMessage("The confirm password is required and cannot be empty");
                    return;
                }
                if (NewPassword.Length < 6 || NewPassword.Length > 20)
                {
                    ShowMessage("The new password must be at least 6 and not more than 20 characters long");
                    return;
                }
                if (NewPassword != ConfirmPassword)
                {
                    ShowMessage("The new password and its confirm are not the same");
                    return;
                }
                try
                {
                    object result = ClientSecurity.ChangePassword(OldPassword, NewPassword, ConfirmPassword);
                    if (result.GetType() == typeof(Dictionary<PasswordValidationStatuses, object>))
                    {
                        StringBuilder sb = new StringBuilder(); 
                        Dictionary<PasswordValidationStatuses, object> dict = result as Dictionary<PasswordValidationStatuses, object>;
                        if (dict.ContainsKey(PasswordValidationStatuses.RequireDigit))
                        {
                            sb.AppendLine("The password requires a numeric digit ('0' - '9').");
                        }
                        if (dict.ContainsKey(PasswordValidationStatuses.RequireLowercase))
                        {
                            sb.AppendLine("The password requires a lower case letter ('a' - 'z').");
                        }
                        if (dict.ContainsKey(PasswordValidationStatuses.RequireUppercase))
                        {
                            sb.AppendLine("The password requires an upper case letter ('A' - 'Z').");
                        }
                        if (dict.ContainsKey(PasswordValidationStatuses.RequireNonLetterOrDigit))
                        {
                            sb.AppendLine("The password requires a non-letter or digit character.");
                        }
                        if (dict.ContainsKey(PasswordValidationStatuses.RequiredLength))
                        {
                            sb.AppendLine(string.Format("The password must be at least {0} characters long.", dict[PasswordValidationStatuses.RequiredLength]));
                        }
                        if (sb.Length > 0)
                        {
                            ShowMessage(sb.ToString());
                            return;
                        }
                    }
                    if (result.GetType() == typeof(ValidationStatus))
                    {
                        ValidationStatus status = (ValidationStatus)result;
                        switch (status)
                        {
                            case ValidationStatus.Success:
                                DialogResult = true;
                                break;
                            case ValidationStatus.Invalidation:
                                ShowMessage("The current password is incorrect");
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
                    }
                }
                catch (Exception e)
                {
                    ShowException(e);
                    return;
                }
            });

            CancelCommand = new RelayCommand((_) =>
            {
                DialogResult = false;
            });
        }


    }
}
