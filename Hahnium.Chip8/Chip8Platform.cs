using System;

namespace Hahnium.Chip8
{
    public class Chip8Platform
    {
        public Chip8Cpu cpu;
        public Chip8Apu apu;
        public Chip8Ppu ppu;
        public Memory<byte> ram;

        public Chip8Platform(ReadOnlyMemory<byte> romImage)
        {
            this.ram = new Memory<byte>(new byte[0x1000]);
            romImage.CopyTo(this.ram.Slice(0x200));

            this.cpu = new Chip8Cpu(this);
            this.apu = new Chip8Apu(this);
            this.ppu = new Chip8Ppu(this);
        }

        public void Cycle()
        {
            this.ppu.Cycle();
            this.cpu.Cycle();
        }
    }
}