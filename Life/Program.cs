using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;
using ScottPlot;

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
    public void LoadFromFile(string fileName) {
      using (StreamReader reader = new StreamReader(fileName)) {
        int cols = int.Parse(dimensions[0]);
        int rows = int.Parse(dimensions[1]);

        for (int y = 0; y < rows; y++) {
          string line = reader.ReadLine();
          for (int x = 0; x < cols; x++) {
            Cells[x, y].IsAlive = line[x] == '1';
          }
        }
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
  public class ClusterAnalyzer {
    public static List<HashSet<(int, int)>> FindClusters(Board board) {
      var clusters = new List<HashSet<(int, int)>>();
      var visited = new bool[board.Columns, board.Rows];

      for (int y = 0; y < board.Rows; y++) {
        for (int x = 0; x < board.Columns; x++) {
          if (board.Cells[x, y].IsAlive && !visited[x, y]) {
            var cluster = new HashSet<(int, int)>();
            ExploreCluster(board, x, y, visited, cluster);
            clusters.Add(cluster);
          }
        }
      }

      return clusters;
    }

    private static void ExploreCluster(Board board, int x, int y, bool[,] visited, HashSet<(int, int)> cluster) {
      var queue = new Queue<(int, int)>();
      queue.Enqueue((x, y));
      visited[x, y] = true;

      while (queue.Count > 0) {
        var (cx, cy) = queue.Dequeue();
        cluster.Add((cx, cy));

        for (int dy = -1; dy <= 1; dy++) {
          for (int dx = -1; dx <= 1; dx++) {
            if (dx == 0 && dy == 0)
              continue;

            int nx = (cx + dx + board.Columns) % board.Columns;
            int ny = (cy + dy + board.Rows) % board.Rows;

            if (board.Cells[nx, ny].IsAlive && !visited[nx, ny]) {
              visited[nx, ny] = true;
              queue.Enqueue((nx, ny));
            }
          }
        }
      }
    }

    public static string ClassifyCluster(HashSet<(int x, int y)> cluster, string patternsDir) {
      var normalized = NormalizeCluster(cluster);

      var templates = LoadTemplates(patternsDir);

      foreach (var (name, template) in templates) {
        if (AreClustersEqual(normalized, template)) {
          return name;
        }
      }

      return $"Unknown ({cluster.Count} cells)";
    }

    private static HashSet<(int x, int y)> NormalizeCluster(HashSet<(int x, int y)> cluster) {
      int minX = cluster.Min(p => p.x);
      int minY = cluster.Min(p => p.y);

      return [.. cluster.Select(p => (p.x - minX, p.y - minY))];
    }

    private static Dictionary<string, HashSet<(int x, int y)>> LoadTemplates(string dir) {
      var templates = new Dictionary<string, HashSet<(int x, int y)>>();

      foreach (var file in Directory.GetFiles(dir, "*.txt")) {
        var pattern = new HashSet<(int x, int y)>();
        string[] lines = File.ReadAllLines(file);

        for (int y = 0; y < lines.Length; y++) {
          for (int x = 0; x < lines[y].Length; x++) {
            if (lines[y][x] == '1') {
              pattern.Add((x, y));
            }
          }
        }

        templates.Add(Path.GetFileNameWithoutExtension(file), pattern);
      }

      return templates;
    }

    private static bool AreClustersEqual(
        HashSet<(int x, int y)> cluster1,
        HashSet<(int x, int y)> cluster2) {
      if (cluster1.Count != cluster2.Count)
        return false;

      for (int rotation = 0; rotation < 4; rotation++) {
        var rotated = RotateCluster(cluster1, rotation);
        if (rotated.SetEquals(cluster2))
          return true;
      }

      return false;
    }

    private static HashSet<(int x, int y)> RotateCluster(
        HashSet<(int x, int y)> cluster,
        int rotations) {
      var result = new HashSet<(int x, int y)>();
      int size = cluster.Max(p => Math.Max(p.x, p.y)) + 1;

      foreach (var (x, y) in cluster) {
        var (rx, ry) = (x, y);

        for (int i = 0; i < rotations; i++) {
          (rx, ry) = (ry, size - 1 - rx);
        }

        result.Add((rx, ry));
      }

      return result;
    }
  }
  public class StabilityAnalyzer {
    private const int StabilityThreshold = 5;
    private Queue<int> history = new Queue<int>();

    public bool CheckStability(Board board) {
      int aliveCount = 0;
      for (int y = 0; y < board.Rows; y++)
        for (int x = 0; x < board.Columns; x++)
          if (board.Cells[x, y].IsAlive)
            aliveCount++;

      history.Enqueue(aliveCount);
      if (history.Count > StabilityThreshold)
        history.Dequeue();

      return history.Distinct().Count() == 1 && history.Count == StabilityThreshold;
    }

    public void SaveValue(int generation, string fileName) {
      List<string> lines = File.ReadAllLines(fileName).ToList();
      string newRecord = $"{DateTime.Now.ToString()}: {generation}";
      if (lines.Count > 0)
        lines[lines.Count - 1] = newRecord;
      else
        lines.Add(newRecord);
      int average = 0;
      foreach (string line in lines) {
        average += int.Parse(line.Split(": ")[1]);
      }
      average /= lines.Count;
      lines.Add($"Average genereations count: {average}");
      File.WriteAllLines(fileName, lines);
    }
  }
  class Program {
    static Board board;
    static GameSettings settings;
    static StabilityAnalyzer stabilityAnalyzer = new StabilityAnalyzer();
    static int delay;
    static int generation = 1;
    static int stableGeneration = 1;

    static private void Reset(string fileName = "") {
      generation = 1;
      stableGeneration = 1;
      board = new Board(
          width: settings.Width,
          height: settings.Height,
          cellSize: settings.CellSize,
          liveDensity: settings.LiveDensity);
      if (!string.IsNullOrEmpty(fileName)) {
        board.LoadFromFile(fileName);
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
    static int KeyAction(string savePath) {
      if (Console.KeyAvailable) {
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.S) {
          board.SaveToFile(savePath);
          Console.WriteLine("\nState saved to board.txt");
        }
        else if (key == ConsoleKey.L) {
          board.LoadFromFile(savePath);
          Console.WriteLine("\nState loaded from board.txt");
        }
        else if (key == ConsoleKey.Escape) {
          return 1;
        }
      }
      return 0;
    }
    static void PrintClustersInfo(string patternsPath) {
      var clusters = ClusterAnalyzer.FindClusters(board);
      Console.WriteLine($"\nclusters count: {clusters.Count}");
      foreach (var cluster in clusters.OrderBy(c => -c.Count)) {
        Console.WriteLine($"{ClusterAnalyzer.ClassifyCluster(cluster, patternsPath)} (size: {cluster.Count})");
      }
    }

    static void SingleStart() {
      string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
      string configPath = Path.Combine(projectDirectory, "config.json");
      string savePath = Path.Combine(projectDirectory, "board.txt");
      string analiticsPath = Path.Combine(projectDirectory, "stabilityAnalysis/stableGenerationsFor0_9.txt");
      string patternsPath = Path.Combine(projectDirectory, "patterns/");
      LoadSettings(configPath);
      //Reset(savePath);
      Reset();
      //board.LoadPattern(patternsPath + "gosperGun.txt");

      while (true) {
        if (KeyAction(savePath) == 1)
          break;

        Console.Clear();
        Render();

        if (stabilityAnalyzer.CheckStability(board)) {
          Console.WriteLine($"\nThe system has stabilized for a generation: {stableGeneration}");
          //stabilityAnalyzer.SaveValue(stableGeneration, analiticsPath);
          PrintClustersInfo(patternsPath);
          break;
        }
        else {
          stableGeneration++;
        }

        board.Advance();
        Thread.Sleep(delay);
      }
    }
    static void RunPopulationTracking(string fileName) {
      double density = 0.6;
      int maxGenerations = 600;
      StabilityAnalyzer stabilityAnalyzer = new StabilityAnalyzer();

      File.WriteAllText(fileName,
          "Generation AliveCells\n");

      var board = new Board(50, 20, 1, density);
      int gen = 0;
      while (gen < maxGenerations && !stabilityAnalyzer.CheckStability(board)) {
        int aliveCount = 0;
        for (int y = 0; y < board.Rows; y++)
          for (int x = 0; x < board.Columns; x++)
            if (board.Cells[x, y].IsAlive)
              aliveCount++;

        File.AppendAllText(fileName,
            $"{gen} {aliveCount}\n");

        board.Advance();
        gen++;
      }
    }
    public static void CreatePopulationPlot(string dataFileName, string plotFileName) {
      var data = File.ReadAllLines(dataFileName)
          .Skip(1)
          .Select(line => line.Split(' '))
          .Where(parts => parts.Length == 2)
          .Select(parts => new {
            Generation = int.Parse(parts[0]),
            AliveCells = int.Parse(parts[1])
          })
          .ToList();

      var plot = new Plot();

      var xValues = data.Select(x => (double)x.Generation).ToArray();
      var yValues = data.Select(x => (double)x.AliveCells).ToArray();
      var sig = plot.Add.Scatter(xValues, yValues);
      sig.MarkerSize = 1;

      plot.Title("Dynamics of the number of living cells for density 0.6", size: 16);
      plot.XLabel("Generation number", size: 14);
      plot.YLabel("Number of cells", size: 14);

      plot.Axes.AutoScale();

      plot.SavePng(plotFileName, 500, 500);
    }
    static void RunPopulationTrackingForAll(string fileName) {
      double[] densities = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };
      int maxGenerations = 600;

      File.WriteAllText(fileName,
          "Generation Density AliveCells\n");

      foreach (var density in densities) {
        var board = new Board(50, 20, 1, density);

        for (int gen = 0; gen < maxGenerations; gen++) {
          int aliveCount = 0;
          for (int y = 0; y < board.Rows; y++)
            for (int x = 0; x < board.Columns; x++)
              if (board.Cells[x, y].IsAlive)
                aliveCount++;

          File.AppendAllText(fileName,
              $"{gen} {density} {aliveCount}\n");

          board.Advance();
        }
      }
    }
    public static void CreatePopulationPlotForAll(string dataFileName, string plotFileName) {
      var data = File.ReadAllLines(dataFileName)
          .Skip(1)
          .Select(line => line.Split(' '))
          .Where(parts => parts.Length == 3)
          .Select(parts => new {
            Generation = int.Parse(parts[0]),
            Density = double.Parse(parts[1]),
            AliveCells = int.Parse(parts[2])
          })
          .ToList();

      var plot = new Plot();

      var groups = data.GroupBy(x => x.Density);

      var colors = new[] {
        Colors.Red,
        Colors.Orange,
        Colors.Yellow,
        Colors.Green,
        Colors.SkyBlue,
        Colors.DarkBlue,
        Colors.Violet,
        Colors.Gray,
        Colors.Black
      };

      int colorIndex = 0;
      foreach (var group in groups) {
        var xValues = group.Select(x => (double)x.Generation).ToArray();
        var yValues = group.Select(x => (double)x.AliveCells).ToArray();

        var sig = plot.Add.Scatter(xValues, yValues);
        sig.Color = colors[colorIndex];
        sig.MarkerSize = 1;
        sig.LegendText = group.First().Density.ToString();

        colorIndex = (colorIndex + 1) % colors.Length;
      }

      plot.Title("Dynamics of the number of living cells", size: 16);
      plot.XLabel("Generation number", size: 14);
      plot.YLabel("Number of cells", size: 14);

      plot.Axes.AutoScale();
      plot.ShowLegend();

      plot.SavePng(plotFileName, 2000, 1000);
    }
    static void Main(string[] args) {
      SingleStart();
      //string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
      //string dataPath = Path.Combine(projectDirectory, "data.txt");
      //string plotPath = Path.Combine(projectDirectory, "plot.png");
      //RunPopulationTracking(dataPath);
      //CreatePopulationPlot(dataPath, plotPath);
    }
  }
}