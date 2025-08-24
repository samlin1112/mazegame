using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MazeGame
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }

    public class GameForm : Form
    {
        // === 可調參數 ===
        private const int Rows = 21;   // 迷宮行數（>2）
        private const int Cols = 31;   // 迷宮列數（>2）
        private const int CellSize = 24; // 每格像素
        private const int WallThickness = 2; // 牆線粗細

        // === 狀態 ===
        private Cell[,] grid;
        private Point player;
        private Point goal;
        private readonly Random rng = new Random();
        private readonly Pen wallPen = new Pen(Color.Black, WallThickness);
        private readonly Brush playerBrush = new SolidBrush(Color.DeepSkyBlue);
        private readonly Brush goalBrush = new SolidBrush(Color.OrangeRed);
        private readonly Brush bgBrush = new SolidBrush(Color.White);
        private bool won = false;

        public GameForm()
        {
            Text = "迷宮（方向鍵移動 / R 重生 / 空白鍵回到起點）";
            DoubleBuffered = true;
            BackColor = Color.Gainsboro;

            int width = Cols * CellSize + WallThickness + 1;
            int height = Rows * CellSize + WallThickness + 1 + 40; // 多留一條資訊列
            ClientSize = new Size(width, height);

            KeyPreview = true;
            KeyDown += OnKeyDown;

            GenerateNewMaze();
        }

        #region 迷宮資料結構與生成
        private class Cell
        {
            public bool Visited;
            public bool Top = true, Right = true, Bottom = true, Left = true; // 牆存在與否
        }

        private void GenerateNewMaze()
        {
            grid = new Cell[Rows, Cols];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    grid[r, c] = new Cell();

            // 深度優先回溯（Perfect Maze：任兩點唯一路徑）
            Stack<Point> stack = new Stack<Point>();
            int sr = rng.Next(Rows);
            int sc = rng.Next(Cols);
            grid[sr, sc].Visited = true;
            stack.Push(new Point(sc, sr));

            while (stack.Count > 0)
            {
                var cur = stack.Peek();
                var neighbors = GetUnvisitedNeighbors(cur.Y, cur.X);
                if (neighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }
                var (nr, nc, dir) = neighbors[rng.Next(neighbors.Count)];
                RemoveWall(cur.Y, cur.X, nr, nc, dir);
                grid[nr, nc].Visited = true;
                stack.Push(new Point(nc, nr));
            }

            player = new Point(0, 0);
            goal = new Point(Cols - 1, Rows - 1);
            won = false;
            Invalidate();
        }

        private List<(int r, int c, int dir)> GetUnvisitedNeighbors(int r, int c)
        {
            var res = new List<(int, int, int)>();
            // dir: 0=上,1=右,2=下,3=左
            if (r > 0 && !grid[r - 1, c].Visited) res.Add((r - 1, c, 0));
            if (c < Cols - 1 && !grid[r, c + 1].Visited) res.Add((r, c + 1, 1));
            if (r < Rows - 1 && !grid[r + 1, c].Visited) res.Add((r + 1, c, 2));
            if (c > 0 && !grid[r, c - 1].Visited) res.Add((r, c - 1, 3));
            return res;
        }

        private void RemoveWall(int r1, int c1, int r2, int c2, int dir)
        {
            // 打通 r1,c1 與 r2,c2 的牆
            if (dir == 0) { grid[r1, c1].Top = false; grid[r2, c2].Bottom = false; }
            else if (dir == 1) { grid[r1, c1].Right = false; grid[r2, c2].Left = false; }
            else if (dir == 2) { grid[r1, c1].Bottom = false; grid[r2, c2].Top = false; }
            else if (dir == 3) { grid[r1, c1].Left = false; grid[r2, c2].Right = false; }
        }
        #endregion

        #region 輸入與邏輯
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                GenerateNewMaze();
                return;
            }
            if (e.KeyCode == Keys.Space)
            {
                player = new Point(0, 0);
                won = false;
                Invalidate();
                return;
            }

            if (won) return;

            int r = player.Y, c = player.X;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (!grid[r, c].Top) r--;
                    break;
                case Keys.Right:
                    if (!grid[r, c].Right) c++;
                    break;
                case Keys.Down:
                    if (!grid[r, c].Bottom) r++;
                    break;
                case Keys.Left:
                    if (!grid[r, c].Left) c--;
                    break;
                default:
                    return;
            }

            // 邊界保護
            r = Math.Max(0, Math.Min(Rows - 1, r));
            c = Math.Max(0, Math.Min(Cols - 1, c));
            player = new Point(c, r);

            if (player.X == goal.X && player.Y == goal.Y)
                won = true;

            Invalidate();
        }
        #endregion

        #region 繪製
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            // 迷宮背景
            g.FillRectangle(bgBrush, new Rectangle(0, 0, Cols * CellSize + WallThickness, Rows * CellSize + WallThickness));

            // 畫牆
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    int x = c * CellSize;
                    int y = r * CellSize;

                    if (grid[r, c].Top)
                        g.DrawLine(wallPen, x, y, x + CellSize, y);
                    if (grid[r, c].Right)
                        g.DrawLine(wallPen, x + CellSize, y, x + CellSize, y + CellSize);
                    if (grid[r, c].Bottom)
                        g.DrawLine(wallPen, x, y + CellSize, x + CellSize, y + CellSize);
                    if (grid[r, c].Left)
                        g.DrawLine(wallPen, x, y, x, y + CellSize);
                }
            }

            // 起點、終點與玩家
            DrawCell(g, new Point(0, 0), Brushes.LightGreen);
            DrawCell(g, goal, goalBrush);
            DrawPlayer(g);

            // 資訊列
            string info = won ? "✔ 完成！按 R 重新生成；空白鍵回起點" : "方向鍵移動；R 重新生成；空白鍵回起點";
            TextRenderer.DrawText(g, info, this.Font, new Point(6, Rows * CellSize + 8), Color.Black);
        }

        private void DrawCell(Graphics g, Point cell, Brush brush)
        {
            int x = cell.X * CellSize + WallThickness;
            int y = cell.Y * CellSize + WallThickness;
            g.FillRectangle(brush, x + 2, y + 2, CellSize - 3, CellSize - 3);
        }

        private void DrawPlayer(Graphics g)
        {
            int cx = player.X * CellSize + CellSize / 2;
            int cy = player.Y * CellSize + CellSize / 2;
            int radius = (int)(CellSize * 0.35);
            g.FillEllipse(playerBrush, cx - radius, cy - radius, radius * 2, radius * 2);
        }
        #endregion

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            wallPen.Dispose();
            playerBrush.Dispose();
            goalBrush.Dispose();
            bgBrush.Dispose();
        }
    }
}
