using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TetrisAvalonia
{
    // Класс для хранения рекордов
    public class HighScore
    {
        public string Name { get; set; } = "Player";
        public int Score { get; set; }
    }

    // Основной класс игры Тетрис
    public class TetrisGame : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // События для уведомления об изменениях состояния
        public event Action<bool>? GameStateChanged;
        public event Action? GameOver;
        public event Action? ScoreChanged;
        public event Action? LinesClearedChanged;
        public event Action? LevelChanged;
        public event Action? GameReset;

        public const int Width = 10;
        public const int Height = 20;
        public const int CellSize = 30;

        private readonly int[,] _grid = new int[Height, Width];
        private readonly Random _rnd = new Random();

        private const string ServerUrl = "https://qkun8i-217-71-131-234.ru.tuna.am/api/scores";
        private static readonly HttpClient client = new HttpClient();

        // Фигуры: 7 тетромино в формате 4x4
        private static readonly int[,,] SHAPES = new int[7, 4, 4]
        {
            // I
            {{0,0,0,0},{1,1,1,1},{0,0,0,0},{0,0,0,0}},
            // J
            {{2,0,0,0},{2,2,2,0},{0,0,0,0},{0,0,0,0}},
            // L
            {{0,0,3,0},{3,3,3,0},{0,0,0,0},{0,0,0,0}},
            // O
            {{0,4,4,0},{0,4,4,0},{0,0,0,0},{0,0,0,0}},
            // S
            {{0,5,5,0},{5,5,0,0},{0,0,0,0},{0,0,0,0}},
            // T
            {{0,6,0,0},{6,6,6,0},{0,0,0,0},{0,0,0,0}},
            // Z
            {{7,7,0,0},{0,7,7,0},{0,0,0,0},{0,0,0,0}},
        };

        private readonly (byte r, byte g, byte b)[] COLORS = new (byte, byte, byte)[]
        {
            (17,17,17), // 0 фон
            (0,255,255), // 1 I - бирюзовый
            (0,0,255),   // 2 J - синий
            (255,128,0), // 3 L - оранжевый
            (255,255,0), // 4 O - желтый
            (0,255,0),   // 5 S - зеленый
            (128,0,128), // 6 T - фиолетовый
            (255,0,0),   // 7 Z - красный
        };

        private int _score = 0;
        private int _linesCleared = 0;
        private bool _isGameOver = false;
        private double _fallInterval = 0.5;
        private List<HighScore> _highScores = new List<HighScore>();

        public int[,] Grid => _grid;

        // Текущая и следующая фигуры
        private int[,] _current = new int[4, 4];
        private int[,] _next = new int[4, 4];
        public int[,]? CurrentPiece => _current;
        public int[,]? NextPiece => _next;

        public int CurrentX { get; private set; }
        public int CurrentY { get; private set; }
        public int CurrentColor { get; private set; } = 1;

        private double _accumulator = 0.0;

        public int Score
        {
            get => _score;
            private set
            {
                if (_score != value)
                {
                    _score = value;
                    OnPropertyChanged();
                    ScoreChanged?.Invoke();
                }
            }
        }

        public int LinesCleared
        {
            get => _linesCleared;
            private set
            {
                if (_linesCleared != value)
                {
                    _linesCleared = value;
                    OnPropertyChanged();
                    LinesClearedChanged?.Invoke();

                    // При изменении линий также может измениться уровень
                    LevelChanged?.Invoke();
                }
            }
        }

        public int Level => (LinesCleared / 10) + 1;

        public string PlayerName { get; set; } = "Player";

        public bool IsGameOver
        {
            get => _isGameOver;
            private set
            {
                if (_isGameOver != value)
                {
                    _isGameOver = value;
                    OnPropertyChanged();
                    GameStateChanged?.Invoke(value);

                    if (value)
                    {
                        GameOver?.Invoke();
                    }
                }
            }
        }

        public List<HighScore> HighScores
        {
            get => _highScores;
            set
            {
                _highScores = value;
                OnPropertyChanged();
            }
        }

        public TetrisGame()
        {
            Reset();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task LoadHighScoresAsync()
        {
            try
            {
                var json = await client.GetStringAsync(ServerUrl);
                var scores = JsonSerializer.Deserialize<List<HighScore>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (scores != null)
                {
                    HighScores = scores;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка сети: " + ex.Message);
            }
        }

        public async Task AddHighScoreAsync(string name, int score)
        {
            var newRecord = new HighScore { Name = name, Score = score };
            HighScores.Add(newRecord);
            HighScores = HighScores.OrderByDescending(h => h.Score).Take(10).ToList();
            OnPropertyChanged(nameof(HighScores));

            try
            {
                var json = JsonSerializer.Serialize(newRecord);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(ServerUrl, content);
                await LoadHighScoresAsync();
            }
            catch { }
        }

        // Сброс игры
        public void Reset()
        {
            Array.Clear(_grid, 0, _grid.Length);
            SpawnNextPiece();
            SpawnNewPiece();
            Score = 0;
            LinesCleared = 0;
            _accumulator = 0.0;
            _fallInterval = 0.5;
            IsGameOver = false;

            GameReset?.Invoke();
        }

        private void SpawnNextPiece()
        {
            int idx = _rnd.Next(0, 7);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    _next[y, x] = SHAPES[idx, y, x];
        }

        private void SpawnNewPiece()
        {
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    _current[y, x] = _next[y, x];

            SpawnNextPiece();
            CurrentX = Width / 2 - 2;
            CurrentY = 0;

            if (CheckCollision(CurrentX, CurrentY, _current))
            {
                IsGameOver = true;
            }
        }

        public bool Update()
        {
            if (IsGameOver) return false;

            _accumulator += 0.03;
            bool rendered = false;
            if (_accumulator >= _fallInterval)
            {
                _accumulator = 0.0;
                if (!TryMove(0, 1))
                {
                    MergeCurrent();
                    ClearLines();
                    SpawnNewPiece();
                }
                rendered = true;
            }
            return rendered;
        }

        public bool TryMove(int dx, int dy)
        {
            if (!CheckCollision(CurrentX + dx, CurrentY + dy, _current))
            {
                CurrentX += dx;
                CurrentY += dy;
                return true;
            }
            return false;
        }

        public void TryRotate()
        {
            int[,] rotated = new int[4, 4];
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    rotated[x, 3 - y] = _current[y, x];

            if (!CheckCollision(CurrentX, CurrentY, rotated))
            {
                _current = rotated;
            }
        }

        public void HardDrop()
        {
            while (TryMove(0, 1)) { }
            MergeCurrent();
            ClearLines();
            SpawnNewPiece();
        }

        private void MergeCurrent()
        {
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    if (_current[y, x] != 0)
                    {
                        int gx = CurrentX + x;
                        int gy = CurrentY + y;
                        if (gy >= 0 && gy < Height && gx >= 0 && gx < Width)
                            _grid[gy, gx] = _current[y, x];
                    }
                }
        }

        private void ClearLines()
        {
            int cleared = 0;
            for (int y = Height - 1; y >= 0; y--)
            {
                bool full = true;
                for (int x = 0; x < Width; x++)
                    if (_grid[y, x] == 0)
                    {
                        full = false;
                        break;
                    }

                if (full)
                {
                    cleared++;
                    for (int ty = y; ty > 0; ty--)
                        for (int x = 0; x < Width; x++)
                            _grid[ty, x] = _grid[ty - 1, x];

                    for (int x = 0; x < Width; x++)
                        _grid[0, x] = 0;

                    y++;
                }
            }

            if (cleared > 0)
            {
                int points = cleared switch
                {
                    1 => 100,
                    2 => 300,
                    3 => 500,
                    4 => 800,
                    _ => 0
                };

                Score += points;
                LinesCleared += cleared;
                _fallInterval = Math.Max(0.05, _fallInterval - cleared * 0.02);
            }
        }

        private bool CheckCollision(int px, int py, int[,] piece)
        {
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    if (piece[y, x] == 0) continue;
                    int gx = px + x;
                    int gy = py + y;
                    if (gx < 0 || gx >= Width || gy >= Height) return true;
                    if (gy >= 0 && _grid[gy, gx] != 0) return true;
                }
            return false;
        }

        public Avalonia.Media.Color ColorFor(int value)
        {
            if (value < 0 || value >= COLORS.Length)
                return Color.FromRgb(17, 17, 17);

            var c = COLORS[value];
            return Color.FromRgb(c.r, c.g, c.b);
        }
    }
}