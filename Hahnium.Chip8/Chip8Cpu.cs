using System;
using System.Buffers;
using System.Data;

namespace Hahnium.Chip8
{
    internal class Chip8Cpu
    {
        private const ushort OperandsMask = 0x0fff;
        private Memory<byte> ram;
        private MemoryHandle memoryhandle;
        byte[] registers = new byte[0x10];
        ushort address;
        ushort pc = 0x200;
        ushort[] stack = new ushort[48];
        byte sp = 0;

        public Chip8Cpu(Memory<byte> ram)
        {
            this.ram = ram;
            this.memoryhandle = ram.Pin();
        }

        public unsafe void Tick()
        {
            byte* memory = (byte*)memoryhandle.Pointer;
            ushort opcode = (ushort)((*(memory + pc++) << 8) | *(memory + pc++));

            byte miniop = (byte)(opcode >> 12);
            ushort operands = (ushort)(opcode & OperandsMask);

            switch (miniop)
            {
                case 0:
                    Op0(operands);
                    break;
                case 1:
                    Op1(operands);
                    break;
                case 2:
                    Op2(operands);
                    break;
                case 10:
                    OpA(operands);
                    break;
                default: throw new NotImplementedException();
            }
        }

        private void OpA(ushort operands)
        {
            // Sets I to the address NNN
            this.address = operands;
        }

        private void Op2(ushort operands)
        {
            // Calls subroutine at NNN.
            // TODO: Figure out stack bullshit
            stack[sp++] = this.pc;
            stack[sp++] = this.address;
            this.pc = operands;
        }

        private void Op1(ushort operands)
        {
            // Jumps to address NNN.
            // 
            this.pc = operands;
        }

        private void Op0(ushort operands)
        {
            if (operands == 0x00e0)
            {
                //Clears the screen.
                Console.Clear();
            }
            else if (operands == 0x00ee)
            {
                // TODO: return
            }
        }
    }
}