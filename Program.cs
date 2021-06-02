using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ImGuiNET;

namespace marine
{
    class Program
    {
        // Задание путей
        static readonly string ProgramName = "MarineLab";
        static readonly string ExportPath = "export.json";
        static readonly string ImagePath = "fig.png";
        static readonly string ConfigPath = "colors.ini";
        static void Main(string[] args)
        {
            // Генерация начальной карты
            var g = new Game();
            g.Export(ExportPath);
            Pyplot.MakePlot(ExportPath, ImagePath, ConfigPath);

            // Создание пользовательского интерфейса
            var gui = new Gui(ImagePath, ConfigPath);

            // Обработчики ивентов нажатия кнопок пользовательского интерфейса
            gui.GenerateButtonPressed += () =>
            {
                if (gui.GenerateSemaphore.WaitOne(25))
                {
                    var t = Task.Run(() =>
                    {
                        gui.ToggleLoadingText(true);
                        g = new Game();
                        g.Export(ExportPath);
                        Pyplot.MakePlot(ExportPath, ImagePath, ConfigPath);
                        gui.LoadImage();
                        gui.ToggleLoadingText(false);
                        gui.GenerateSemaphore.Release();
                    });
                }
            };
            gui.SaveColorsButtonPressed += () =>
            {
                if (gui.GenerateSemaphore.WaitOne())
                {
                    var t = Task.Run(() =>
                    {
                        using (var s = new StreamWriter(ConfigPath, false))
                        {
                            s.Write(gui.ColorsToString());
                        }
                        gui.GenerateSemaphore.Release();
                    });
                }
            };

            // Запуск пользовательского интерфейса
            gui.Init(ProgramName, 700, 600);
        }

        // Пользовательский интерфейс
        class Gui : ImGuiWindow
        {
            static bool _mainWindow;
            static bool _configWindow;
            static System.IntPtr _image;
            static string _loadingText = "";
            static Vector3[] _colors;
            public static string ImagePath { get; private set; }
            public static string ConfigPath { get; private set; }

            public delegate void Generate();
            public event Generate GenerateButtonPressed;
            public delegate void SaveColors();
            public event SaveColors SaveColorsButtonPressed;
            public volatile Semaphore GenerateSemaphore = new Semaphore(1, 1);
            public Gui(string imgPath, string configPath)
            {
                ImagePath = imgPath;
                ConfigPath = configPath;

                // Задание начальных цветов
                _colors = new Vector3[4];
                try
                {
                    using(var s = new StreamReader(ConfigPath)) 
                        ReadColors(s);
                }
                catch
                {
                    using(var s = new StreamWriter(ConfigPath, false))
                    {
                        s.Write("FF000000FF00FFFF000000FF");
                    }
                    ReadColors(new StreamReader(ConfigPath));
                }
            }

            void ReadColors(StreamReader s)
            {
                // Чтение цветов из файла
                for (int i = 0; i < 4; i++)
                {
                    _colors[i] = new Vector3();
                    char[] hex = new char[6];
                    s.ReadBlock(hex, 0, 6);
                    string hexs = new String(hex);
                    _colors[i].X = Convert.ToInt32(hexs.Substring(0, 2), 16) / 255f;
                    _colors[i].Y = Convert.ToInt32(hexs.Substring(2, 2), 16) / 255f;
                    _colors[i].Z = Convert.ToInt32(hexs.Substring(4, 2), 16) / 255f;
                }
            }

            public string ColorsToString()
            {
                // Перевод цветов в 16-ричную систему
                string s = "";
                foreach(var color in _colors)
                {
                    s += ((int)Math.Round(color.X * 255)).ToString("X2");
                    s += ((int)Math.Round(color.Y * 255)).ToString("X2");
                    s += ((int)Math.Round(color.Z * 255)).ToString("X2");
                }
                return s;
            }

            public void ToggleLoadingText(bool isLoading)
            {
                // Изменение состояния строки загрузки новой карты
                if (isLoading) _loadingText = "Loading";
                else _loadingText = "Loaded";
            }

            protected override void SubmitUI()
            {
                // Основное окно
                ImGui.SetNextWindowPos(new Vector2(0, 0));
                ImGui.SetNextWindowSize(new Vector2(_window.Width, _window.Height));
                ImGui.Begin("ImGui Window",ref _mainWindow ,ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBringToFrontOnFocus);
                if (ImGui.Button("Colors")) _configWindow = true;
                ImGui.SameLine();
                if (ImGui.Button("Generate")) GenerateButtonPressed?.Invoke();
                ImGui.SameLine();
                ImGui.Text(_loadingText);
                ImGui.Image(_image, new Vector2(640, 480));
                ImGui.End();
                if (_configWindow) ConfigColor();
            }

