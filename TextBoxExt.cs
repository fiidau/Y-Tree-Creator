﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace YTreeCreator
{
    class TextBoxExt : TextBox
    {
        new public void AppendText(string text)
        {
            if (this.Text.Length == this.MaxLength)
            {
                return;
            }
            else if (this.Text.Length + text.Length > this.MaxLength)
            {
                base.AppendText(text.Substring(0, (this.MaxLength - this.Text.Length)));
            }
            else
            {
                base.AppendText(text);
            }
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Length > this.MaxLength)
                {
                    base.Text = value.Substring(0, this.MaxLength);
                }
                else
                {
                    base.Text = value;
                }
            }
        }

        // Also: Clearing top X lines with high performance
        public void ClearTopLines(int count)
        {
            if (count <= 0)
            {
                return;
            }
            else if (!this.Multiline)
            {
                this.Clear();
                return;
            }

            string txt = this.Text;
            int cursor = 0, ixOf = 0, brkLength = 0, brkCount = 0;

            while (brkCount < count)
            {
                ixOf = txt.IndexOfBreak(cursor, out brkLength);
                if (ixOf < 0)
                {
                    this.Clear();
                    return;
                }
                cursor = ixOf + brkLength;
                brkCount++;
            }
            this.Text = txt.Substring(cursor);
        }
    }

    public static class StringExt
    {
        public static int IndexOfBreak(this string str, out int length)
        {
            return IndexOfBreak(str, 0, out length);
        }

        public static int IndexOfBreak(this string str, int startIndex, out int length)
        {
            if (string.IsNullOrEmpty(str))
            {
                length = 0;
                return -1;
            }
            int ub = str.Length - 1;
            int intchr;
            if (startIndex > ub)
            {
                throw new ArgumentOutOfRangeException();
            }
            for (int i = startIndex; i <= ub; i++)
            {
                intchr = str[i];
                if (intchr == 0x0D)
                {
                    if (i < ub && str[i + 1] == 0x0A)
                    {
                        length = 2;
                    }
                    else
                    {
                        length = 1;
                    }
                    return i;
                }
                else if (intchr == 0x0A)
                {
                    length = 1;
                    return i;
                }
            }
            length = 0;
            return -1;
        }
    }
}
