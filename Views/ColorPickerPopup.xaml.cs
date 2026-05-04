using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawMe.Views;

public partial class ColorPickerPopup : UserControl
{
    public event Action<Color>? ColorChanged;

    private double _hue   = 200;
    private double _sat   = 0.8;
    private double _val   = 0.9;
    private byte   _alpha = 255;

    private bool _draggingSv    = false;
    private bool _draggingHue   = false;
    private bool _draggingAlpha = false;
    private bool _updatingHex   = false;
    private bool _loaded        = false;

    private static readonly string[] Presets =
    {
        "#FFFFFF", "#000000", "#E74C3C", "#E67E22", "#F1C40F",
        "#2ECC71", "#3498DB", "#9B59B6", "#E91E63", "#546E7A"
    };

    public ColorPickerPopup()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        BuildPresets();
        UpdateUI();
    }

    public Color SelectedColor
    {
        get => HsvToRgb(_hue, _sat, _val, _alpha);
        set
        {
            RgbToHsv(value, out _hue, out _sat, out _val);
            _alpha = value.A;
            if (_loaded) UpdateUI();
        }
    }

    public void Refresh() => UpdateUI();

    // ── Couleurs prédéfinies ──────────────────────────────────────
    private void BuildPresets()
    {
        SwatchPanel.Children.Clear();
        foreach (var hex in Presets)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var swatch = new Border
            {
                Width           = 17,
                Height          = 17,
                CornerRadius    = new CornerRadius(4),
                Background      = new SolidColorBrush(color),
                Margin          = new Thickness(1),
                Cursor          = Cursors.Hand,
                BorderBrush     = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1)
            };
            swatch.MouseDown += (_, _) =>
            {
                SelectedColor = color;
                ColorChanged?.Invoke(SelectedColor);
            };
            SwatchPanel.Children.Add(swatch);
        }
    }

    // ── Carré SV ─────────────────────────────────────────────────
    private void SvGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingSv = true;
        SvGrid.CaptureMouse();
        UpdateSv(e.GetPosition(SvGrid));
    }

    private void SvGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingSv && e.LeftButton == MouseButtonState.Pressed)
            UpdateSv(e.GetPosition(SvGrid));
    }

    private void SvGrid_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggingSv = false;
        SvGrid.ReleaseMouseCapture();
    }

    private void UpdateSv(Point p)
    {
        _sat = Math.Clamp(p.X / SvGrid.ActualWidth,  0, 1);
        _val = Math.Clamp(1 - p.Y / SvGrid.ActualHeight, 0, 1);
        UpdateUI();
        ColorChanged?.Invoke(SelectedColor);
    }

    // ── Slider teinte ────────────────────────────────────────────
    private void HueGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingHue = true;
        HueGrid.CaptureMouse();
        UpdateHue(e.GetPosition(HueGrid));
    }

    private void HueGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingHue && e.LeftButton == MouseButtonState.Pressed)
            UpdateHue(e.GetPosition(HueGrid));
    }

    private void HueGrid_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggingHue = false;
        HueGrid.ReleaseMouseCapture();
    }

    private void UpdateHue(Point p)
    {
        _hue = Math.Clamp(p.X / HueGrid.ActualWidth, 0, 1) * 360;
        UpdateUI();
        ColorChanged?.Invoke(SelectedColor);
    }

    // ── Slider opacité ───────────────────────────────────────────
    private void AlphaGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingAlpha = true;
        AlphaGrid.CaptureMouse();
        UpdateAlpha(e.GetPosition(AlphaGrid));
    }

    private void AlphaGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingAlpha && e.LeftButton == MouseButtonState.Pressed)
            UpdateAlpha(e.GetPosition(AlphaGrid));
    }

    private void AlphaGrid_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggingAlpha = false;
        AlphaGrid.ReleaseMouseCapture();
    }

    private void UpdateAlpha(Point p)
    {
        _alpha = (byte)Math.Clamp(p.X / AlphaGrid.ActualWidth * 255, 0, 255);
        UpdateUI();
        ColorChanged?.Invoke(SelectedColor);
    }

    // ── Champ Hex ────────────────────────────────────────────────
    private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingHex) return;
        var t = HexBox.Text.Trim();
        if (t.Length == 6 || t.Length == 8)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString("#" + t);
                RgbToHsv(c, out _hue, out _sat, out _val);
                if (t.Length == 8) _alpha = c.A;
                UpdateUI(skipHex: true);
                ColorChanged?.Invoke(SelectedColor);
            }
            catch { }
        }
    }

    // ── Mise à jour de l'UI ──────────────────────────────────────
    private void UpdateUI(bool skipHex = false)
    {
        if (!_loaded) return;

        // Fond du carré SV = teinte pure
        HueRect.Fill = new SolidColorBrush(HsvToRgb(_hue, 1, 1, 255));

        // Position du curseur SV
        double sw = SvGrid.ActualWidth, sh = SvGrid.ActualHeight;
        Canvas.SetLeft(SvThumb, _sat * sw - 7);
        Canvas.SetTop (SvThumb, (1 - _val) * sh - 7);

        // Position du curseur teinte
        Canvas.SetLeft(HueThumb, _hue / 360.0 * HueGrid.ActualWidth - 3);

        // Dégradé + curseur opacité
        var opaque = HsvToRgb(_hue, _sat, _val, 255);
        AlphaRect.Fill = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(0,   opaque.R, opaque.G, opaque.B), 0),
                new GradientStop(Color.FromArgb(255, opaque.R, opaque.G, opaque.B), 1)
            },
            new Point(0, 0), new Point(1, 0));
        Canvas.SetLeft(AlphaThumb, _alpha / 255.0 * AlphaGrid.ActualWidth - 3);

        // Aperçu
        PreviewSwatch.Background = new SolidColorBrush(HsvToRgb(_hue, _sat, _val, _alpha));

        // Champ hex
        if (!skipHex)
        {
            var final = HsvToRgb(_hue, _sat, _val, _alpha);
            _updatingHex = true;
            HexBox.Text = _alpha == 255
                ? $"{final.R:X2}{final.G:X2}{final.B:X2}"
                : $"{final.A:X2}{final.R:X2}{final.G:X2}{final.B:X2}";
            _updatingHex = false;
        }
    }

    // ── Conversion HSV ↔ RGB ─────────────────────────────────────
    private static Color HsvToRgb(double h, double s, double v, byte a)
    {
        h = ((h % 360) + 360) % 360;
        double c = v * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = v - c;
        double r, g, b;
        if      (h < 60)  { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else              { r = c; g = 0; b = x; }
        return Color.FromArgb(a,
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    private static void RgbToHsv(Color c, out double h, out double s, out double v)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        v = max;
        s = max == 0 ? 0 : delta / max;
        if (delta == 0) { h = 0; return; }
        if      (max == r) h = 60 * ((g - b) / delta % 6);
        else if (max == g) h = 60 * ((b - r) / delta + 2);
        else               h = 60 * ((r - g) / delta + 4);
        if (h < 0) h += 360;
    }
}
