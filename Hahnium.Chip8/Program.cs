using System;
using System.IO;

namespace Hahnium.Chip8
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var romImage = LoadRom(args[0]);
            var platform = new Chip8Platform(romImage);

            while (true)
            {
                platform.Cycle();
            }
        }

        private static ReadOnlyMemory<byte> LoadRom(string path)
        {
            return new ReadOnlyMemory<byte>(File.ReadAllBytes(path));
        }
    }
}
