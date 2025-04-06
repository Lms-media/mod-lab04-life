using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;

namespace cli_life {
  public class Cell {
    public bool IsAlive;
    public readonly List<Cell> neighbors = new List<Cell>();
    private bool IsAliveNext;
    public void DetermineNextLiveState() {
      int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
      if (IsAlive)
        IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
      else
        IsAliveNext = liveNeighbors == 3;
    }
    public void Advance() {
      IsAlive = IsAliveNext;
    }
  }
  public class Board {
    public readonly Cell[,] Cells;
    public readonly int CellSize;

    public int Columns { get { return Cells.GetLength(0); } }
    public int Rows { get { return Cells.GetLength(1); } }
    public int Width { get { return Columns * CellSize; } }
    public int Height { get { return Rows * CellSize; } }

    public Board(int width, int height, int cellSize, double liveDensity = .1) {
      CellSize = cellSize;

      Cells = new Cell[width / cellSize, height / cellSize];
      for (int x = 0; x < Columns; x++)
        for (int y = 0; y < Rows; y++)
          Cells[x, y] = new Cell();

      ConnectNeighbors();
      Randomize(liveDensity);
    }

    readonly Random rand = new Random();
    public void Randomize(double liveDensity) {
      foreach (var cell in Cells)
        cell.IsAlive = rand.NextDouble() < liveDensity;
    }

    public void Advance() {
      foreach (var cell in Cells)
        cell.DetermineNextLiveState();
      foreach (var cell in Cells)
        cell.Advance();
    }
    private void ConnectNeighbors() {
      for (int x = 0; x < Columns; x++) {
        for (int y = 0; y < Rows; y++) {
          int xL = (x > 0) ? x - 1 : Columns - 1;
          int xR = (x < Columns - 1) ? x + 1 : 0;

          int yT = (y > 0) ? y - 1 : Rows - 1;
          int yB = (y < Rows - 1) ? y + 1 : 0;

          Cells[x, y].neighbors.Add(Cells[xL, yT]);
          Cells[x, y].neighbors.Add(Cells[x, yT]);
          Cells[x, y].neighbors.Add(Cells[xR, yT]);
          Cells[x, y].neighbors.Add(Cells[xL, y]);
          Cells[x, y].neighbors.Add(Cells[xR, y]);
          Cells[x, y].neighbors.Add(Cells[xL, yB]);
          Cells[x, y].neighbors.Add(Cells[x, yB]);
          Cells[x, y].neighbors.Add(Cells[xR, yB]);
        }
      }
    }
    public void SaveToFile(string fileName) {
      using (StreamWriter writer = new StreamWriter(fileName)) {
        writer.WriteLine($"{Columns} {Rows} {CellSize}");
        for (int y = 0; y < Rows; y++) {
          for (int x = 0; x < Columns; x++) {
            writer.Write(Cells[x, y].IsAlive ? '1' : '0');
          }
          writer.WriteLine();
        }
      }
    }
    public static Board LoadFromFile(string fileName) {
      using (StreamReader reader = new StreamReader(fileName)) {
        var dimensions = reader.ReadLine().Split(' ');
        int cols = int.Parse(dimensions[0]);
        int rows = int.Parse(dimensions[1]);
        int cellSize = int.Parse(dimensions[2]);
        Board board = new Board(cols * cellSize, rows * cellSize, cellSize);

        for (int y = 0; y < rows; y++) {
          string line = reader.ReadLine();
          for (int x = 0; x < cols; x++) {
            board.Cells[x, y].IsAlive = line[x] == '1';
          }
        }

        return board;
      }
    }
    public void LoadPattern(string fileName, int offsetX = 0, int offsetY = 0) {
      string[] lines = File.ReadAllLines(fileName);
      for (int y = 0; y < lines.Length; y++) {
        for (int x = 0; x < lines[y].Length; x++) {
          int targetX = (x + offsetX) % Columns;
          int targetY = (y + offsetY) % Rows;
          Cells[targetX, targetY].IsAlive = lines[y][x] == '1';
        }
      }
    }
  }

  public class GameSettings {
    public int Width { get; set; } = 50;
    public int Height { get; set; } = 20;
    public int CellSize { get; set; } = 1;
    public double LiveDensity { get; set; } = 0.5;
    public int Delay { get; set; } = 1000;
  }
  class Program {
    static Board board;
    static GameSettings settings;
    static int delay;
    static int generation = 1;

    static private void Reset(string fileName = "") {
      if (string.IsNullOrEmpty(fileName)) {
        board = new Board(
          width: settings.Width,
          height: settings.Height,
          cellSize: settings.CellSize,
          liveDensity: settings.LiveDensity);
      }
      else {
        board = Board.LoadFromFile(fileName);
      }
    }
    static void Render() {
      for (int row = 0; row < board.Rows; row++) {
        for (int col = 0; col < board.Columns; col++) {
          var cell = board.Cells[col, row];
          if (cell.IsAlive) {
            Console.Write('*');
          }
          else {
            Console.Write(' ');
          }
        }
        Console.Write('\n');
      }
      Console.Write($"Generation {generation}");
      generation++;
    }

    static void LoadSettings(string fileName) {
      try {
        string json = File.ReadAllText(fileName);
        settings = JsonSerializer.Deserialize<GameSettings>(json);
        delay = settings.Delay;
      }
      catch {
        settings = new GameSettings();
        delay = settings.Delay;
      }
    }
    static void Main(string[] args) {
      string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
      string configPath = Path.Combine(projectDirectory, "config.json");
      string savePath = Path.Combine(projectDirectory, "board.txt");
      string patternPath = Path.Combine(projectDirectory, "patterns/gosperGun.txt");
      LoadSettings(configPath);
      Reset(savePath);
      //Reset();
      board.LoadPattern(patternPath);
      while (generation < 101) {
        Console.Clear();
        Render();
        board.Advance();
        Thread.Sleep(delay);
      }
      board.SaveToFile(savePath);
    }
  }
}