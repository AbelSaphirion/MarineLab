using System;
using System.Linq;

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
        }

        class Game
        {
            byte[][] Map = new byte[10][];
            int[] Boats = new int[] { 1, 2, 3, 4 };

            public Game()
            {
                for(int i = 0; i < 10; i++) Map[i] = new byte[10];

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

            public byte[][] GetMap()
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
        }
    }
}
