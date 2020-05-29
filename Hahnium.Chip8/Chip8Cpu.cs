using System;
using System.Buffers;

namespace Hahnium.Chip8
{
    public unsafe class Chip8Cpu
    {
        public unsafe struct CpuRegisters
        {
            public fixed byte Variables[0x10];
            public ushort PC;
            public ushort Index;
        }

        private Random rand = new Random();
        private Chip8Platform platform;
        private const ushort OperandsMask = 0x0fff;
        public MemoryHandle memoryhandle;
        public ushort[] stack = new ushort[48];
        public byte sp = 0;
        public CpuRegisters Registers = new CpuRegisters
        {
            PC = 0x200
        };

        public Chip8Cpu(Chip8Platform platform)
        {
            this.platform = platform;
            this.memoryhandle = platform.ram.Pin();
        }

        public unsafe void Cycle()
        {
            byte* memory = (byte*)memoryhandle.Pointer;
            ushort opcode = (ushort)((*(memory + this.Registers.PC++) << 8) | *(memory + this.Registers.PC++));

            ushort operands = (ushort)(opcode & OperandsMask);

            switch (opcode.GetNibble(3))
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
                case 3:
                    Op3(operands);
                    break;
                case 4:
                    Op4(operands);
                    break;
                case 5:
                    Op5(operands);
                    break;
                case 6:
                    Op6(operands);
                    break;
                case 7:
                    Op7(operands);
                    break;
                case 8:
                    Op8(operands);
                    break;
                case 0xA:
                    OpA(operands);
                    break;
                case 0xC:
                    OpC(operands);
                    break;
                case 0xD:
                    OpD(operands);
                    break;
                default: throw new NotImplementedException();
            }
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

        private void Op1(ushort operands)
        {
            // Jumps to address NNN.
            // 
            this.Registers.PC = operands;
        }

        private void Op2(ushort operands)
        {
            // Calls subroutine at NNN.
            // TODO: Figure out stack bullshit
            stack[sp++] = this.Registers.PC;
            this.Registers.PC = operands;
        }

        private void Op3(ushort operands)
        {
            // 3XNN	-> if(Vx==NN)
            // Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block)

            var variableId = operands.GetNibble(2);
            if (this.Registers.Variables[variableId] == operands.GetByte(0))
            {
                this.Registers.PC += 2;
            }
        }

        private void Op4(ushort operands)
        {
            // 4XNN -> if(Vx!=NN)
            // Skips the next instruction if VX doesn't equal NN. (Usually the next instruction is a jump to skip a code block)

            var variableId = operands.GetNibble(2);
            if (this.Registers.Variables[variableId] != operands.GetByte(0))
            {
                this.Registers.PC += 2;
            }
        }

        private void Op5(ushort operands)
        {
            // 5XY0 -> if(Vx==Vy)
            // Skips the next instruction if VX equals VY. (Usually the next instruction is a jump to skip a code block)

            var variableX = operands.GetNibble(2);
            var variableY = operands.GetNibble(1);
            if (this.Registers.Variables[variableX] == this.Registers.Variables[variableY])
            {
                this.Registers.PC += 2;
            }
        }

        private void Op6(ushort operands)
        {
            // Sets VX to NN.
            // 6XNN -> Vx = NN

            var variableId = operands.GetNibble(2);
            this.Registers.Variables[variableId] = operands.GetByte(0);
        }

        private void Op7(ushort operands)
        {
            // 7XNN -> Vx += NN	Adds NN to VX. (Carry flag is not changed)
            var variableId = operands.GetNibble(2);
            var value = operands.GetByte(0);

            this.Registers.Variables[variableId] += value;
        }

        private void Op8(ushort operands)
        {
            var variableX = operands.GetNibble(2);
            var variableY = operands.GetNibble(1);
            var operation = operands.GetNibble(0);

            switch (operation)
            {
                case 2:
                    // Vx = Vx & Vy    Sets VX to VX and VY. (Bitwise AND operation)
                    this.Registers.Variables[variableX] &= this.Registers.Variables[variableY];
                    break;
                case 8:
                    // Vx=Vx|Vy Sets VX to VX or VY. (Bitwise OR operation)
                    this.Registers.Variables[variableX] |= this.Registers.Variables[variableY];
                    break;
                default: throw new NotImplementedException();
            }
        }

        private void OpA(ushort operands)
        {
            // Sets I to the address NNN
            this.Registers.Index = operands;
        }

        private void OpC(ushort operands)
        {
            // CXNN -> Vx=rand()&NN
            // Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN.
            var variableId = operands.GetNibble(2);
            this.Registers.Variables[variableId] = (byte)(rand.Next() & operands.GetByte(0));
        }

        private void OpD(ushort operands)
        {
            // DXYN -> draw(Vx,Vy,N)

            var x = this.Registers.Variables[operands.GetNibble(2)];
            var y = this.Registers.Variables[operands.GetNibble(1)];
            var height = operands.GetNibble(0);

            if (this.platform.ppu.Draw(x, y, this.platform.ram.Slice(this.Registers.Index, height)))
            {
                this.Registers.Variables[0xF] = 1;
            }
            else
            {
                this.Registers.Variables[0xF] = 0;
            }
        }
    }
}