using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

class Program
{
    static List<(int x, int y, char character, ConsoleColor color)> blocks = new List<(int x, int y, char character, ConsoleColor color)>();
    static ConsoleColor currentColor = ConsoleColor.White;
    static char currentChar = '█';
    static string[] menuItems = { "Új rajz", "Szerkesztés", "Törlés", "Kilépés" };
    static int selectedItem = 0;

    static void Main()
    {
        using (var db = new DrawingContext())
        {
            db.Database.EnsureCreated(); //van adatbazis? biztosra megy
        }

        bool exit = false;

        while (!exit)
        {
            DrawFrame(Console.WindowWidth, Console.WindowHeight - 1);
            DrawMenu(Console.WindowWidth, Console.WindowHeight - 1);
            HandleInput();
        }
    }

    static void SaveDrawingToDatabase(string drawingName, List<(int x, int y, char character, ConsoleColor color)> blocks)
    {
        using (var db = new DrawingContext())
        {
            string drawingData = string.Join(";", blocks.Select(b => $"{b.x},{b.y},{b.character},{(int)b.color}"));
            var drawing = new Drawing { Name = drawingName, Data = drawingData };

            db.Drawings.Add(drawing);
            db.SaveChanges();
            Console.WriteLine("Drawing saved successfully.");
        }
    }

    static List<(int x, int y, char character, ConsoleColor color)> LoadDrawingFromDatabase(string drawingName)
    {
        using (var db = new DrawingContext())
        {
            var drawing = db.Drawings.FirstOrDefault(d => d.Name == drawingName);
            if (drawing == null)
            {
                Console.WriteLine("Drawing not found.");
                return new List<(int x, int y, char character, ConsoleColor color)>();
            }

            List<(int x, int y, char character, ConsoleColor color)> loadedBlocks = new List<(int x, int y, char character, ConsoleColor color)>();
            foreach (string block in drawing.Data.Split(';'))
            {
                string[] parts = block.Split(',');
                int x = int.Parse(parts[0]);
                int y = int.Parse(parts[1]);
                char character = parts[2][0];
                ConsoleColor color = (ConsoleColor)int.Parse(parts[3]);
                loadedBlocks.Add((x, y, character, color));
            }

            return loadedBlocks;
        }
    }

    static void DrawFrame(int width, int height)
    {
        Console.Clear();
        Console.SetCursorPosition(0, 1);
        Console.Write("╔");
        for (int i = 1; i < width - 1; i++) Console.Write("═");
        Console.Write("╗");
        for (int i = 1; i < height - 1; i++)
        {
            Console.SetCursorPosition(0, i + 1);
            Console.Write("║");
            Console.SetCursorPosition(width - 1, i + 1);
            Console.Write("║");
        }
        Console.SetCursorPosition(0, height);
        Console.Write("╚");
        for (int i = 1; i < width - 1; i++) Console.Write("═");
        Console.SetCursorPosition(width - 1, height);
        Console.Write("╝");
    }

