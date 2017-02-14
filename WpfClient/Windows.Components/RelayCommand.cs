using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Windows.Components
{
    public class RelayCommand : RelayCommand_
    {
        public RelayCommand(Action<object> execute)
            : base(execute)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
            :base(execute, canExecute)
        {    
        }
    }
}
