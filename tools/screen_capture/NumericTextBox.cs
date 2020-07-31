// from http://msdn.microsoft.com/en-us/library/ms229644.aspx

namespace btnet
{
    using System.Globalization;
    using System.Windows.Forms;

    public class NumericTextBox : TextBox // really, just ints...
    {
        public int IntValue => int.Parse(Text);

        // Restricts the entry of characters to digits (including hex), the negative sign,
        // the decimal point, and editing keystrokes (backspace).
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            var numberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;
            var decimalSeparator = numberFormatInfo.NumberDecimalSeparator;
            var groupSeparator = numberFormatInfo.NumberGroupSeparator;
            var negativeSign = numberFormatInfo.NegativeSign;

            var keyInput = e.KeyChar.ToString();

            if (char.IsDigit(e.KeyChar))
            {
                // Digits are OK
            }
            //else if (keyInput.Equals(decimalSeparator) || keyInput.Equals(groupSeparator) ||
            // keyInput.Equals(negativeSign))
            //{
            //    // Decimal separator is OK
            //}
            else if (e.KeyChar == '\b')
            {
                // Backspace key is OK
            }
            //    else if ((ModifierKeys & (Keys.Control | Keys.Alt)) != 0)
            //    {
            //     // Let the edit control handle control and alt key combinations
            //    }
            //else if (this.allowSpace && e.KeyChar == ' ')
            //{

            //}
            else
            {
                // Consume this invalid key and beep
                e.Handled = true;
                //    MessageBeep();
            }
        }

        //public decimal DecimalValue
        //{
        //    get
        //    {
        //        return Decimal.Parse(this.Text);
        //    }
        //}

        //public bool AllowSpace
        //{
        //    set
        //    {
        //        this.allowSpace = value;
        //    }

        //    get
        //    {
        //        return this.allowSpace;
        //    }
    }
}