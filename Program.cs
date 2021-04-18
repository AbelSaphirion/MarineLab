using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;

using static ImGuiNET.ImGuiNative;

namespace marine
{
    class Program
    {
        static void Main(string[] args)
        {
            var g = new Game();
            for (int y = 0; y < 10; y++)
                Console.WriteLine(g.GetMap()[y].Aggregate("", (acc, x) => acc + x.ToString() + " "));
            Console.ReadLine();

            Console.WriteLine(Pyplot.MakePlot(g.Export("export.json")));
            Console.ReadLine();

        }

        static class Pyplot
        {
            static string _pathPy = "plot.py";
            static string _condaEnv = "base";
            static string _pngName = "fig.png";

            public static string MakePlot(string json)
            {
                var cmd = new Process();
                var info = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = "./",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                var output = new List<string>();
                cmd.OutputDataReceived += (sender, args) => output.Add(args.Data);

                cmd.StartInfo = info;
                cmd.Start();

                using (var s = cmd.StandardInput)
                {
                    cmd.BeginOutputReadLine();
                    s.WriteLine("conda activate {0}", _condaEnv);
                    s.WriteLine("python {0} {1} {2}", _pathPy, json, _pngName);
                }
                cmd.WaitForExit();
                string response = output.Last();
                for (int i = 0; i < output.Count - 1; i++)
                {
                    if(output[i].Contains("python"))
                    {
                        response = output[i + 1];
                        break;
                    }
                }
                return response;
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

            public string Export(string fileName)
            {
                string json = JsonConvert.SerializeObject(GetMap());
                using (var s = new JsonTextWriter(new StreamWriter(fileName, false)))
                {
                    s.WriteRaw(json);
                }
                return fileName;
            }
        }
    }
}
