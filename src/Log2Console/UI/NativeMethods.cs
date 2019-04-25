using System;
using System.Runtime.InteropServices;

namespace Log2Console.UI
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        #region Interop-Defines

        [StructLayout(LayoutKind.Sequential)]
        public struct CHARFORMAT2_STRUCT
        {
            public uint cbSize;
            public uint dwMask;
            public uint dwEffects;
            public readonly int yHeight;
            public readonly int yOffset;
            public readonly int crTextColor;
            public readonly byte bCharSet;
            public readonly byte bPitchAndFamily;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szFaceName;

            public readonly ushort wWeight;
            public readonly ushort sSpacing;
            public readonly int crBackColor; // Color.ToArgb() -> int
            public readonly int lcid;
            public readonly int dwReserved;
            public readonly short sStyle;
            public readonly short wKerning;
            public readonly byte bUnderlineType;
            public readonly byte bAnimation;
            public readonly byte bRevAuthor;
            public readonly byte bReserved1;
        }

        public const int WM_NCLBUTTONDBLCLK = 0x00A3;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;

        public const int WM_USER = 0x0400;
        public const int EM_GETCHARFORMAT = WM_USER + 58;
        public const int EM_SETCHARFORMAT = WM_USER + 68;

        public const int SCF_SELECTION = 0x0001;
        public const int SCF_WORD = 0x0002;
        public const int SCF_ALL = 0x0004;

        #region CHARFORMAT2 Flags

        public const uint CFE_BOLD = 0x0001;
        public const uint CFE_ITALIC = 0x0002;
        public const uint CFE_UNDERLINE = 0x0004;
        public const uint CFE_STRIKEOUT = 0x0008;
        public const uint CFE_PROTECTED = 0x0010;
        public const uint CFE_LINK = 0x0020;
        public const uint CFE_AUTOCOLOR = 0x40000000;
        public const uint CFE_SUBSCRIPT = 0x00010000; /* Superscript and subscript are */
        public const uint CFE_SUPERSCRIPT = 0x00020000; /*  mutually exclusive			 */

        public const int CFM_SMALLCAPS = 0x0040; /* (*)	*/
        public const int CFM_ALLCAPS = 0x0080; /* Displayed by 3.0	*/
        public const int CFM_HIDDEN = 0x0100; /* Hidden by 3.0 */
        public const int CFM_OUTLINE = 0x0200; /* (*)	*/
        public const int CFM_SHADOW = 0x0400; /* (*)	*/
        public const int CFM_EMBOSS = 0x0800; /* (*)	*/
        public const int CFM_IMPRINT = 0x1000; /* (*)	*/
        public const int CFM_DISABLED = 0x2000;
        public const int CFM_REVISED = 0x4000;

        public const int CFM_BACKCOLOR = 0x04000000;
        public const int CFM_LCID = 0x02000000;
        public const int CFM_UNDERLINETYPE = 0x00800000; /* Many displayed by 3.0 */
        public const int CFM_WEIGHT = 0x00400000;
        public const int CFM_SPACING = 0x00200000; /* Displayed by 3.0	*/
        public const int CFM_KERNING = 0x00100000; /* (*)	*/
        public const int CFM_STYLE = 0x00080000; /* (*)	*/
        public const int CFM_ANIMATION = 0x00040000; /* (*)	*/
        public const int CFM_REVAUTHOR = 0x00008000;


        public const uint CFM_BOLD = 0x00000001;
        public const uint CFM_ITALIC = 0x00000002;
        public const uint CFM_UNDERLINE = 0x00000004;
        public const uint CFM_STRIKEOUT = 0x00000008;
        public const uint CFM_PROTECTED = 0x00000010;
        public const uint CFM_LINK = 0x00000020;
        public const uint CFM_SIZE = 0x80000000;
        public const uint CFM_COLOR = 0x40000000;
        public const uint CFM_FACE = 0x20000000;
        public const uint CFM_OFFSET = 0x10000000;
        public const uint CFM_CHARSET = 0x08000000;
        public const uint CFM_SUBSCRIPT = CFE_SUBSCRIPT | CFE_SUPERSCRIPT;
        public const uint CFM_SUPERSCRIPT = CFM_SUBSCRIPT;

        public const byte CFU_UNDERLINENONE = 0x00000000;
        public const byte CFU_UNDERLINE = 0x00000001;
        public const byte CFU_UNDERLINEWORD = 0x00000002; /* (*) displayed as ordinary underline	*/
        public const byte CFU_UNDERLINEDOUBLE = 0x00000003; /* (*) displayed as ordinary underline	*/
        public const byte CFU_UNDERLINEDOTTED = 0x00000004;
        public const byte CFU_UNDERLINEDASH = 0x00000005;
        public const byte CFU_UNDERLINEDASHDOT = 0x00000006;
        public const byte CFU_UNDERLINEDASHDOTDOT = 0x00000007;
        public const byte CFU_UNDERLINEWAVE = 0x00000008;
        public const byte CFU_UNDERLINETHICK = 0x00000009;
        public const byte CFU_UNDERLINEHAIRLINE = 0x0000000A; /* (*) displayed as ordinary underline	*/

        #endregion

        #endregion
    }


}