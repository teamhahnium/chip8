using System;

namespace Hahnium.Chip8
{
    internal class Chip8Platform
    {
        private Chip8Cpu cpu;
        private Chip8Apu apu;
        private Chip8Ppu ppu;
        private Memory<byte> ram;

        public Chip8Platform(ReadOnlyMemory<byte> romImage)
        {
            this.ram = new Memory<byte>(new byte[0x1000]);
            romImage.CopyTo(this.ram.Slice(0x200));
            this.cpu = new Chip8Cpu(this.ram);
            this.apu = new Chip8Apu(this.ram);
            this.ppu = new Chip8Ppu(this.cpu, this.ram);
        }

        public void Cycle()
        {
            this.cpu.Cycle();
            this.ppu.Cycle();
        }
    }
}