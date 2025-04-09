using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using System.Text.Json;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Life.Tests;

[TestClass]
public class CellsTests {
  [TestMethod]
  public void CellTest1() {
    var cell = new Cell { IsAlive = true };
    cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 1));
    cell.DetermineNextLiveState();
    Assert.IsFalse(cell.IsAliveNext);
  }

  [TestMethod]
  public void CellTest2() {
    var cell = new Cell { IsAlive = true };
    cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 2));
    cell.DetermineNextLiveState();
    Assert.IsTrue(cell.IsAliveNext);
  }

  [TestMethod]
  public void CellTest3() {
    var cell = new Cell { IsAlive = true };
    cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 4));
    cell.DetermineNextLiveState();
    Assert.IsFalse(cell.IsAliveNext);
  }

  [TestMethod]
  public void CellTest4() {
    var cell = new Cell { IsAlive = false };
    cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));
    cell.DetermineNextLiveState();
    Assert.IsTrue(cell.IsAliveNext);
  }
}

[TestClass]
public class BoardTests {
  string testDir = Directory.GetCurrentDirectory();

  [TestMethod]
  public void BoardTest1() {
    var board = new Board(100, 50, 2);
    Assert.AreEqual(50, board.Columns);
    Assert.AreEqual(25, board.Rows);
  }

  [TestMethod]
  public void BoardTest2() {
    var board = new Board(3, 3, 1);
    var centerCell = board.Cells[1, 1];
    Assert.AreEqual(8, centerCell.neighbors.Count);
  }

  [TestMethod]
  public void BoardTest3() {
    var board = new Board(3, 3, 1);
    var cornerCell = board.Cells[0, 0];
    Assert.IsTrue(cornerCell.neighbors.Contains(board.Cells[2, 2]));
    Assert.IsTrue(cornerCell.neighbors.Contains(board.Cells[0, 2]));
    Assert.IsTrue(cornerCell.neighbors.Contains(board.Cells[2, 0]));
  }

  [TestMethod]
  public void BoardTest4() {
    var board = new Board(100, 100, 1, 0.3);
    double aliveRatio = board.Cells.Cast<Cell>().Count(c => c.IsAlive) / (double)(10000);
    Assert.IsTrue(aliveRatio >= 0.25 && aliveRatio <= 0.35);
  }

  [TestMethod]
  public void BoardTest5() {
    var board = new Board(100, 100, 1);
    var exception = Assert.ThrowsException<Exception>(() => board.LoadPattern("somepattern"));
    Assert.AreEqual(exception.Message, "File somepattern not found");
  }

  [TestMethod]
  public void BoardTest6() {
    var board = new Board(100, 100, 1);
    var exception = Assert.ThrowsException<Exception>(() => board.SaveToFile("somepattern"));
    Assert.AreEqual(exception.Message, "File somepattern not found");
  }

  [TestMethod]
  public void BoardTest7() {
    var board = new Board(100, 100, 1);
    var exception = Assert.ThrowsException<Exception>(() => board.LoadFromFile("somepattern"));
    Assert.AreEqual(exception.Message, "File somepattern not found");
  }

  [TestMethod]
  public void BoardTest8() {
    string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");
    string patternPath = Path.Combine(patternsDir, "glider.txt");

    var board = new Board(10, 10, 1, 0);
    board.LoadPattern(patternPath);

    Assert.IsTrue(board.Cells[1, 0].IsAlive);
    Assert.IsTrue(board.Cells[2, 1].IsAlive);
    Assert.IsTrue(board.Cells[0, 2].IsAlive);
    Assert.IsTrue(board.Cells[1, 2].IsAlive);
    Assert.IsTrue(board.Cells[2, 2].IsAlive);
  }

  [TestMethod]
  public void BoardTest9() {
    string lifeDir = Path.Combine(testDir, "..", "..", "..", "..", "Life");
    string boardPath = Path.Combine(lifeDir, "board.txt");

    var board = new Board(50, 20, 1);

    board.LoadFromFile(boardPath);
    board.SaveToFile(boardPath);
    Assert.IsTrue(board.Cells != null);
    Assert.IsTrue(board.Cells.Length != 0);
  }
}

[TestClass]
public class AnalyzerTests {
  string testDir = Directory.GetCurrentDirectory();

  [TestMethod]
  public void AnalyzerTest1() {
    var board = new Board(5, 5, 1, 0);
    board.Cells[1, 1].IsAlive = true;
    board.Cells[1, 2].IsAlive = true;

    var clusters = ClusterAnalyzer.FindClusters(board);
    Console.WriteLine(clusters.Count);
    Assert.AreEqual(1, clusters.Count);
    Assert.AreEqual(2, clusters[0].Count);
  }

  [TestMethod]
  public void AnalyzerTest2() {
    var board = new Board(5, 5, 1, 0);
    board.Cells[1, 1].IsAlive = true;
    board.Cells[4, 4].IsAlive = true;

    var clusters = ClusterAnalyzer.FindClusters(board);
    Assert.AreEqual(2, clusters.Count);
    Assert.AreEqual(1, clusters[0].Count);
    Assert.AreEqual(1, clusters[1].Count);
  }

  [TestMethod]
  public void AnalyzerTest3() {
    string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");

    var cluster = new HashSet<(int, int)> { (1, 0), (1, 1), (1, 2) };
    string type = ClusterAnalyzer.ClassifyCluster(cluster, patternsDir);
    Assert.AreEqual("blinker", type);
  }
}

[TestClass]
public class SettingsTests {
  string testDir = Directory.GetCurrentDirectory();

  [TestMethod]
  public void SettingsTest() {
    var original = new GameSettings {
      Width = 50,
      Height = 20,
      CellSize = 1,
      LiveDensity = 0.7,
      Delay = 100
    };

    string lifeDir = Path.Combine(testDir, "..", "..", "..", "..", "Life");
    string settingsPath = Path.Combine(lifeDir, "config.json");
    string json = File.ReadAllText(settingsPath);
    var loaded = JsonSerializer.Deserialize<GameSettings>(json);

    Assert.AreEqual(original.Width, loaded?.Width);
    Assert.AreEqual(original.Height, loaded?.Height);
    Assert.AreEqual(original.CellSize, loaded?.CellSize);
    Assert.AreEqual(original.LiveDensity, loaded?.LiveDensity);
    Assert.AreEqual(original.Delay, loaded?.Delay);
  }
}

[TestClass]
public class IterativeTests() {
  string testDir = Directory.GetCurrentDirectory();

  [TestMethod]
  public void GliderTest() {
    string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");
    var board = new Board(10, 10, 1, 0);
    board.LoadPattern(Path.Combine(patternsDir, "glider.txt"));

    var initialPositions = board.Cells.Cast<Cell>().Count(c => c.IsAlive);

    for (int i = 0; i < 4; i++)
      board.Advance();

    Assert.AreEqual(initialPositions, board.Cells.Cast<Cell>().Count(c => c.IsAlive));
  }

  [TestMethod]
  public void BlockTest() {
    string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");
    var board = new Board(4, 4, 1, 0);
    board.LoadPattern(Path.Combine(patternsDir, "block.txt"));

    var before = board.Cells.Cast<Cell>().Where(c => c.IsAlive).ToList();
    board.Advance();
    var after = board.Cells.Cast<Cell>().Where(c => c.IsAlive).ToList();

    CollectionAssert.AreEquivalent(before, after);
  }
}