using System;

namespace Hahnium.Chip8
{
    public class Chip8Platform
    {
        public Chip8Cpu cpu;
        public Chip8Apu apu;
        public Chip8Ppu ppu;
        public Memory<byte> ram;

        private ReadOnlyMemory<byte> font = new ReadOnlyMemory<byte>(new byte[] {
            // Font "0"
            0xF0,0x90,0x90,0x90,0xF0,
            // Font "1"
            0x20,0x60,0x20,0x20,0x70,
            // Font "2"
            0xF0,0x10,0xF0,0x80,0xF0,
            // Font "3"
            0xF0,0x10,0xF0,0x10,0xF0,
            // Font "4"
            0x90,0x90,0xF0,0x10,0x10,
            // Font "5"
            0xF0,0x80,0xF0,0x10,0xF0,
            // Font "6"
            0xF0,0x80,0xF0,0x90,0xF0,
            // Font "7"
            0xF0,0x10,0x20,0x40,0x40,
            // Font "8"
            0xF0,0x90,0xF0,0x90,0xF0,
            // Font "9"
            0xF0,0x90,0xF0,0x10,0xF0,
            // Font "A"
            0xF0,0x90,0xF0,0x90,0x90,
            // Font "B"
            0xE0,0x90,0xE0,0x90,0xE0,
            // Font "C"
            0xF0,0x80,0x80,0x80,0xF0,
            // Font "D"
            0xE0,0x90,0x90,0x90,0xE0,
            // Font "E"
            0xF0,0x80,0xF0,0x80,0xF0,
            // Font "F"
            0xF0,0x80,0xF0,0x80,0x80,
        });

        public Chip8Platform(ReadOnlyMemory<byte> romImage)
        {
            this.ram = new Memory<byte>(new byte[0x1000]);
            this.font.CopyTo(this.ram);
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