    static void DrawMenu(int width, int height)
    {
        int startX = (width - 20) / 2;
        int startY = (height - menuItems.Length * 2) / 2;

        for (int i = 0; i < menuItems.Length; i++)
        {
            Console.SetCursorPosition(startX, startY + i * 2);

            if (i == selectedItem)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write($">> {menuItems[i]} <<");
                Console.ResetColor();
            }
            else
            {
                Console.Write($"   {menuItems[i]}   ");
            }
        }
    }

    static void HandleInput()
    {
        ConsoleKeyInfo keyInfo = Console.ReadKey(true);

        if (keyInfo.Key == ConsoleKey.UpArrow)
        {
            selectedItem = (selectedItem - 1 + menuItems.Length) % menuItems.Length;
        }
        else if (keyInfo.Key == ConsoleKey.DownArrow)
        {
            selectedItem = (selectedItem + 1) % menuItems.Length;
        }
        else if (keyInfo.Key == ConsoleKey.Enter)
        {
            switch (selectedItem)
            {
                case 0: // Új rajz
                    StartDrawing();
                    break;
                case 1: // Szerkesztés
                    EditDrawing();
                    break;
                case 2: // Törlés
                    DeleteDrawing();
                    break;
                case 3: // Kilépés
                    Environment.Exit(0);
                    break;
            }
        }
    }

    static void StartDrawing()
    {
        Console.Clear();
        Console.WriteLine("Adj egy nevet a rajzodnak:");
        string drawingName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(drawingName))
        {
            Console.WriteLine("Érvénytelen név. Visszaviszlek a főmenübe.");
            Console.ReadKey();
            return;
        }

        blocks.Clear();
        int width = Console.WindowWidth;
        int height = Console.WindowHeight - 2;
        DrawFrame(width, height);

        int x = width / 2;
        int y = height / 2;

        Console.CursorVisible = false;

        while (true)
        {
            if (Console.WindowWidth != width || Console.WindowHeight != height + 2)
            {
                width = Console.WindowWidth;
                height = Console.WindowHeight - 2;
                DrawFrame(width, height);
                x = width / 2;
                y = height / 2;
            }

            DisplayCurrentSelection();

            Console.SetCursorPosition(x, y + 1);

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (y > 1) y--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (y < height - 2) y++;
                        break;
                    case ConsoleKey.LeftArrow:
                        if (x > 1) x--;
                        break;
                    case ConsoleKey.RightArrow:
                        if (x < width - 2) x++;
                        break;

                    case ConsoleKey.Spacebar:
                        PlaceBlock(x, y, currentChar, currentColor);
                        break;

                    case ConsoleKey.D1:
                        currentColor = ConsoleColor.Red;
                        break;
                    case ConsoleKey.D2:
                        currentColor = ConsoleColor.Green;
                        break;
                    case ConsoleKey.D3:
                        currentColor = ConsoleColor.Blue;
                        break;
                    case ConsoleKey.D4:
                        currentColor = ConsoleColor.Yellow;
                        break;

                    case ConsoleKey.Escape:
                        if (blocks.Count > 0)
                        {
                            SaveDrawingToDatabase(drawingName, blocks);
                        }
                        return;
                }
            }
        }
    }

    static void EditDrawing()
    {
        Console.Clear();
        using (var db = new DrawingContext())
        {
            var drawingNames = db.Drawings.Select(d => d.Name).ToList();
            if (!drawingNames.Any())
            {
                Console.SetCursorPosition(Console.WindowWidth / 2 - 8, Console.WindowHeight / 2);
                Console.WriteLine("Nincs mentett rajzod.");
                Console.ReadKey();
                return;
            }

            int selectedDrawingIndex = 0;

            while (true)
            {
                Console.Clear();
                DrawFrame(Console.WindowWidth, Console.WindowHeight - 2);

                Console.SetCursorPosition(Console.WindowWidth / 2 - 10, Console.WindowHeight / 2 - drawingNames.Count / 2 - 1);
                Console.WriteLine("Válaszd ki a szerkesztendő rajzod:");

                for (int i = 0; i < drawingNames.Count; i++)
                {
                    Console.SetCursorPosition(Console.WindowWidth / 2 - drawingNames[i].Length / 2, Console.WindowHeight / 2 - drawingNames.Count / 2 + i);
                    if (i == selectedDrawingIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"{i + 1}. {drawingNames[i]}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"{i + 1}. {drawingNames[i]}");
                    }
                }

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    selectedDrawingIndex = (selectedDrawingIndex - 1 + drawingNames.Count) % drawingNames.Count;
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    selectedDrawingIndex = (selectedDrawingIndex + 1) % drawingNames.Count;
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    string selectedDrawing = drawingNames[selectedDrawingIndex];
                    blocks = LoadDrawingFromDatabase(selectedDrawing);
                    Console.Clear();
                    DrawFrame(Console.WindowWidth, Console.WindowHeight - 2);

                    foreach (var block in blocks)
                    {
                        Console.SetCursorPosition(block.x, block.y + 1);
                        Console.ForegroundColor = block.color;
                        Console.Write(block.character);
                    }

                    currentChar = blocks[0].character;
                    currentColor = blocks[0].color;

                    StartDrawing();
                    return;
                }
                else if (keyInfo.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }
        }
    }

    static void DeleteDrawing()
    {
        Console.Clear();
        using (var db = new DrawingContext())
        {
            var drawingNames = db.Drawings.Select(d => d.Name).ToList();
            if (!drawingNames.Any())
            {
                Console.SetCursorPosition(Console.WindowWidth / 2 - 8, Console.WindowHeight / 2);
                Console.WriteLine("Nincs mentett rajzod.");
                Console.ReadKey();
                return;
            }

            int selectedDrawingIndex = 0;

            while (true)
            {
                Console.Clear();
                DrawFrame(Console.WindowWidth, Console.WindowHeight - 2);

                Console.SetCursorPosition(Console.WindowWidth / 2 - 10, Console.WindowHeight / 2 - drawingNames.Count / 2 - 1);
                Console.WriteLine("Melyik rajzod törölnéd?:");

                for (int i = 0; i < drawingNames.Count; i++)
                {
                    Console.SetCursorPosition(Console.WindowWidth / 2 - drawingNames[i].Length / 2, Console.WindowHeight / 2 - drawingNames.Count / 2 + i);
                    if (i == selectedDrawingIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"{i + 1}. {drawingNames[i]}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"{i + 1}. {drawingNames[i]}");
                    }
                }

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    selectedDrawingIndex = (selectedDrawingIndex - 1 + drawingNames.Count) % drawingNames.Count;
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    selectedDrawingIndex = (selectedDrawingIndex + 1) % drawingNames.Count;
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    string selectedDrawing = drawingNames[selectedDrawingIndex];
                    var drawingToDelete = db.Drawings.FirstOrDefault(d => d.Name == selectedDrawing);
                    if (drawingToDelete != null)
                    {
                        db.Drawings.Remove(drawingToDelete);
                        db.SaveChanges();
                        Console.Clear();
                        Console.WriteLine($"Rajz '{selectedDrawing}' törölve.");
                        Console.ReadKey();
                        return;
                    }
                }
                else if (keyInfo.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }
        }
    }

    static void DisplayCurrentSelection()
    {
        Console.SetCursorPosition(1, 0);
        Console.Write("Kiválasztott karakter: ");
        Console.ForegroundColor = currentColor;
        Console.Write($"'{currentChar}'");
        Console.ResetColor();
    }

    static void PlaceBlock(int x, int y, char character, ConsoleColor color)
    {
        blocks.Add((x, y, character, color));
        Console.SetCursorPosition(x, y + 1);
        Console.ForegroundColor = color;
        Console.Write(character);
    }
}
