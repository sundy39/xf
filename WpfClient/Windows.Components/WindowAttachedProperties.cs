using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XData.Windows.Components
{
    public static class WindowAttachedProperties
    {
        public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached("DialogResult",
            typeof(Boolean?), typeof(WindowAttachedProperties), new PropertyMetadata(OnDialogResultPropertyChanged));

        public static void SetDialogResult(Window window, Boolean? dialogResult)
        {
            window.SetValue(DialogResultProperty, dialogResult);
        }

        public static Boolean? GetDialogResult(Window window)
        {
            return window.GetValue(DialogResultProperty) as Boolean?;
        }

        private static void OnDialogResultPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window != null) window.DialogResult = e.NewValue as bool?;
        }


    }
}
