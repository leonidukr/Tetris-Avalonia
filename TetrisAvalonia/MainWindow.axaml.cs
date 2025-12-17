using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TetrisAvalonia
{
    public partial class MainWindow : Window
    {
        private readonly TetrisGame _game;
        private readonly DispatcherTimer _timer;
        private bool _running = false;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _game = new TetrisGame();
            DataContext = _game;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(30); // render/update интервал
            _timer.Tick += Timer_Tick;

            this.KeyDown += MainWindow_KeyDown;

            // Инициализируем отрисовку
            RenderAll();
            UpdateUi();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var needRender = _game.Update();
            if (needRender)
            {
                RenderAll();
                UpdateUi();
            }
        }

        private void UpdateUi()
        {
            TxtState.Text = _running ? "Running" : "Stopped";
        }

        // Метод для отрисовки
        private void RenderAll()
        {
            PlayfieldCanvas.Children.Clear();
            int cell = TetrisGame.CellSize;
            var grid = _game.Grid;
            for (int y = 0; y < TetrisGame.Height; y++)
                for (int x = 0; x < TetrisGame.Width; x++)
                {
                    var v = grid[y, x];
                    var rect = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = cell - 2,
                        Height = cell - 2,
                        Fill = new SolidColorBrush(_game.ColorFor(v)),
                    };
                    Canvas.SetLeft(rect, x * cell + 1);
                    Canvas.SetTop(rect, y * cell + 1);
                    PlayfieldCanvas.Children.Add(rect);

                    // Border
                    var border = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = cell,
                        Height = cell,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(border, x * cell);
                    Canvas.SetTop(border, y * cell);
                    PlayfieldCanvas.Children.Add(border);
                }

            // Отрисовка текущей фигуры
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
                                Fill = new SolidColorBrush(_game.ColorFor(v))
                            };
                            Canvas.SetLeft(rect, (_game.CurrentX + x) * cell + 1);
                            Canvas.SetTop(rect, (_game.CurrentY + y) * cell + 1);
                            PlayfieldCanvas.Children.Add(rect);
                        }
                    }
            }
        }

        // Обработчик нажатий клавиш
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

            UpdateUi();
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
            _game.Reset();
            _running = true;
            _timer.Start();
            UpdateUi();
        }

        private void PauseGame()
        {
            _timer.Stop();
            _running = false;
            UpdateUi();
        }

        private void ResumeGame()
        {
            _timer.Start();
            _running = true;
            UpdateUi();
        }
    }
}