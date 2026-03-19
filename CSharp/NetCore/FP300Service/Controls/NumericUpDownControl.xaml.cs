using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FP300NetCoreService.Controls;

public partial class NumericUpDownControl : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(decimal),
        typeof(NumericUpDownControl),
        new FrameworkPropertyMetadata(decimal.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(decimal),
        typeof(NumericUpDownControl),
        new PropertyMetadata(decimal.Zero));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(decimal),
        typeof(NumericUpDownControl),
        new PropertyMetadata(100m));

    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment),
        typeof(decimal),
        typeof(NumericUpDownControl),
        new PropertyMetadata(1m));

    public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
        nameof(DecimalPlaces),
        typeof(int),
        typeof(NumericUpDownControl),
        new PropertyMetadata(0, OnFormatPropertyChanged));

    public NumericUpDownControl()
    {
        InitializeComponent();
        UpdateText();
    }

    public decimal Value
    {
        get => (decimal)GetValue(ValueProperty);
        set => SetValue(ValueProperty, Coerce(value));
    }

    public decimal Minimum
    {
        get => (decimal)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public decimal Maximum
    {
        get => (decimal)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public decimal Increment
    {
        get => (decimal)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumericUpDownControl control)
        {
            control.UpdateText();
        }
    }

    private static void OnFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumericUpDownControl control)
        {
            control.UpdateText();
        }
    }

    private void IncreaseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Value = Coerce(Value + Increment);
    }

    private void DecreaseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Value = Coerce(Value - Increment);
    }

    private void PartTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var candidate = PART_TextBox.Text.Insert(PART_TextBox.CaretIndex, e.Text);
        var pattern = DecimalPlaces > 0
            ? $"^-?[0-9]*({Regex.Escape(separator)}[0-9]{{0,{DecimalPlaces}}})?$"
            : "^-?[0-9]*$";
        e.Handled = !Regex.IsMatch(candidate, pattern);
    }

    private void PartTextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        CommitText();
    }

    private void PartTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitText();
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            Value = Coerce(Value + Increment);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            Value = Coerce(Value - Increment);
            e.Handled = true;
        }
    }

    private void CommitText()
    {
        if (decimal.TryParse(PART_TextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
        {
            Value = Coerce(parsed);
        }
        else
        {
            UpdateText();
        }
    }

    private decimal Coerce(decimal value)
    {
        if (value < Minimum)
        {
            return Minimum;
        }

        if (value > Maximum)
        {
            return Maximum;
        }

        return decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
    }

    private void UpdateText()
    {
        PART_TextBox.Text = Value.ToString($"F{DecimalPlaces}", CultureInfo.CurrentCulture);
    }
}
