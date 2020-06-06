using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

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
        private Task inputTask;
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
            inputTask = Task.Factory.StartNew(InputLoop);
        }

        private char[] keyboardMap = new char[]
        {
            'z', 'x', 'c', 'v', 'a', 's', 'd', 'f', 'q', 'w', 'e', 'r', '1', '2', '3', '4'
        };

        private AutoResetEvent keyBlock = new AutoResetEvent(false);
        private byte lastKey = byte.MaxValue;

        public void InputLoop()
        {
            while (true)
            {
                var key = Console.ReadKey(true);
                lastKey = (byte)Array.IndexOf(keyboardMap, key.KeyChar);
                keyBlock.Set();
                keyBlock.Reset();
            }
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
                case 9:
                    Op9(operands);
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
                case 0xE:
                    OpE(operands);
                    break;
                case 0xF:
                    OpF(operands);
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
                case 0:
                    // Vx=Vy	Sets VX to the value of VY.
                    this.Registers.Variables[variableX] = this.Registers.Variables[variableY];
                    break;
                case 2:
                    // Vx = Vx & Vy    Sets VX to VX and VY. (Bitwise AND operation)
                    this.Registers.Variables[variableX] &= this.Registers.Variables[variableY];
                    break;
                case 4:
                    //Vx += Vy	Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there isn't.
                    this.Registers.Variables[variableX] += this.Registers.Variables[variableY];
                    break;
                case 8:
                    // Vx=Vx|Vy Sets VX to VX or VY. (Bitwise OR operation)
                    this.Registers.Variables[variableX] |= this.Registers.Variables[variableY];
                    break;
                default: throw new NotImplementedException();
            }
        }

        private void Op9(ushort operands)
        {
            var x = this.Registers.Variables[operands.GetNibble(2)];
            var y = this.Registers.Variables[operands.GetNibble(1)];

            // 9XY0 ->	if(Vx!=Vy)	Skips the next instruction if VX doesn't equal VY. (Usually the next instruction is a jump to skip a code block)
            if (x != y)
            {
                this.Registers.PC += 2;
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

        private void OpE(ushort operands)
        {
            var x = this.Registers.Variables[operands.GetNibble(2)];

            switch (operands.GetByte(0))
            {
                case 0x9E:
                    // EX9E KeyOp   if (key() == Vx) Skips the next instruction if the key stored in VX is pressed. (Usually the next instruction is a jump to skip a code block)
                    if (lastKey == x)
                    {
                        this.Registers.PC += 2;
                    }
                    break;
                case 0xA1:
                    // EXA1 KeyOp   if (key() != Vx) Skips the next instruction if the key stored in VX isn't pressed. (Usually the next instruction is a jump to skip a code block)
                    if (lastKey != x)
                    {
                        this.Registers.PC += 2;
                    }
                    break;
                default: throw new NotImplementedException();
            }
        }

        private void OpF(ushort operands)
        {
            var lsb = operands.GetByte(0);
            var x = operands.GetNibble(2);

            switch (lsb)
            {
                case 0x0A:
                    // Vx = get_key()	A key press is awaited, and then stored in VX. (Blocking Operation. All instruction halted until next key event)
                    this.keyBlock.WaitOne();
                    break;
                case 0x18:
                    // TODO: better beep here
                    Console.Beep(420, 15);
                    break;
                case 0x1E:
                    // I += Vx  Adds VX to I. VF is set to 1 when there is a range overflow(I + VX > 0xFFF), and to 0 when there isn't.[c]
                    var newIndex = this.Registers.Index + this.Registers.Variables[x];
                    this.Registers.Variables[0xF] = (byte)((newIndex > ushort.MaxValue) ? 1 : 0);
                    this.Registers.Index = (ushort)newIndex;
                    break;
                case 0x65:
                    // reg_load(Vx,&I)	Fills V0 to VX (including VX) with values from memory starting at address I. The offset from I is increased by 1 for each value written, but I itself is left unmodified.

                    byte* memoryPointer = ((byte*)memoryhandle.Pointer) + this.Registers.Index;
                    fixed (void* vPtr = this.Registers.Variables)
                    {
                        Buffer.MemoryCopy(memoryPointer, vPtr, 16, 16);
                    }
                    break;
                default: throw new NotImplementedException();
            }
        }
    }
}