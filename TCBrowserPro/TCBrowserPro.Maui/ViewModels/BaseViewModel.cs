using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SignInl.Maui.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject, IDataErrorInfo
    {
        private Dictionary<string, string> _errors = new Dictionary<string, string>();
        private bool isValid = false;

        protected Dictionary<string, string> Errors
        {
            get => _errors;
            private set => SetProperty(ref _errors, value);
        }

        protected bool IsValid
        {
            get => isValid;
            private set => SetProperty(ref isValid, value);
        }

        [RelayCommand]
        protected void Validate()
        {
            var context = new ValidationContext(this);
            var errors = new List<ValidationResult>();

            isValid = Validator.TryValidateObject(this, context, errors, true);
            foreach (var error in errors)
            {
                var columnName = error.MemberNames.First();

                if (!_errors.ContainsKey(columnName))
                    _errors.Add(columnName, error.ErrorMessage);
            }
        }


        // check for general model error
        public string Error { get; private set; } = null;
        public virtual bool HasError
        {
            get { return Errors.Any(); }
        }

        // check for property errors
        public string this[string columnName]
        {
            get
            {
                Validate();
                return _errors[columnName];
            }
        }
    }
}

