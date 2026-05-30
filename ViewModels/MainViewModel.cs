using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NodeTracker.Commands;
using NodeTracker.Models;
using NodeTracker.Services;

namespace NodeTracker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NoteService _noteService;
        private readonly AuthService _authService;
        private string _searchText = string.Empty;
        private bool _showFavoritesOnly;
        private Note _editorNote = new Note();
        private Note? _selectedNote;
        private string _statusMessage = "Готово";
        private User? _currentUser;
        private string _loginUsername = string.Empty;
        private string _loginPassword = string.Empty;

        public ObservableCollection<Note> Notes { get; } = new ObservableCollection<Note>();
        public ICollectionView NotesView { get; }

        public RelayCommand SaveCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand LoginCommand { get; }
        public RelayCommand RegisterCommand { get; }
        public RelayCommand LogoutCommand { get; }

        public MainViewModel()
        {
            var dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NodeTrackerDiary", "notes.db");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath) ?? string.Empty);
            _noteService = new NoteService(dbPath);
            _authService = new AuthService(dbPath);

            NotesView = CollectionViewSource.GetDefaultView(Notes);
            NotesView.Filter = FilterNotes;

            SaveCommand = new RelayCommand(async _ => await SaveNoteAsync(), _ => IsAuthenticated && !string.IsNullOrWhiteSpace(EditorNote.Title));
            NewCommand = new RelayCommand(_ => CreateNewNote());
            DeleteCommand = new RelayCommand(async _ => await DeleteNoteAsync(), _ => IsAuthenticated && (SelectedNote != null || EditorNote.Id != 0));
            LoginCommand = new RelayCommand(async _ => await LoginAsync());
            RegisterCommand = new RelayCommand(async _ => await RegisterAsync());
            LogoutCommand = new RelayCommand(_ => Logout(), _ => IsAuthenticated);

            EditorNote = new Note { CreatedDate = DateTime.Now };
            LoadNotesAsync();
        }

        public Note EditorNote
        {
            get => _editorNote;
            set
            {
                if (_editorNote == value) return;
                UnsubscribeEditorNote(_editorNote);
                _editorNote = value;
                SubscribeEditorNote(_editorNote);
                OnPropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public Note? SelectedNote
        {
            get => _selectedNote;
            set
            {
                if (_selectedNote == value) return;
                _selectedNote = value;
                OnPropertyChanged();

                if (_selectedNote != null)
                {
                    EditorNote = _selectedNote.Clone();
                    StatusMessage = $"Редактируется: {_selectedNote.Title}";
                }
                else
                {
                    EditorNote = new Note { CreatedDate = DateTime.Now };
                    StatusMessage = "Новая заметка";
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                NotesView.Refresh();
                UpdateStatus();
            }
        }

        public bool ShowFavoritesOnly
        {
            get => _showFavoritesOnly;
            set
            {
                if (_showFavoritesOnly == value) return;
                _showFavoritesOnly = value;
                OnPropertyChanged();
                NotesView.Refresh();
                UpdateStatus();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser == value) return;
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAuthenticated));
                SaveCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                LogoutCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsAuthenticated => CurrentUser != null;

        public string LoginUsername
        {
            get => _loginUsername;
            set { _loginUsername = value; OnPropertyChanged(); }
        }

        public string LoginPassword
        {
            get => _loginPassword;
            set { _loginPassword = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async void LoadNotesAsync()
        {
            try
            {
                List<Note> notes;
                if (CurrentUser != null)
                    notes = await _noteService.GetNotesAsync(CurrentUser.Id);
                else
                    notes = new List<Note>();
                Notes.Clear();
                foreach (var note in notes)
                    Notes.Add(note);

                NotesView.Refresh();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заметок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveNoteAsync()
        {
            if (EditorNote == null)
                return;

            if (string.IsNullOrWhiteSpace(EditorNote.Title))
            {
                MessageBox.Show("Заголовок не может быть пустым.", "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentUser == null)
            {
                MessageBox.Show("Сначала войдите в аккаунт.", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var savedNote = await _noteService.AddOrUpdateNoteAsync(EditorNote, CurrentUser.Id);
            LoadNotesAsync();
            SelectedNote = savedNote;
            StatusMessage = "Заметка сохранена";
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
            {
                MessageBox.Show("Введите имя пользователя и пароль.", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = await _authService.LoginAsync(LoginUsername.Trim(), LoginPassword);
            if (user == null)
            {
                StatusMessage = "Ошибка входа: неверные учетные данные";
                return;
            }

            CurrentUser = user;
            StatusMessage = $"Вход выполнен: {user.Username}";
            LoadNotesAsync();
        }

        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
            {
                MessageBox.Show("Введите имя пользователя и пароль.", "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = await _authService.RegisterAsync(LoginUsername.Trim(), LoginPassword);
            if (user == null)
            {
                StatusMessage = "Имя пользователя занято";
                return;
            }

            CurrentUser = user;
            StatusMessage = $"Аккаунт создан: {user.Username}";
            LoadNotesAsync();
        }

        private async Task DeleteNoteAsync()
        {
            var noteToDelete = SelectedNote ?? (EditorNote.Id != 0 ? EditorNote : null);
            if (noteToDelete == null)
                return;

            var result = MessageBox.Show("Вы уверены, что хотите удалить заметку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            await _noteService.DeleteNoteAsync(noteToDelete);
            CreateNewNote();
            LoadNotesAsync();
            StatusMessage = "Заметка удалена";
        }

        private void CreateNewNote()
        {
            SelectedNote = null;
            EditorNote = new Note { CreatedDate = DateTime.Now };
            StatusMessage = "Создайте новую заметку";
        }

        private bool FilterNotes(object obj)
        {
            if (obj is not Note note)
                return false;

            if (ShowFavoritesOnly && !note.IsFavorite)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var lowerQuery = SearchText.Trim().ToLower();
            return note.Title.ToLower().Contains(lowerQuery)
                || note.Content.ToLower().Contains(lowerQuery);
        }

        private void UpdateStatus()
        {
            StatusMessage = $"Заметок: {Notes.Count} | Фильтр: {(ShowFavoritesOnly ? "избранные" : "все")}";
        }

        private void SubscribeEditorNote(Note note)
        {
            note.PropertyChanged += EditorNote_PropertyChanged;
        }

        private void UnsubscribeEditorNote(Note note)
        {
            note.PropertyChanged -= EditorNote_PropertyChanged;
        }

        private void EditorNote_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Note.Title) || e.PropertyName == nameof(Note.Id))
            {
                SaveCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        private void Logout()
        {
            CurrentUser = null;
            Notes.Clear();
            SelectedNote = null;
            EditorNote = new Note { CreatedDate = DateTime.Now };
            LoginUsername = string.Empty;
            LoginPassword = string.Empty;
            StatusMessage = "Вы вышли из аккаунта";
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
