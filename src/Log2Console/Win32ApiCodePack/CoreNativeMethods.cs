namespace Log2Console.Win32ApiCodePack
{
    internal static class CoreNativeMethods
    {
        public static int LOWORD(long dword)
        {
            return (short) (dword & 0xFFFF);
        }

        public static int HIWORD(long dword, int size)
        {
            return (short) (dword >> size);
        }
    }
}