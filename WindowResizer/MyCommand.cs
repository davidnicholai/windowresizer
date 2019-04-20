using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WindowResizer
{
    public class MyCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private Action _action;
        private bool _canExecute;

        public MyCommand(Action action)
        {
            _action = action;
        }

        public MyCommand(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action?.Invoke();
        }
    }
}
