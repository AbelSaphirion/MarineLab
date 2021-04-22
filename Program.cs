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
        static readonly string ProgramName = "MarineLab";
        static readonly string ExportPath = "export.json";
        static readonly string ImagePath = "fig.png";
        static void Main(string[] args)
        {
            var g = new Game();
            g.Export(ExportPath);
            Pyplot.MakePlot(ExportPath, ImagePath);
            var gui = new Gui(ImagePath);
            gui.GenerateButtonPressed += () =>
            {
                if (gui.GenerateSemaphore.WaitOne(25))
                {
                    var t = Task.Run(() =>
                    {
                        gui.ToggleLoadingText(true);
                        g = new Game();
                        g.Export(ExportPath);
                        Pyplot.MakePlot(ExportPath, ImagePath);
                        gui.LoadImage();
                        gui.ToggleLoadingText(false);
                        gui.GenerateSemaphore.Release();
                    });
                }
            };
            gui.Init(ProgramName, 700, 600);
        }

        class Gui : ImGuiWindow
        {
            static bool _mainWindow;
            static System.IntPtr _image;
            static string _loadingText = "";
            public static string ImagePath { get; private set; }

            public delegate void Generate();
            public event Generate GenerateButtonPressed;
            public volatile Semaphore GenerateSemaphore = new Semaphore(1, 1);
            public Gui(string imgPath)
            {
                ImagePath = imgPath;
            }

            public void ToggleLoadingText(bool isLoading)
            {
                if (isLoading) _loadingText = "Loading";
                else _loadingText = "Loaded";
            }

            protected override void SubmitUI()
            {
                ImGui.SetNextWindowPos(new Vector2(0, 0));
                ImGui.SetNextWindowSize(new Vector2(_window.Width, _window.Height));
                ImGui.Begin("ImGui Window",ref _mainWindow ,ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);
                if (ImGui.Button("Generate")) GenerateButtonPressed?.Invoke();
                ImGui.SameLine();
                ImGui.Text(_loadingText);
                ImGui.Image(_image, new Vector2(640, 480));
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

        static class Pyplot
        {
            static string _pathPy = "plot.py";
            static string _condaEnv = "base";

            public static void MakePlot(string jsonPath, string imagePath)
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
                    s.WriteLine("python {0} {1} {2}", _pathPy, jsonPath, imagePath);
                }
                cmd.WaitForExit();
            }
        }

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
                string json = JsonConvert.SerializeObject(GetMap());
                using (var s = new JsonTextWriter(new StreamWriter(fileName, false)))
                {
                    s.WriteRaw(json);
                }
            }

            public override string ToString()
            {
                string s = "";
                for (int y = 0; y < 10; y++)
                    s += GetMap()[y].Aggregate("", (acc, x) => acc + x.ToString() + " ") + "\n";
                return s;
            }
        }
    }
}
