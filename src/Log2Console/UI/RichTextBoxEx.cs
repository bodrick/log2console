using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Log2Console.UI
{
    public class RichTextBoxEx : RichTextBox
    {
        public RichTextBoxEx() => DetectUrls = false;

        [DefaultValue(false)]
        public new bool DetectUrls
        {
            get => base.DetectUrls;
            set => base.DetectUrls = value;
        }

        /// <summary>
        ///     Insert a given text as a link into the RichTextBox at the current insert position.
        /// </summary>
        /// <param name="text">Text to be inserted</param>
        public void InsertLink(string text) => InsertLink(text, SelectionStart);

        /// <summary>
        ///     Insert a given text at a given position as a link.
        /// </summary>
        /// <param name="text">Text to be inserted</param>
        /// <param name="position">Insert position</param>
        public void InsertLink(string text, int position)
        {
            if (position < 0 || position > Text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            SelectionStart = position;
            SelectedText = text;
            Select(position, text.Length);
            SetSelectionLink(true);
            Select(position + text.Length, 0);
        }

        /// <summary>
        ///     Insert a given text at at the current input position as a link.
        ///     The link text is followed by a hash (#) and the given hyperlink text, both of
        ///     them invisible.
        ///     When clicked on, the whole link text and hyperlink string are given in the
        ///     LinkClickedEventArgs.
        /// </summary>
        /// <param name="text">Text to be inserted</param>
        /// <param name="hyperlink">Invisible hyperlink string to be inserted</param>
        public void InsertLink(string text, string hyperlink) => InsertLink(text, hyperlink, SelectionStart);

        /// <summary>
        ///     Insert a given text at a given position as a link. The link text is followed by
        ///     a hash (#) and the given hyperlink text, both of them invisible.
        ///     When clicked on, the whole link text and hyperlink string are given in the
        ///     LinkClickedEventArgs.
        /// </summary>
        /// <param name="text">Text to be inserted</param>
        /// <param name="hyperlink">Invisible hyperlink string to be inserted</param>
        /// <param name="position">Insert position</param>
        public void InsertLink(string text, string hyperlink, int position)
        {
            if (position < 0 || position > Text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            SelectionStart = position;
            SelectedRtf = @"{\rtf1\ansi " + text + @"\v #" + hyperlink + @"\v0}";
            Select(position, text.Length + hyperlink.Length + 1);
            SetSelectionLink(true);
            Select(position + text.Length + hyperlink.Length + 1, 0);
        }

        /// <summary>
        ///     Set the current selection's link style
        /// </summary>
        /// <param name="link">true: set link style, false: clear link style</param>
        public void SetSelectionLink(bool link) =>
            SetSelectionStyle(NativeMethods.CFM_LINK, link ? NativeMethods.CFE_LINK : 0);

        /// <summary>
        ///     Get the link style for the current selection
        /// </summary>
        /// <returns>0: link style not set, 1: link style set, -1: mixed</returns>
        public int GetSelectionLink() => GetSelectionStyle(NativeMethods.CFM_LINK, NativeMethods.CFE_LINK);


        private void SetSelectionStyle(uint mask, uint effect)
        {
            var cf = new NativeMethods.CHARFORMAT2_STRUCT();
            cf.cbSize = (uint)Marshal.SizeOf(cf);
            cf.dwMask = mask;
            cf.dwEffects = effect;

            var wpar = new IntPtr(NativeMethods.SCF_SELECTION);
            var lpar = Marshal.AllocCoTaskMem(Marshal.SizeOf(cf));
            Marshal.StructureToPtr(cf, lpar, false);

            NativeMethods.SendMessage(Handle, NativeMethods.EM_SETCHARFORMAT, wpar, lpar);

            Marshal.FreeCoTaskMem(lpar);
        }

        private int GetSelectionStyle(uint mask, uint effect)
        {
            var cf = new NativeMethods.CHARFORMAT2_STRUCT();
            cf.cbSize = (uint)Marshal.SizeOf(cf);
            cf.szFaceName = new char[32];

            var wpar = new IntPtr(NativeMethods.SCF_SELECTION);
            var lpar = Marshal.AllocCoTaskMem(Marshal.SizeOf(cf));
            Marshal.StructureToPtr(cf, lpar, false);

            NativeMethods.SendMessage(Handle, NativeMethods.EM_GETCHARFORMAT, wpar, lpar);

            cf = (NativeMethods.CHARFORMAT2_STRUCT)Marshal.PtrToStructure(lpar,
                typeof(NativeMethods.CHARFORMAT2_STRUCT));

            int state;
            // dwMask holds the information which properties are consistent throughout the selection:
            if ((cf.dwMask & mask) == mask)
            {
                state = (cf.dwEffects & effect) == effect ? 1 : 0;
            }
            else
            {
                state = -1;
            }

            Marshal.FreeCoTaskMem(lpar);
            return state;
        }
    }
}