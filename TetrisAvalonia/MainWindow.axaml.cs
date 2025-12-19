using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TetrisAvalonia
{
    public class HighScore { public string Name { get; set; } = "Player"; public int Score { get; set; } }

    public class TetrisGame
    {
        public const int Width = 10;
        public const int Height = 20;
        public const int CellSize = 30;

        private readonly int[,] _grid = new int[Height, Width];
        private readonly Random _rnd = new Random();

        // Shapes: 7 tetrominoes 4x4
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
            (17,17,17), // 0 background
            (0,255,255), // 1
            (0,0,255),   // 2
            (255,128,0), // 3
            (255,255,0), // 4
            (0,255,0),   // 5
            (128,0,128), // 6
            (255,0,0),   // 7
        };

        public int[,] Grid => _grid;

        // Current and next
        private int[,] _current = new int[4, 4];
        private int[,] _next = new int[4, 4];
        public int[,]? CurrentPiece => _current;
        public int[,]? NextPiece => _next;

        public int CurrentX { get; private set; }
        public int CurrentY { get; private set; }
        public int CurrentColor { get; private set; } = 1;

        private double _fallInterval = 0.5; // seconds
        private double _accumulator = 0.0;

        public int Score { get; private set; } = 0;

        // High scores
        private const string HS_FILE = "highscores.json";
        public List<HighScore> HighScores { get; private set; } = new List<HighScore>();

        public TetrisGame()
        {
            LoadHighScores();
            Reset();
        }

        public void Reset()
        {
            Array.Clear(_grid, 0, _grid.Length);
            SpawnNewPiece();
            SpawnNextPiece();
            Score = 0;
            _accumulator = 0.0;
            _fallInterval = 0.5;
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
            // move next to current
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    _current[y, x] = _next[y, x];

            // new next
            SpawnNextPiece();

            CurrentX = Width / 2 - 2;
            CurrentY = 0;

            // if collision at spawn -> game over -> clear grid
            if (CheckCollision(CurrentX, CurrentY, _current))
            {
                // push to highscores maybe
                AddHighScore("Player", Score);
                Array.Clear(_grid, 0, _grid.Length);
                Score = 0;
            }
        }

        public bool Update()
        {
            // Called from timer frequently. We'll accumulate time and step when needed.
            _accumulator += 0.03; // match timer interval (30ms)
            bool rendered = false;
            if (_accumulator >= _fallInterval)
            {
                _accumulator = 0.0;
                // Try move down
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
                for (int x = 0; x < Width; x++) if (_grid[y, x] == 0) { full = false; break; }
                if (full)
                {
                    cleared++;
                    // move all above lines down
                    for (int ty = y; ty > 0; ty--)
                        for (int x = 0; x < Width; x++)
                            _grid[ty, x] = _grid[ty - 1, x];
                    for (int x = 0; x < Width; x++) _grid[0, x] = 0;
                    y++; // recheck same y
                }
            }
            if (cleared > 0)
            {
                Score += cleared * 100;
                // speed up slightly
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


        // High scores
        private void LoadHighScores()
        {
            try
            {
                if (File.Exists(HS_FILE))
                {
                    var json = File.ReadAllText(HS_FILE);
                    HighScores = JsonSerializer.Deserialize<List<HighScore>>(json) ?? new List<HighScore>();
                }
            }
            catch { HighScores = new List<HighScore>(); }
        }

        private void SaveHighScores()
        {
            try
            {
                var json = JsonSerializer.Serialize(HighScores);
                File.WriteAllText(HS_FILE, json);
            }
            catch { }
        }

        public void AddHighScore(string name, int score)
        {
            HighScores.Add(new HighScore { Name = name, Score = score });
            HighScores = HighScores.OrderByDescending(h => h.Score).Take(5).ToList();
            SaveHighScores();
        }
    }
}