using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TetrisAvalonia
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly TetrisGame _game;

        private string _gameState = "READY";
        private SolidColorBrush _gameStateColor = new SolidColorBrush(Color.FromRgb(85, 255, 85));
        private string _playerName = "Player";
        private int _score;
        private int _level = 1;
        private int _lines;
        private int _topScore;
        private bool _isGameScreenVisible;
        private bool _isMenuScreenVisible = true;
        private ObservableCollection<HighScore> _highScores = new ObservableCollection<HighScore>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public string GameState
        {
            get => _gameState;
            set => SetField(ref _gameState, value);
        }

        public SolidColorBrush GameStateColor
        {
            get => _gameStateColor;
            set => SetField(ref _gameStateColor, value);
        }

        public string PlayerName
        {
            get => _playerName;
            set => SetField(ref _playerName, value);
        }

        public int Score
        {
            get => _score;
            set => SetField(ref _score, value);
        }

        public int Level
        {
            get => _level;
            set => SetField(ref _level, value);
        }

        public int Lines
        {
            get => _lines;
            set => SetField(ref _lines, value);
        }

        public int TopScore
        {
            get => _topScore;
            set => SetField(ref _topScore, value);
        }

        public bool IsGameScreenVisible
        {
            get => _isGameScreenVisible;
            set => SetField(ref _isGameScreenVisible, value);
        }

        public bool IsMenuScreenVisible
        {
            get => _isMenuScreenVisible;
            set => SetField(ref _isMenuScreenVisible, value);
        }

        public ObservableCollection<HighScore> HighScores
        {
            get => _highScores;
            set => SetField(ref _highScores, value);
        }

        public TetrisGame Game => _game;

        public GameViewModel()
        {
            _game = new TetrisGame();
            SetupGameEventHandlers();
        }

        private void SetupGameEventHandlers()
        {
            _game.GameOver += OnGameOver;
            _game.ScoreChanged += OnScoreChanged;
            _game.LinesClearedChanged += OnLinesClearedChanged;
            _game.LevelChanged += OnLevelChanged;
            _game.GameStateChanged += OnGameStateChanged;
            _game.PropertyChanged += OnGamePropertyChanged;
        }

        private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TetrisGame.HighScores))
            {
                UpdateTopScore();
                UpdateHighScoresList();
            }
        }

        private void OnGameStateChanged(bool isGameOver)
        {
            if (isGameOver)
            {
                GameState = "GAME OVER";
                GameStateColor = new SolidColorBrush(Color.FromRgb(255, 50, 50));
            }
        }

        private void OnGameOver()
        {
            // Игра окончена - логика будет в MainWindow
        }

        private void OnScoreChanged()
        {
            Score = _game.Score;
        }

        private void OnLinesClearedChanged()
        {
            Lines = _game.LinesCleared;
        }

        private void OnLevelChanged()
        {
            Level = _game.Level;
        }

        public void UpdateTopScore()
        {
            if (_game.HighScores.Any())
            {
                var topScore = _game.HighScores.OrderByDescending(h => h.Score).First();
                TopScore = topScore.Score;
            }
        }

        public void UpdateHighScoresList()
        {
            HighScores = new ObservableCollection<HighScore>(
                _game.HighScores.OrderByDescending(h => h.Score).Take(5));
        }

        public void SetGameStatePlaying()
        {
            GameState = "PLAYING";
            GameStateColor = new SolidColorBrush(Color.FromRgb(85, 255, 85));
        }

        public void SetGameStatePaused()
        {
            GameState = "PAUSED";
            GameStateColor = new SolidColorBrush(Color.FromRgb(255, 85, 85));
        }

        public void SetGameStateReady()
        {
            GameState = "READY";
            GameStateColor = new SolidColorBrush(Color.FromRgb(85, 255, 85));
        }

        public void SwitchToGameScreen()
        {
            IsMenuScreenVisible = false;
            IsGameScreenVisible = true;
        }

        public void SwitchToMenuScreen()
        {
            IsMenuScreenVisible = true;
            IsGameScreenVisible = false;
            UpdateTopScore();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}