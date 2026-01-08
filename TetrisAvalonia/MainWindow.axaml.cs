using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Linq;

namespace TetrisAvalonia
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private bool _running = false;
        private GameViewModel ViewModel => (GameViewModel)DataContext!;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            DataContext = new GameViewModel();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += Timer_Tick;

            this.KeyDown += MainWindow_KeyDown;

            ViewModel.Game.GameOver += OnGameOver;
            LoadScoresOnStart();
        }

        private async void LoadScoresOnStart()
        {
            await ViewModel.Game.LoadHighScoresAsync();
            ViewModel.UpdateTopScore();
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (ViewModel.IsGameScreenVisible)
            {
                this.Focus();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_running) return;

            var needRender = ViewModel.Game.Update();

            if (needRender)
            {
                RenderAll();
                RenderNext();
            }
        }

        private async void SaveScoreAndShowDialog()
        {
            await ViewModel.Game.AddHighScoreAsync(ViewModel.PlayerName, ViewModel.Game.Score);
            ShowGameOverDialog(ViewModel.Game.Score);
        }

        private void OnGameOver()
        {
            _running = false;
            _timer.Stop();

            Dispatcher.UIThread.Post(() =>
            {
                SaveScoreAndShowDialog();
            });
        }

        private void UpdateHighScoresList()
        {
            ViewModel.UpdateHighScoresList();
        }

        private void RenderNext()
        {
            NextCanvas.Children.Clear();
            var block = ViewModel.Game.NextPiece;
            if (block == null) return;

            int cell = 18;

            int minX = 4, maxX = 0, minY = 4, maxY = 0;
            bool hasBlocks = false;

            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    if (block[y, x] != 0)
                    {
                        hasBlocks = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

            if (!hasBlocks) return;

            int width = (maxX - minX + 1) * cell;
            int height = (maxY - minY + 1) * cell;
            int offsetX = (80 - width) / 2 - minX * cell;
            int offsetY = (80 - height) / 2 - minY * cell;

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
                            Fill = new SolidColorBrush(ViewModel.Game.ColorFor(v)),
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(rect, x * cell + offsetX);
                        Canvas.SetTop(rect, y * cell + offsetY);
                        NextCanvas.Children.Add(rect);
                    }
                }
        }

        private void RenderAll()
        {
            PlayfieldCanvas.Children.Clear();
            int cell = TetrisGame.CellSize;
            var grid = ViewModel.Game.Grid;

            for (int y = 0; y < TetrisGame.Height; y++)
                for (int x = 0; x < TetrisGame.Width; x++)
                {
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

                    var v = grid[y, x];
                    if (v != 0)
                    {
                        var rect = new Avalonia.Controls.Shapes.Rectangle
                        {
                            Width = cell - 2,
                            Height = cell - 2,
                            Fill = new SolidColorBrush(ViewModel.Game.ColorFor(v)),
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(rect, x * cell + 1);
                        Canvas.SetTop(rect, y * cell + 1);
                        PlayfieldCanvas.Children.Add(rect);
                    }
                }

            var cur = ViewModel.Game.CurrentPiece;
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
                                Fill = new SolidColorBrush(ViewModel.Game.ColorFor(v)),
                                Stroke = Brushes.Black,
                                StrokeThickness = 1
                            };
                            Canvas.SetLeft(rect, (ViewModel.Game.CurrentX + x) * cell + 1);
                            Canvas.SetTop(rect, (ViewModel.Game.CurrentY + y) * cell + 1);
                            PlayfieldCanvas.Children.Add(rect);
                        }
                    }
            }
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_running) return;

            bool keyHandled = false;

            switch (e.Key)
            {
                case Key.Left:
                    ViewModel.Game.TryMove(-1, 0);
                    RenderAll();
                    keyHandled = true;
                    break;
                case Key.Right:
                    ViewModel.Game.TryMove(1, 0);
                    RenderAll();
                    keyHandled = true;
                    break;
                case Key.Up:
                    ViewModel.Game.TryRotate();
                    RenderAll();
                    keyHandled = true;
                    break;
                case Key.Down:
                    ViewModel.Game.TryMove(0, 1);
                    RenderAll();
                    keyHandled = true;
                    break;
                case Key.Space:
                    ViewModel.Game.HardDrop();
                    RenderAll();
                    keyHandled = true;
                    break;
            }

            if (keyHandled)
            {
                e.Handled = true;
                this.Focus();
            }
        }

        private void BtnStartGame_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ViewModel.PlayerName = string.IsNullOrWhiteSpace(PlayerNameTextBox.Text) ?
                "Player" : PlayerNameTextBox.Text.Trim();

            if (ViewModel.PlayerName.Length > 10)
                ViewModel.PlayerName = ViewModel.PlayerName.Substring(0, 10);

            ViewModel.Game.PlayerName = ViewModel.PlayerName;
            ViewModel.SwitchToGameScreen();
            StartGame();
        }

        private async void BtnHighScoresMenu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await ViewModel.Game.LoadHighScoresAsync();
            ViewModel.UpdateTopScore();
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

            int rank = 1;
            foreach (var score in ViewModel.Game.HighScores.OrderByDescending(h => h.Score).Take(10))
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

        private void ShowGameOverDialog(int score)
        {
            var dialog = new Window
            {
                Title = "Game Over",
                Width = 360,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12
            };

            var title = new TextBlock
            {
                Text = "GAME OVER",
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 50, 50)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            root.Children.Add(title);

            var scoreText = new TextBlock
            {
                Text = $"SCORE: {score:N0}",
                FontSize = 18,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            root.Children.Add(scoreText);

            var buttons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 10
            };

            var btnRestart = new Button
            {
                Content = "RESTART",
                Width = 120,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(85, 255, 85)),
                Foreground = Brushes.Black,
                FontWeight = FontWeight.Bold,
                CornerRadius = new CornerRadius(5)
            };

            btnRestart.Click += (s, e) =>
            {
                dialog.Close();
                StartGame();
            };

            var btnMenu = new Button
            {
                Content = "MENU",
                Width = 120,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Foreground = Brushes.White,
                CornerRadius = new CornerRadius(5)
            };

            btnMenu.Click += (s, e) =>
            {
                dialog.Close();
                ViewModel.SwitchToMenuScreen();
            };

            buttons.Children.Add(btnRestart);
            buttons.Children.Add(btnMenu);
            root.Children.Add(buttons);

            dialog.Content = root;
            dialog.ShowDialog(this);
        }

        private void BtnExit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private void BtnBackToMenu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            PauseGame();
            ViewModel.SwitchToMenuScreen();
        }

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
            ViewModel.Game.Reset();
            _running = true;
            _timer.Start();

            ViewModel.SetGameStatePlaying();

            RenderAll();
            RenderNext();
            UpdateHighScoresList();
            this.Focus();
        }

        private void PauseGame()
        {
            _timer.Stop();
            _running = false;
            ViewModel.SetGameStatePaused();
        }

        private void ResumeGame()
        {
            _timer.Start();
            _running = true;
            ViewModel.SetGameStatePlaying();
            this.Focus();
        }
    }
}