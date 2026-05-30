using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NodeTracker.Models
{
    public class Note : INotifyPropertyChanged
    {
        private int _id;
        private string _title = string.Empty;
        private string _content = string.Empty;
        private string _tags = string.Empty;
        private DateTime _createdDate = DateTime.Now;
        private bool _isFavorite;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        private int _userId;
        public int UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
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

        public Note Clone()
        {
            return new Note
            {
                Id = Id,
                Title = Title,
                Content = Content,
                Tags = Tags,
                CreatedDate = CreatedDate,
                IsFavorite = IsFavorite
            };
        }
    }
}
