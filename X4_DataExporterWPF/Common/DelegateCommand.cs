using System;
using System.Windows.Input;

namespace X4_DataExporterWPF.Common
{
    public class DelegateCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action ExecuteMethod;
        private readonly Func<bool> CanExecuteMethod;

        public DelegateCommand(Action executeMethod) : this(executeMethod, AlwaysTrue)
        {
            
        }

        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            ExecuteMethod = executeMethod;
            CanExecuteMethod = canExecuteMethod;
        }


        static private bool AlwaysTrue()
        {
            return true;
        }


        public bool CanExecute(object parameter)
        {
            return CanExecuteMethod();
        }

        public void Execute(object parameter)
        {
            ExecuteMethod();
        }
    }


    public class DelegateCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action<T> ExecuteMethod;
        private readonly Func<T, bool> CanExecuteMethod;

        public DelegateCommand(Action<T> executeMethod) : this(executeMethod, AlwaysTrue)
        {

        }

        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            ExecuteMethod = executeMethod;
            CanExecuteMethod = canExecuteMethod;
        }


        static private bool AlwaysTrue(T parameter)
        {
            return true;
        }


        public bool CanExecute(object parameter)
        {
            return CanExecuteMethod?.Invoke((T)parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            ExecuteMethod((T)parameter);
        }
    }
}
