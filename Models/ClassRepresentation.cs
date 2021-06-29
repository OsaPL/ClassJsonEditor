using System;
using ClassJsonEditor.ViewModels;
using ReactiveUI;

namespace ClassJsonEditor.Models
{
    public class ClassRepresentation: ViewModelBase 
    {
        private Type _type;
        private bool _isReflectionOnly;
        private bool _failedLoading;

        public ClassRepresentation(Type type, bool isReflectionOnly=false, bool failedLoading=false) 
        {
            IsReflectionOnly = isReflectionOnly;
            Type = type;
            FailedLoading = failedLoading;
        }

        public Type Type
        {
            get => _type;
            set => _type = value;
        }

        public bool IsReflectionOnly
        {
            get => _isReflectionOnly;
            set => this.RaiseAndSetIfChanged(ref _isReflectionOnly, value);
        }

        public bool FailedLoading
        {
            get => _failedLoading;
            set => _failedLoading = value;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}