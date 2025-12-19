using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting;

namespace TetrisAvalonia
{
    public partial class MainWindow : Window
    {
        private readonly TetrisGame _game;
        private readonly DispatcherTimer _timer;
        private bool _running = false;
        private int _totalLinesCleared = 0;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _game = new TetrisGame();

            // Таймер для игрового цикла
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += Timer_Tick;

            // Обработка клавиатуры
            this.KeyDown += MainWindow_KeyDown;

            // Загружаем лучший рекорд для меню
            UpdateTopScore();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var needRender = _game.Update();
            if (needRender)
            {
                RenderAll();
                UpdateGameUI();
            }
        }

        private void UpdateGameUI()
        {
            TxtScore.Text = _game.Score.ToString("N0");

            // Обновляем состояние игры
            TxtGameState.Text = _running ? "PLAYING" : "PAUSED";

            // Уровень (основан на количестве очищенных линий)
            int level = (_totalLinesCleared / 10) + 1;
            TxtLevel.Text = level.ToString();

            // Количество линий
            TxtLines.Text = _totalLinesCleared.ToString();

            // Следующая фигура
            RenderNext();

            // Обновляем таблицу рекордов
            UpdateHighScoresList();
        }

        private void UpdateHighScoresList()
        {
            HighScoresList.Items.Clear();

            foreach (var score in _game.HighScores.OrderByDescending(h => h.Score).Take(5))
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 0, 0, 5),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                var nameText = new TextBlock
                {
                    Text = score.Name,
                    Foreground = Brushes.White,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var scoreText = new TextBlock
                {
                    Text = score.Score.ToString("N0"),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                Grid.SetColumn(scoreText, 1);

                grid.Children.Add(nameText);
                grid.Children.Add(scoreText);
                border.Child = grid;

                HighScoresList.Items.Add(border);
            }
        }

        private void UpdateTopScore()
        {
            if (_game.HighScores.Any())
            {
                var topScore = _game.HighScores.OrderByDescending(h => h.Score).First();
                TxtTopScore.Text = topScore.Score.ToString("N0");
            }
        }

        private void RenderNext()
        {
            NextCanvas.Children.Clear();
            var block = _game.NextPiece;
            if (block == null) return;

            int cell = 25;
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    var v = block[y, x];
                    if (v != 0)
                    {
                        var rect = new Avalonia.Controls.Shapes.Rectangle
                        {
                            Width = cell - 2,
                            Height = cell - 2,
                            Fill = new SolidColorBrush(_game.ColorFor(v)),
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(rect, x * cell + 10);
                        Canvas.SetTop(rect, y * cell + 10);
                        NextCanvas.Children.Add(rect);
                    }
                }
        }

        private void RenderAll()
        {
            PlayfieldCanvas.Children.Clear();
            int cell = TetrisGame.CellSize;
            var grid = _game.Grid;

            // Сетка игрового поля
            for (int y = 0; y < TetrisGame.Height; y++)
                for (int x = 0; x < TetrisGame.Width; x++)
                {
                    // Ячейка сетки
                    var border = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = cell,
                        Height = cell,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 0.5,
                        Fill = new SolidColorBrush(Color.FromRgb(20, 20, 20))
                    };
                    Canvas.SetLeft(border, x * cell);
                    Canvas.SetTop(border, y * cell);
                    PlayfieldCanvas.Children.Add(border);

                    // Блок (если есть)
                    var v = grid[y, x];
                    if (v != 0)
                    {
                        var rect = new Avalonia.Controls.Shapes.Rectangle
                        {
                            Width = cell - 2,
                            Height = cell - 2,
                            Fill = new SolidColorBrush(_game.ColorFor(v)),
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(rect, x * cell + 1);
                        Canvas.SetTop(rect, y * cell + 1);
                        PlayfieldCanvas.Children.Add(rect);
                    }
                }

            // Текущая фигура
            var cur = _game.CurrentPiece;
            if (cur != null)
            {
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                    {
                        var v = cur[y, x];
                        if (v != 0)
                        {
                            var rect = new Avalonia.Controls.Shapes.Rectangle
                            {
                                Width = cell - 2,
                                Height = cell - 2,
                                Fill = new SolidColorBrush(_game.ColorFor(v)),
                                Stroke = Brushes.Black,
                                StrokeThickness = 1
                            };
                            Canvas.SetLeft(rect, (_game.CurrentX + x) * cell + 1);
                            Canvas.SetTop(rect, (_game.CurrentY + y) * cell + 1);
                            PlayfieldCanvas.Children.Add(rect);
                        }
                    }
            }
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_running) return;

