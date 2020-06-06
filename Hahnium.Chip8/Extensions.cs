using System;
using System.Collections.Generic;
using System.Text;

namespace Hahnium.Chip8
{
    public static class Extensions
    {
        public static byte GetNibble(this ushort value, int nibbleId) => (byte)((value >> 4 * nibbleId) & 0xf);

        public static byte GetByte(this ushort value, int byteId) => (byte)((value >> 8 * byteId) & 0xff);
    }
}
