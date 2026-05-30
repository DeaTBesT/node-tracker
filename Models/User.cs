using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NodeTracker.Models
{
    public class User : INotifyPropertyChanged
    {
        private int _id;
        private string _username = string.Empty;
        private string _passwordHash = string.Empty;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string PasswordHash
        {
            get => _passwordHash;
            set => SetProperty(ref _passwordHash, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
