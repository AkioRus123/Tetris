using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Tetris
{
    public partial class MainWindow : Window
    {
        private int Rows = 20;
        private int Columns = 10;
        private int BlockSize;//размеры игового поля 

        private DispatcherTimer timer;
        private int[,] grid;
        private List<Rectangle> currentShape;
        private Point currentShapePosition;
        private Point[] currentShapeBlocks;
        private List<Point[]> shapes;
        private int score = 0;
        private bool isGameOver = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            BlockSize = (int)(screenHeight / Rows);

            GameCanvas.Width = Columns * BlockSize;
            GameCanvas.Height = Rows * BlockSize;

            GameCanvas.HorizontalAlignment = HorizontalAlignment.Center;
            GameCanvas.VerticalAlignment = VerticalAlignment.Center;

            grid = new int[Rows, Columns];
            shapes = new List<Point[]>
            {
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) }, // []-квадрат так пишу
                new Point[] { new Point(-1, 0), new Point(0, 0), new Point(1, 0), new Point(2, 0) }, // I
                new Point[] { new Point(0, 0), new Point(-1, 0), new Point(0, 1), new Point(1, 1) }, // S
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(-1, 1) }, // Z
                new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(0, 1) }, // T
                new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(-1, 1) }, // L
                new Point[] { new Point(0, 0), new Point(-1, 0), new Point(1, 0), new Point(1, 1) }  // J
            };

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += GameLoop;

            DrawGrid();
        }

        private void DrawGrid()
        {
            GameCanvas.Children.Clear();

            // Сетка
            for (int i = 0; i <= Rows; i++)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = i * BlockSize,
                    X2 = Columns * BlockSize,
                    Y2 = i * BlockSize,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5
                };
                GameCanvas.Children.Add(line);
            }

            for (int j = 0; j <= Columns; j++)
            {
                var line = new Line
                {
                    X1 = j * BlockSize,
                    Y1 = 0,
                    X2 = j * BlockSize,
                    Y2 = Rows * BlockSize,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5
                };
                GameCanvas.Children.Add(line);
            }

            // Отрисовываем блоки сетки
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    if (grid[i, j] == 1)
                    {
                        var rect = new Rectangle
                        {
                            Width = BlockSize,
                            Height = BlockSize,
                            Fill = Brushes.Blue,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(rect, j * BlockSize);
                        Canvas.SetTop(rect, i * BlockSize);
                        GameCanvas.Children.Add(rect);
                    }
                }
            }

            // Отрисовываем текущую фигуру
            if (currentShape != null)//Проверяет существует ли текущая фигура 
            {
                foreach (var rect in currentShape)
                {
                    GameCanvas.Children.Add(rect);
                }
            }
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)//обработка собътий
        {
            ResetGame();
            SpawnShape();
            timer.Start();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Управление в игре:\n" +
                            "← - переместить фигуру влево\n" +
                            "→ - переместить фигуру вправо\n" +
                            "↓ - ускорить падение фигуры\n" +
                            "Пробел - мгновенно опустить фигуру\n" +
                            "\nЦель игры: заполнить горизонтальные линии блоками, чтобы очистить их и набрать очки.",
                            "Помощь");
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ResetGame()
        {
            Array.Clear(grid, 0, grid.Length);
            score = 0;
            isGameOver = false;
            ScoreLabel.Text = "Очки: 0";
            GameCanvas.Children.Clear();
            DrawGrid();
        }

        private void SpawnShape()
        {
            if (isGameOver) return;

            Random random = new Random();
            currentShapeBlocks = shapes[random.Next(shapes.Count)];
            currentShape = new List<Rectangle>();
            currentShapePosition = new Point(Columns / 2, 0);

            foreach (var block in currentShapeBlocks)
            {
                double x = currentShapePosition.X + block.X;
                double y = currentShapePosition.Y + block.Y;

                if (y >= 0 && grid[(int)y, (int)x] == 1)
                {
                    EndGame();
                    return;
                }
                
                var rect = new Rectangle
                {
                    Width = BlockSize,
                    Height = BlockSize,
                    Fill = Brushes.Red,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(rect, x * BlockSize);
                Canvas.SetTop(rect, y * BlockSize);
                currentShape.Add(rect);
            }

            DrawGrid();
        }

        private void EndGame()
        {
            timer.Stop();
            isGameOver = true;
            MessageBox.Show($"Игра окончена! Твои очки: {score}", "Tetris");
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (!MoveShape(0, 1))
            {
                PlaceShape();
                ClearRows();
                SpawnShape();
            }
        }

        private bool MoveShape(int offsetX, int offsetY)
        {
            if (CheckCollision(offsetX, offsetY)) return false;

            currentShapePosition.X += offsetX;
            currentShapePosition.Y += offsetY;

            for (int i = 0; i < currentShape.Count; i++)
            {
                var rect = currentShape[i];
                Canvas.SetLeft(rect, (currentShapePosition.X + currentShapeBlocks[i].X) * BlockSize);
                Canvas.SetTop(rect, (currentShapePosition.Y + currentShapeBlocks[i].Y) * BlockSize);
            }

            DrawGrid();
            return true;
        }

        private bool CheckCollision(int offsetX, int offsetY)
        {
            foreach (var block in currentShapeBlocks)
            {
                double x = currentShapePosition.X + block.X + offsetX;
                double y = currentShapePosition.Y + block.Y + offsetY;

                if (x < 0 || x >= Columns || y >= Rows || (y >= 0 && grid[(int)y, (int)x] == 1))
                {
                    return true;
                }
            }
            return false;
        }

        private void PlaceShape()
        {
            foreach (var block in currentShapeBlocks)
            {
                int x = (int)(currentShapePosition.X + block.X);
                int y = (int)(currentShapePosition.Y + block.Y);

                if (y >= 0)
                {
                    grid[y, x] = 1;
                }
            }

            currentShape.Clear();
            DrawGrid();
        }

        private void ClearRows()
        {
            for (int i = Rows - 1; i >= 0; i--)
            {
                bool isFull = true;

                for (int j = 0; j < Columns; j++)
                {
                    if (grid[i, j] == 0)
                    {
                        isFull = false;
                        break;
                    }
                }

                if (isFull)
                {
                    for (int k = i; k > 0; k--)
                    {
                        for (int j = 0; j < Columns; j++)
                        {
                            grid[k, j] = grid[k - 1, j];
                        }
                    }

                    for (int j = 0; j < Columns; j++)
                    {
                        grid[0, j] = 0;
                    }

                    score += 100;
                    ScoreLabel.Text = $"Очки: {score}";
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (isGameOver) return;

            switch (e.Key)
            {
                case Key.Left:
                    MoveShape(-1, 0);
                    break;
                case Key.Right:
                    MoveShape(1, 0);
                    break;
                case Key.Down:
                    MoveShape(0, 1);
                    break;
                case Key.Space:
                    while (MoveShape(0, 1)) { }
                    break;
            }
        }
    }
}