            void ConfigColor()
            {
                // Окно изменения цветов
                ImGui.SetNextWindowPos(new Vector2(_window.Width / 2 - 150, _window.Height / 2 - 100));
                ImGui.SetNextWindowSize(new Vector2(300, 200));
                ImGui.Begin("Color Options", ref _configWindow, ImGuiWindowFlags.NoResize);
                ImGui.ColorEdit3("1", ref _colors[0]);
                ImGui.ColorEdit3("2", ref _colors[1]);
                ImGui.ColorEdit3("3", ref _colors[2]);
                ImGui.ColorEdit3("4", ref _colors[3]);
                if(ImGui.Button("Save"))
                {
                    SaveColorsButtonPressed.Invoke();
                    _configWindow = false;
                }
                ImGui.End();
            }

            protected override void Preload()
            {
                LoadImage();
            }

            public void LoadImage()
            {
                _image = NewImage(ImagePath);
            }
        }

        // Запуск Python скрипта, создающего график
        static class Pyplot
        {
            static string _pathPy = "plot.py";
            static string _condaEnv = "base";

            public static void MakePlot(string jsonPath, string imagePath, string colorsPath)
            {
                var cmd = new Process();
                var info = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = "./",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                cmd.StartInfo = info;
                cmd.Start();

                using (var s = cmd.StandardInput)
                {
                    s.WriteLine("conda activate {0}", _condaEnv);
                    s.WriteLine("python {0} {1} {2} {3}", _pathPy, jsonPath, imagePath, colorsPath);
                }
                cmd.WaitForExit();
            }
        }

        // Генерация карты морского боя
        class Game
        {
            int[][] Map = new int[10][];
            int[] Boats = new int[] { 1, 2, 3, 4 };

            public Game()
            {
                for(int i = 0; i < 10; i++) Map[i] = new int[10];

                GenerateMap();
            }
            delegate bool CheckLineAngleDelegate(int offset);
            bool IsPositionFree(int x, int y, int size, bool angle)
            {
                // Проверка клетки на занятость
                bool CheckLine(int xl, int yl)
                {
                    int xx = xl, yy = yl;
                    bool free = true;
                    for (int i = 0; i < size + 2; i++)
                    {
                        if (angle) xx = xl + i;
                        else yy = yl + i;
                        if (xx > 10 || yy > 10) return false;
                        free &= xx < 0 || xx == 10 || yy < 0 || yy == 10 || Map[xx][yy] == 0;
                    }
                    return free;
                }
                CheckLineAngleDelegate check;
                if (angle) check = (yOff) => CheckLine(x - 1, y + yOff);
                else check = (xOff) => CheckLine(x + xOff, y - 1);
                return Enumerable.Range(-1, 3).Aggregate(true, (acc, i) => acc &= check.Invoke(i));
            }
            void SetPositions(byte value, int x, int y, int size, bool angle)
            {
                // Запись позиции
                for(int i = 0; i < size; i++)
                {
                    if (angle) Map[x + i][y] = value;
                    else Map[x][y + i] = value;
                }
            }

            public int[][] GetMap()
            {
                return Map;
            }
            void GenerateMap()
            {
                // Генерация карты
                var r = new Random();
                byte size = Convert.ToByte(Boats.Length);
                foreach(var boatNum in Boats)
                {
                    for(int i = 0; i < boatNum; i++)
                    {
                        bool set = false;
                        while(!set)
                        {
                            int x = r.Next(10);
                            int y = r.Next(10);

                            bool[] rotation = r.Next(2) == 1 ? new bool[] { true, false } : new bool[] { false, true };

                            foreach(bool b in rotation)
                            {
                                if (IsPositionFree(x, y, size, b))
                                {
                                    SetPositions(size, x, y, size, b);
                                    set = true;
                                }
                            }
                        }
                    }
                    size--;
                }
            }
            public void Export(string fileName)
            {
                // Экспорт карты в JSON файл
                string json = JsonConvert.SerializeObject(GetMap());
                using (var s = new JsonTextWriter(new StreamWriter(fileName, false)))
                {
                    s.WriteRaw(json);
                }
            }

            public override string ToString()
            {
                // Запись карты в строку
                string s = "";
                for (int y = 0; y < 10; y++)
                    s += GetMap()[y].Aggregate("", (acc, x) => acc + x.ToString() + " ") + "\n";
                return s;
            }
        }
    }
}