            switch (e.Key)
            {
                case Key.Left:
                    _game.TryMove(-1, 0);
                    RenderAll();
                    break;
                case Key.Right:
                    _game.TryMove(1, 0);
                    RenderAll();
                    break;
                case Key.Up:
                    _game.TryRotate();
                    RenderAll();
                    break;
                case Key.Down:
                    _game.TryMove(0, 1);
                    RenderAll();
                    break;
                case Key.Space:
                    _game.HardDrop();
                    RenderAll();
                    break;
            }

            UpdateGameUI();
        }

        // === ОБРАБОТЧИКИ МЕНЮ ===

        private void BtnStartGame_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Переключаемся на экран игры
            MenuScreen.IsVisible = false;
            GameScreen.IsVisible = true;
            StartGame();
        }

        private void BtnHighScoresMenu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Обновляем отображение рекордов
            UpdateTopScore();

            // Показываем диалог с рекордами
            ShowHighScoresDialog();
        }

        private void ShowHighScoresDialog()
        {
            var dialog = new Window
            {
                Title = "High Scores",
                Width = 300,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var title = new TextBlock
            {
                Text = "HIGH SCORES",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(title);

            // Добавляем рекорды
            int rank = 1;
            foreach (var score in _game.HighScores.OrderByDescending(h => h.Score).Take(10))
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 0, 0, 5),
                    Padding = new Thickness(10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                var rankText = new TextBlock
                {
                    Text = $"{rank}.",
                    Foreground = Brushes.Gray,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var nameText = new TextBlock
                {
                    Text = score.Name,
                    Foreground = Brushes.White,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var scoreText = new TextBlock
                {
                    Text = score.Score.ToString("N0"),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                Grid.SetColumn(nameText, 1);
                Grid.SetColumn(scoreText, 2);

                grid.Children.Add(rankText);
                grid.Children.Add(nameText);
                grid.Children.Add(scoreText);
                border.Child = grid;

                stackPanel.Children.Add(border);
                rank++;
            }

            dialog.Content = stackPanel;
            dialog.ShowDialog(this);
        }

        private void BtnExit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private void BtnBackToMenu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            PauseGame();
            GameScreen.IsVisible = false;
            MenuScreen.IsVisible = true;
            UpdateTopScore();
        }

        // === ОБРАБОТЧИКИ ИГРЫ ===

        private void BtnStart_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            StartGame();
        }

        private void BtnPause_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_running)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }

        private void StartGame()
        {
            _game.Reset();
            _totalLinesCleared = 0;
            _running = true;
            _timer.Start();

            TxtGameState.Text = "PLAYING";
            TxtGameState.Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 85));

            RenderAll();
            UpdateGameUI();

            // Фокус на окно для клавиатуры
            this.Focus();
        }

        private void PauseGame()
        {
            _timer.Stop();
            _running = false;

            TxtGameState.Text = "PAUSED";
            TxtGameState.Foreground = new SolidColorBrush(Color.FromRgb(255, 85, 85));

            UpdateGameUI();
        }

        private void ResumeGame()
        {
            _timer.Start();
            _running = true;

            TxtGameState.Text = "PLAYING";
            TxtGameState.Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 85));

            UpdateGameUI();
            this.Focus();
        }
    }
}