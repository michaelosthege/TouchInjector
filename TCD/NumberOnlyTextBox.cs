using System.Windows.Controls;

namespace TCD.Controls
{
    /// <summary>
    /// A TextBox that only allows numbers.
    /// </summary>
    public class NumberOnlyTextBox : TextBox
    {
        protected override void OnPreviewTextInput(System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !AreAllValidNumericChars(e.Text);
            base.OnPreviewTextInput(e);
        }
        bool AreAllValidNumericChars(string str)
        {
            foreach (char c in str)
                if (!char.IsDigit(c))
                    return false; 
            return true;
        }

    }
}
