using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Chip8
{
    public unsafe class Chip8Ppu
    {
        private const byte MSB = 0b1000_0000;
        private const char BlockUpper = '▀';
        private const char BlockLower = '▄';
        private const char BlockFull = '█';
        private const int DisplayWidth = 64;
        private const int DisplayHeight = 32;
        private static readonly char[] Blocks = new[] { ' ', BlockLower, BlockUpper, BlockFull };
        private static readonly Vector<byte> spritePixelMask;

        private Memory<byte> frameBuffer = new byte[DisplayWidth * (DisplayHeight) + Vector<byte>.Count]; // HACK: Extra buffer for overflow handling
        private MemoryHandle frameBufferHandle;
        private Chip8Platform platform;
        private Chip8Cpu cpu => this.platform.cpu;

        public bool TestMode { get; set; } = false;

        static Chip8Ppu()
        {
            if (Vector<byte>.Count < 8)
            {
                throw new PlatformNotSupportedException("Wow, your CPU is too bad to support this 1970s-era language");
            }

            var mask = new Span<byte>(new byte[Vector<byte>.Count]);
            for (int i = 0; i < 8; i++)
            {
                mask[i] = (byte)(MSB >> i);
            }

            spritePixelMask = new Vector<byte>(mask);
        }

        public Chip8Ppu(Chip8Platform platform)
        {
            this.frameBufferHandle = this.frameBuffer.Pin();
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
            Console.Clear();
        }

        int cycleCount = 0;
        int frameCount = 0;
        Stopwatch timer = Stopwatch.StartNew();
        Stopwatch lastFrame = Stopwatch.StartNew();
        TimeSpan frameTime = TimeSpan.FromSeconds(1.0 / 60.0);

        internal void Cycle()
        {
            // Only paint the screen every 8th cycle to get ~480cpu cycles per second
            if (cycleCount++ % 8 != 0)
            {
                return;
            }

            frameCount++;

            // Decrement delay counter
            if (this.cpu.Registers.Delay > 0)
            {
                this.cpu.Registers.Delay--;
            }

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
{RenderRegister(0)}   {RenderRegister(4)}   {RenderRegister(8)}   {RenderRegister(12)}   {RenderAddress()}   {RenderTimer("CPU", platform.cpuTimer)}
{RenderRegister(1)}   {RenderRegister(5)}   {RenderRegister(9)}   {RenderRegister(13)}   {RenderPC()}   {RenderTimer("PPU", platform.ppuTimer)}
{RenderRegister(2)}   {RenderRegister(6)}   {RenderRegister(10)}   {RenderRegister(14)}   {RenderSP()}
{RenderRegister(3)}   {RenderRegister(7)}   {RenderRegister(11)}   {RenderRegister(15)}   {RenderDelay()}
Clock {cycleCount / timer.Elapsed.TotalSeconds}
FPS {frameCount / timer.Elapsed.TotalSeconds}");

            // Reset stat counts every second
            if (timer.Elapsed.TotalSeconds > 1.0)
            {
                timer.Restart();
                cycleCount = 0;
                frameCount = 0;
            }

            // Reset cpu/ppu timers every frame
            this.platform.cpuTimer.Reset();
            this.platform.ppuTimer.Reset();

            // Framerate limiter
            while(frameTime > lastFrame.Elapsed)
            {
                Thread.Sleep(0);
            }

            lastFrame.Restart();
        }

        private string RenderTimer(string label, Stopwatch timer) => $"{label}: {timer.ElapsedMilliseconds,4}ms";

        public bool Draw(byte xPosition, byte yPosition, Memory<byte> spriteMemory)
        {
            // Sprites are always 8px wide. Each bit is a pixel.
            bool bitFlipped = false;
            using var spriteHandle = spriteMemory.Pin();
            var sprite = (byte*)spriteHandle.Pointer;

            for (int y = 0; y < spriteMemory.Length; y++)
            {
                var spriteLine = new Vector<byte>(*sprite); // Initializes vector with value of the sprite row
                var selectedPixels = spriteLine & spritePixelMask; // Select each pixel in the line
                var linePixels = Vector.GreaterThan(selectedPixels, Vector<byte>.Zero); // If the pixels are non-zero, then normalize to 0x00/0xFF

                var region = ((byte*)frameBufferHandle.Pointer) + ((((y + yPosition) * DisplayWidth) + xPosition) % (frameBuffer.Length - 1));
                var regionSpan = new Span<byte>(region, Vector<byte>.Count);
                var regionLine = new Vector<byte>(regionSpan);
                if (!bitFlipped) // If a flipped bit has been found, this check can be skipped
                {
                    bitFlipped = Vector.GreaterThanAny(regionLine & linePixels, Vector<byte>.Zero); // Detecting if any set framebuffer pixel will be flipped
                }
                var renderedLine = regionLine ^ linePixels;
                renderedLine.CopyTo(regionSpan); // Copy rendered line back into the frame buffer region

                // Move pointers
                sprite++;
            }

            return bitFlipped;
        }

        public bool DrawOld(byte xPosition, byte yPosition, Memory<byte> spriteMemory)
        {
            bool bitFlipped = false;
            using var spriteHandle = spriteMemory.Pin();
            var sprite = (byte*)spriteHandle.Pointer;
            for (int y = yPosition; y < yPosition + spriteMemory.Length; y++)
            {
                byte spriteLine = *sprite;

                for (int x = xPosition; x < xPosition + 8; x++)
                {
                    int pixelId = (y * DisplayWidth) + x;

                    if (pixelId >= frameBuffer.Length || pixelId < 0)
                    {
                        continue;
                    }

                    byte pixel = *(((byte*)frameBufferHandle.Pointer) + (y * DisplayWidth) + x);
                    var spritePixel = (spriteLine & MSB) >> 7;
                    spriteLine <<= 1;

                    if (!bitFlipped && pixel > 0 && spritePixel > 0)
                    {
                        bitFlipped = true;
                    }

                    *(((byte*)frameBufferHandle.Pointer) + (y * DisplayWidth) + x) = (byte)(pixel ^ spritePixel);
                }

                sprite++;
            }

            return bitFlipped;
        }

        private unsafe string RenderRegister(int register) => $"V{register:X} ${this.cpu.Registers.Variables[register]:X2}";

        private string RenderAddress() => $"A  #{this.cpu.Registers.Index:X4}";

        private string RenderPC() => $"PC #{this.cpu.Registers.PC:X4}";

        private string RenderSP() => $"SP #{this.cpu.sp:X4}";
        private string RenderDelay() => $"D  #{this.cpu.Registers.Delay:X4}";

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
                byte upper = *(((byte*)frameBufferHandle.Pointer) + (DisplayWidth * rowIndex) + i);
                byte lower = *(((byte*)frameBufferHandle.Pointer) + (DisplayWidth * (rowIndex + 1)) + i);
                row[i] = Blocks[(upper & 0x2) | (lower & 0x1)];
            }

            return new string(row);
        }
    }
}