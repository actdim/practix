namespace THREE.Math
{
    public class Color
    {
        /// <summary>
        /// Red channel, 0-1.
        /// </summary>
        public float R { get; set; }

        /// <summary>
        /// Green channel, 0-1.
        /// </summary>
        public float G { get; set; }

        /// <summary>
        /// Blue channel, 0-1.
        /// </summary>
        public float B { get; set; }

        /// <summary>
        /// Construct a color from r, g, b values.
        /// </summary>
        /// <param name="r">Red channel.</param>
        /// <param name="g">Green channel.</param>
        /// <param name="b">Blue channel.</param>
        public Color(float r, float g, float b)
        {
            SetFromRGB(r, g, b);
        }

        public void SetFromRGB(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public Color(int color)
        {
            SetFromInt(color);
        }

        public void SetFromInt(int color)
        {
            // THREEJS specific conversion:
            R = (color >> 16 & 255) / 255f;
            G = (color >> 8 & 255) / 255f;
            B = (color & 255) / 255f;

            // standard conversion examples:
            //R = (color >> 16) & 0xff;
            //G = (color >> 8) & 0xff;
            //B = color & 0xff;

            //R = (color & 0xff0000) >> 16;
            //G = (color & 0x00ff00) >> 8;
            //B = color & 0x0000ff;

            //R = color / (256 * 256);
            //G = (color / 256) % 256;
            //B = color % 256;
        }

        public Color()
        {
            SetFromRGB(0, 0, 0);
        }

        /// <summary>
        /// Convert color to 8-bit integer.
        /// </summary>
        /// <returns>An integer representation of three float channels.</returns>
        public static int ToInt(float r, float g, float b)
        {
            // THREEJS specific conversion:
            return ((int)(r * 255) << 16) + ((int)(g * 255) << 8) + (int)(b * 255);

            // standard conversion examples:
            // r * 256 * 256 + g * 256 + b;
            // r * 0xffff + g * 0xff + b
            // or
            // ((r & 255) << 16) | ((g & 255) << 8) | (b & 255)
            // (r << 16) + (g << 8) + b
        }

        /// <summary>
        /// Convert this color to 8-bit integer.
        /// </summary>
        /// <returns>An int representation of the color.</returns>
        public int ToInt()
        {
            return ToInt(R, G, B);
        }

        // TODO: move to utility classes
        // public static unsafe byte[] GetBytes(int value)
        // {
        // 	byte[] buffer = new byte[4];
        // 	fixed (byte* bufferRef = buffer)
        // 	{
        // 		*((int*)bufferRef) = value;
        // 	}
        // 	return buffer;
        // }

        // public static void WriteInt(byte[] buffer, int offset, int value)
        // {
        // 	buffer[offset] = (byte)(value >> 24);
        // 	buffer[offset + 1] = (byte)(value >> 16);
        // 	buffer[offset + 2] = (byte)(value >> 8);
        // 	buffer[offset + 3] = (byte)value;
        // }

        // [StructLayout(LayoutKind.Explicit)]
        // struct IntBytes
        // {
        // 	[FieldOffset(0)]
        // 	public int Integer;
        // 	[FieldOffset(0)]
        // 	public byte Byte0;
        // 	[FieldOffset(1)]
        // 	public byte Byte1;
        // 	[FieldOffset(2)]
        // 	public byte Byte2;
        // 	[FieldOffset(3)]
        // 	public byte Byte3;
        // }
    }
}
