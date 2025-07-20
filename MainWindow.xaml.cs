using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace LogicPad
{
    public partial class MainWindow : Window
    {
        private long currentValue = 0;
        private long operand = 0;
        private string currentOperator = string.Empty;
        private bool isNewEntry = true;
        private int currentBitSize = 8;
        private int currentRadix = 16;
        private int currentQFormatFractionalBits = 8; // New member for fixed-point fractional bits
        private string currentExpression = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            // Set initial value for FixedPointFractionalBitsInput
            FixedPointFractionalBitsInput.Text = currentQFormatFractionalBits.ToString();
            UpdateDisplay();
            EnableDisableButtons();
        }

        private void UpdateDisplay()
        {
            long mask = (currentBitSize == 64) ? -1L : (1L << currentBitSize) - 1;
            currentValue &= mask;

            HexDisplay.TextChanged -= Display_TextChanged;
            DecDisplay.TextChanged -= Display_TextChanged;
            OctDisplay.TextChanged -= Display_TextChanged;
            BinDisplay.TextChanged -= Display_TextChanged;
            FixedDisplay.TextChanged -= Display_TextChanged;
            FloatDisplay.TextChanged -= Display_TextChanged;

            HexDisplay.Text = Convert.ToString(currentValue, 16).ToUpper();
            DecDisplay.Text = Convert.ToString(currentValue, 10);
            OctDisplay.Text = Convert.ToString(currentValue, 8);
            BinDisplay.Text = Convert.ToString(currentValue, 2).PadLeft(currentBitSize, '0');

            // Fixed-point conversion
            double fixedPointValue = (double)currentValue / (1L << currentQFormatFractionalBits);
            FixedDisplay.Text = fixedPointValue.ToString();

            // Float conversion (assuming 32-bit float for now)
            // This is a simplified conversion and might not be perfectly accurate for all cases
            float floatValue = BitConverter.ToSingle(BitConverter.GetBytes((int)currentValue), 0);
            FloatDisplay.Text = floatValue.ToString();

            HexDisplay.TextChanged += Display_TextChanged;
            DecDisplay.TextChanged += Display_TextChanged;
            OctDisplay.TextChanged += Display_TextChanged;
            BinDisplay.TextChanged += Display_TextChanged;
            FixedDisplay.TextChanged += Display_TextChanged;
            FloatDisplay.TextChanged += Display_TextChanged;

            UpdateHorizontalBitDisplay();
            UpdateVerticalBitDisplay();

            ExpressionTextBlock.Text = currentExpression;
        }

        private void UpdateHorizontalBitDisplay()
        {
            HorizontalBitDisplayPanel.Children.Clear();
            string binary = Convert.ToString(currentValue, 2).PadLeft(currentBitSize, '0');

            for (int i = 0; i < binary.Length; i++)
            {
                int bitPosition = binary.Length - 1 - i;
                var button = new ToggleButton
                {
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = bitPosition.ToString(), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center },
                            new TextBlock { Text = binary[i].ToString(), FontFamily = new FontFamily("Consolas"), FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center }
                        }
                    },
                    Tag = bitPosition,
                    IsChecked = binary[i] == '1',
                    Margin = new Thickness(1),
                    Padding = new Thickness(2)
                };
                button.Click += Bit_Click;
                HorizontalBitDisplayPanel.Children.Add(button);
            }
        }

        private void UpdateVerticalBitDisplay()
        {
            VerticalBitDisplayPanel.Children.Clear();
            string binary = Convert.ToString(currentValue, 2).PadLeft(currentBitSize, '0');

            for (int i = 0; i < currentBitSize; i++)
            {
                int bitPosition = i;
                char bitValue = binary[currentBitSize - 1 - i];

                var grid = new Grid
                {
                    Margin = new Thickness(2, 1, 2, 1)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

                var bitLabel = new TextBlock
                {
                    Text = bitPosition.ToString(),
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Grid.SetColumn(bitLabel, 0);

                var button = new ToggleButton
                {
                    Content = bitValue.ToString(),
                    Tag = bitPosition,
                    IsChecked = bitValue == '1',
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 16,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                button.Click += Bit_Click;
                Grid.SetColumn(button, 1);

                grid.Children.Add(bitLabel);
                grid.Children.Add(button);

                VerticalBitDisplayPanel.Children.Insert(0, grid); // Insert at top to reverse order
            }
        }


        private void Bit_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton button = (ToggleButton)sender;
            int bitPosition = (int)button.Tag;
            long bitMask = 1L << bitPosition;
            currentValue ^= bitMask; // XOR to toggle the bit
            isNewEntry = false;
            UpdateDisplay();
        }

        private void Number_Click(object sender, RoutedEventArgs e)
        {
            if (isNewEntry)
            {
                currentValue = 0;
                isNewEntry = false;
            }

            Button button = (Button)sender;
            string? digit = button.Content.ToString();
            if (digit == null) return;

            long digitValue;
            try
            {
                digitValue = Convert.ToInt64(digit, currentRadix);
            }
            catch (FormatException)
            {
                // Handle cases like 'A' in decimal mode
                return;
            }

            string currentInput = Convert.ToString(currentValue, currentRadix);
            string newInput = currentInput + digit;

            try
            {
                currentValue = Convert.ToInt64(newInput, currentRadix);
            }
            catch (OverflowException)
            {
                // Value too large, ignore last digit
            }

            currentExpression = currentValue.ToString();
            UpdateDisplay();
        }

        private void Operator_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string? op = button.Content.ToString();
            if (op == null) return;

            if (!isNewEntry)
            {
                Calculate();
            }

            operand = currentValue;
            currentOperator = op;
            isNewEntry = true;
        }

        private void Equals_Click(object sender, RoutedEventArgs e)
        {
            Calculate();
            currentOperator = string.Empty;
            isNewEntry = true;
        }

        private void Calculate()
        {
            if (isNewEntry && currentOperator != "Not") return;

            string expression = operand.ToString() + " " + currentOperator + " " + currentValue.ToString();

            switch (currentOperator)
            {
                case "+": currentValue = operand + currentValue; break;
                case "-": currentValue = operand - currentValue; break;
                case "*": currentValue = operand * currentValue; break;
                case "/": currentValue = (currentValue != 0) ? operand / currentValue : 0; break;
                case "Mod": currentValue = (currentValue != 0) ? operand % currentValue : 0; break;
                case "And": currentValue = operand & currentValue; break;
                case "Or": currentValue = operand | currentValue; break;
                case "Xor": currentValue = operand ^ currentValue; break;
                case "Lsh": currentValue = operand << (int)(currentValue & 0x3F); break;
                case "Rsh": currentValue = operand >> (int)(currentValue & 0x3F); break;
            }
            currentExpression = expression + " = " + currentValue.ToString();
            UpdateDisplay();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            currentValue = 0;
            operand = 0;
            currentOperator = string.Empty;
            isNewEntry = true;
            currentExpression = "0";
            UpdateDisplay();
        }

        private void ClearEntry_Click(object sender, RoutedEventArgs e)
        {
            currentValue = 0;
            isNewEntry = true;
            currentExpression = "0";
            UpdateDisplay();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (isNewEntry) return;
            string currentInput = Convert.ToString(currentValue, currentRadix);
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
            }
            if (string.IsNullOrEmpty(currentInput))
            {
                currentInput = "0";
            }
            currentValue = Convert.ToInt64(currentInput, currentRadix);
            currentExpression = currentValue.ToString();
            UpdateDisplay();
        }

        private void Not_Click(object sender, RoutedEventArgs e)
        {
            currentValue = ~currentValue;
            currentExpression = "~" + currentValue.ToString();
            UpdateDisplay();
        }

        private void Display_TextChanged(object sender, TextChangedEventArgs? e)
        {
            if (!this.IsLoaded) return;

            TextBox changedTextBox = (TextBox)sender;
            if (!changedTextBox.IsFocused) return; // Only update if the user is actively typing in this box

            string text = changedTextBox.Text;
            long newValue;

            if (string.IsNullOrEmpty(text))
            {
                newValue = 0;
            }
            else
            {
                try
                {
                    if (changedTextBox == HexDisplay)
                    {
                        newValue = Convert.ToInt64(text, 16);
                    }
                    else if (changedTextBox == DecDisplay)
                    {
                        newValue = Convert.ToInt64(text, 10);
                    }
                    else if (changedTextBox == OctDisplay)
                    {
                        newValue = Convert.ToInt64(text, 8);
                    }
                    else if (changedTextBox == BinDisplay)
                    {
                        newValue = Convert.ToInt64(text, 2);
                    }
                    else if (changedTextBox == FixedDisplay)
                    {
                        double val = double.Parse(text);
                        newValue = (long)(val * (1L << currentQFormatFractionalBits));
                    }
                    else if (changedTextBox == FloatDisplay)
                    {
                        float val = float.Parse(text);
                        newValue = BitConverter.ToInt32(BitConverter.GetBytes(val), 0);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (FormatException)
                {
                    // Ignore invalid input for now, or show an error message
                    return;
                }
                catch (OverflowException)
                {
                    // Value too large for long, handle as needed
                    return;
                }
            }

            currentValue = newValue;
            UpdateDisplay();
        }

        private void FixedPointFractionalBits_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            if (int.TryParse(FixedPointFractionalBitsInput.Text, out int bits))
            {
                currentQFormatFractionalBits = Math.Max(0, bits);
                UpdateDisplay();
            }
        }

        private void Radix_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;
            RadioButton rb = (sender as RadioButton)!;
            currentRadix = rb.Content.ToString() switch
            {
                "HEX" => 16,
                "DEC" => 10,
                "OCT" => 8,
                "BIN" => 2,
                "Fixed" => 10, // Fixed and Float will use decimal input
                "Float" => 10,
                _ => 10
            };
            EnableDisableButtons();
            isNewEntry = true;
            UpdateDisplay();
        }

        private void DataSize_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;
            RadioButton rb = (sender as RadioButton)!;
            currentBitSize = Convert.ToInt32(rb.Tag);
            isNewEntry = true;
            UpdateDisplay();
        }

        private void ToggleTopmostButton_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = !this.Topmost;
            ToggleTopmostButton.Content = this.Topmost ? "Always On Top: On" : "Always On Top: Off";
        }

        private void EnableDisableButtons()
        {
            foreach (var button in FindVisualChildren<Button>(this))
            {
                if (button.Content is string s && s.Length == 1)
                {
                    if ("0123456789ABCDEF".Contains(s))
                    {
                        int val;
                        bool isHex = int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out val);
                        if (isHex)
                        {
                            button.IsEnabled = val < currentRadix;
                        }
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null)
                {
                    if (child is T)
                    {                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
