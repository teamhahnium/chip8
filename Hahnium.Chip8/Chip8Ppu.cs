using System;
using System.Diagnostics;

namespace Hahnium.Chip8
{
    public unsafe class Chip8Ppu
    {
        private const char BlockUpper = '▀';
        private const char BlockLower = '▄';
        private const char BlockFull = '█';
        private char[] Blocks = new[] { ' ', BlockLower, BlockUpper, BlockFull };
        private const int DisplayWidth = 64;
        private const int DisplayHeight = 32;
        public byte[] frameBuffer = new byte[DisplayWidth * DisplayHeight];
        private Chip8Platform platform;
        private Chip8Cpu cpu => this.platform.cpu;

        public Chip8Ppu(Chip8Platform platform)
        {
            this.platform = platform;
            /* Display design *
            ╔══════════════════════════════════════════════════════════════╗ Addr  Op
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                   Display 64x32 (64c, 16r)                   ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ║                                                              ║ #FFFF $FFFF
            ╚══════════════════════════════════════════════════════════════╝ #FFFF $FFFF
            V0 $FF   V4 $FF   V8 $FF   VC $FF   A  #FFFF
            V1 $FF   V5 $FF   V9 $FF   VD $FF   PC #FFFF
            V2 $FF   V6 $FF   VA $FF   VE $FF   
            V3 $FF   V7 $FF   VB $FF   VF $FF   SP #FFFF
            */
            Console.SetWindowSize(80, 25);
            Console.CursorVisible = false;
        }

        int cycleCount = 0;
        Stopwatch timer = Stopwatch.StartNew();

        internal void Cycle()
        {
            // Test animation
            //for (int y = 0; y < DisplayHeight; y++)
            //{
            //    for (int x = 0; x < DisplayWidth; x++)
            //    {
            //        int offset = (y * DisplayWidth) + x;
            //        frameBuffer[offset] = (byte)(((offset + cycleCount + y) % 8) == 1 ? 1 : 0);
            //    }
            //}

            Console.SetCursorPosition(0, 0);
            Console.Write(@$"{RenderRow(0)} Addr  Op
{RenderRow(2)}  {RenderProgramCounter(-7)}
{RenderRow(4)}  {RenderProgramCounter(-6)}
{RenderRow(6)}  {RenderProgramCounter(-5)}
{RenderRow(8)}  {RenderProgramCounter(-4)}
{RenderRow(10)}  {RenderProgramCounter(-3)}
{RenderRow(12)}  {RenderProgramCounter(-2)}
{RenderRow(14)}  {RenderProgramCounter(-1)}
{RenderRow(16)} >{RenderProgramCounter(0)}
{RenderRow(18)}  {RenderProgramCounter(1)}
{RenderRow(20)}  {RenderProgramCounter(2)}
{RenderRow(22)}  {RenderProgramCounter(3)}
{RenderRow(24)}  {RenderProgramCounter(4)}
{RenderRow(26)}  {RenderProgramCounter(5)}
{RenderRow(28)}  {RenderProgramCounter(6)}
{RenderRow(30)}  {RenderProgramCounter(7)}
{RenderRegister(0)}   {RenderRegister(4)}   {RenderRegister(8)}   {RenderRegister(12)}   {RenderAddress()}
{RenderRegister(1)}   {RenderRegister(5)}   {RenderRegister(9)}   {RenderRegister(13)}   {RenderPC()}
{RenderRegister(2)}   {RenderRegister(6)}   {RenderRegister(10)}   {RenderRegister(14)}   {RenderSP()}
{RenderRegister(3)}   {RenderRegister(7)}   {RenderRegister(11)}   {RenderRegister(15)}
FPS {++cycleCount / timer.Elapsed.TotalSeconds}");

            if (timer.Elapsed.TotalSeconds > 1.0)
            {
                timer = Stopwatch.StartNew();
                cycleCount = 0;
            }
        }

        public bool Draw(byte xPosition, byte yPosition, Memory<byte> spriteMemory)
        {
            bool bitFlipped = false;
            using var spriteHandle = spriteMemory.Pin();
            var sprite = (byte*)spriteHandle.Pointer;

            for (int y = yPosition; y < yPosition + spriteMemory.Length; y++)
            {
                byte spriteLine = *sprite;

                for (int x = xPosition; x < xPosition + 8; x++)
                {
                    byte pixel = frameBuffer[(y * DisplayWidth) + x];
                    var spritePixel = (spriteLine & 0b1000_0000) >> 7;
                    spriteLine <<= 1;

                    if (!bitFlipped && pixel > 0 && spritePixel > 0)
                    {
                        bitFlipped = true;
                    }

                    frameBuffer[(y * DisplayWidth) + x] = (byte)(pixel ^ spritePixel);
                }
            }

            return bitFlipped;
        }

        private unsafe string RenderRegister(int register) => $"V{register:X} ${this.cpu.Registers.Variables[register]:X2}";

        private string RenderAddress() => $"A  #{this.cpu.Registers.Index:X4}";

        private string RenderPC() => $"PC #{this.cpu.Registers.PC:X4}";

        private string RenderSP() => $"SP #{this.cpu.sp:X4}";

        private unsafe string RenderProgramCounter(short pcOffset)
        {
            ushort address = (ushort)((pcOffset << 1) + this.cpu.Registers.PC);
            var value = *(ushort*)(((byte*)this.cpu.memoryhandle.Pointer) + address);
            value = (ushort)((value << 8) | (value >> 8));
            return $"#{address:X4} ${value:X4}";
        }

        private string RenderRow(int rowIndex)
        {
            var row = new char[DisplayWidth];

            for (int i = 0; i < DisplayWidth; i++)
            {
                byte upper = frameBuffer[(DisplayWidth * rowIndex) + i];
                byte lower = frameBuffer[(DisplayWidth * (rowIndex + 1)) + i];
                row[i] = Blocks[(upper << 1) | lower];
            }

            return new string(row);
        }
    }
}