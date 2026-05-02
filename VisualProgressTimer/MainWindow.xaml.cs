using System;
using System.Globalization;
using System.Media;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace VisualProgressTimer;

public partial class MainWindow : Window
{
    public static readonly DependencyProperty WindowBackgroundBrushProperty =
        DependencyProperty.Register(nameof(WindowBackgroundBrush), typeof(Brush), typeof(MainWindow), new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty ForegroundBrushProperty =
        DependencyProperty.Register(nameof(ForegroundBrush), typeof(Brush), typeof(MainWindow), new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty PanelBrushProperty =
        DependencyProperty.Register(nameof(PanelBrush), typeof(Brush), typeof(MainWindow), new PropertyMetadata(Brushes.Transparent));

    private readonly DispatcherTimer _timer;
    private TimeSpan _duration = TimeSpan.FromMinutes(45);
    private TimeSpan _remaining = TimeSpan.FromMinutes(45);
    private DateTime _lastTick;
    private const int MaxMinutes = 60;
    private bool _isRunning;
    private bool _isDragging;
    private Color _gaugeColor = Color.FromRgb(32, 42, 68);
    private SoundPlayer? _alarmPlayer;
    private WinForms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += Timer_Tick;

        LoadUserSettings();
        ApplyTheme();
        UpdateDisplay();
        Loaded += (_, _) => DrawTimer();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _alarmPlayer?.Stop();
            _alarmPlayer?.Dispose();
            _notifyIcon?.Dispose();
        };
    }

    public Brush WindowBackgroundBrush
    {
        get => (Brush)GetValue(WindowBackgroundBrushProperty);
        set => SetValue(WindowBackgroundBrushProperty, value);
    }

    public Brush ForegroundBrush
    {
        get => (Brush)GetValue(ForegroundBrushProperty);
        set => SetValue(ForegroundBrushProperty, value);
    }

    public Brush PanelBrush
    {
        get => (Brush)GetValue(PanelBrushProperty);
        set => SetValue(PanelBrushProperty, value);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        _remaining -= now - _lastTick;
        _lastTick = now;

        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            StopTimer();
            NotifyTimeUp();
        }

        UpdateDisplay();
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            StopTimer();
            return;
        }

        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = _duration;
        }

        _isRunning = true;
        _lastTick = DateTime.Now;
        _timer.Start();
        StartStopButton.Content = "Stop";
    }

    private void StopTimer()
    {
        _timer.Stop();
        _isRunning = false;
        StartStopButton.Content = "Start";
    }

    private void TimerSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        TimerSurface.CaptureMouse();
        SetDurationFromPoint(e.GetPosition(TimerSurface));
    }

    private void TimerSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            SetDurationFromPoint(e.GetPosition(TimerSurface));
        }
    }

    private void TimerSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        TimerSurface.ReleaseMouseCapture();
    }

    private void TimerSurface_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (BorderlessMenuItem.IsChecked)
        {
            DragMove();
        }
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 1 : -1;
        SetDurationMinutes(Math.Clamp((int)Math.Round(_remaining.TotalMinutes) + delta, 1, MaxMinutes));
        e.Handled = true;
    }

    private void SetDurationFromPoint(Point point)
    {
        StopTimer();

        var center = new Point(TimerSurface.ActualWidth / 2, TimerSurface.ActualHeight / 2);
        var angle = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI + 90;
        if (angle < 0)
        {
            angle += 360;
        }

        var minutes = Math.Clamp((int)Math.Round(angle / 360 * MaxMinutes), 1, MaxMinutes);
        SetDurationMinutes(minutes);
    }

    private void SetDurationMinutes(int minutes)
    {
        _duration = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, MaxMinutes));
        _remaining = _duration;
        SaveUserSettings();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        TimeText.Text = $"{(int)_remaining.TotalMinutes:00}:{_remaining.Seconds:00}";
        DrawTimer();
    }

    private void DrawTimer()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        TimerCanvas.Children.Clear();

        var available = Math.Min(Root.ActualWidth, Math.Max(280, Root.ActualHeight - ControlsPanel.ActualHeight - SettingsMenu.ActualHeight - 24));
        var caseSize = Math.Max(280, Math.Min(available, 460));
        TimerSurface.Width = caseSize;
        TimerSurface.Height = caseSize;
        TimerCanvas.Width = caseSize;
        TimerCanvas.Height = caseSize;

        DrawTimerCase(caseSize);
        DrawDial(caseSize);
    }

    private void DrawTimerCase(double size)
    {
        var dark = DarkModeMenuItem?.IsChecked == true;
        var frameFill = BlendWith(dark ? Color.FromRgb(44, 49, 56) : Color.FromRgb(232, 238, 245), _gaugeColor, dark ? 0.32 : 0.18);
        var frameStroke = BlendWith(dark ? Color.FromRgb(132, 143, 158) : Color.FromRgb(156, 171, 188), _gaugeColor, 0.35);
        var outer = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size * 0.13),
            Background = new SolidColorBrush(frameFill),
            BorderBrush = new SolidColorBrush(frameStroke),
            BorderThickness = new Thickness(size * 0.012)
        };
        TimerCanvas.Children.Add(outer);

        var inset = size * 0.085;
        var face = new Border
        {
            Width = size - inset * 2,
            Height = size - inset * 2,
            CornerRadius = new CornerRadius(size * 0.09),
            Background = new SolidColorBrush(dark ? Color.FromRgb(237, 240, 244) : Colors.White),
            BorderBrush = new SolidColorBrush(BlendWith(Color.FromRgb(98, 112, 128), _gaugeColor, 0.3)),
            BorderThickness = new Thickness(size * 0.007)
        };
        TimerCanvas.Children.Add(face);
        Canvas.SetLeft(face, inset);
        Canvas.SetTop(face, inset);
    }

    private void DrawDial(double size)
    {
        var center = new Point(size / 2, size / 2);
        var faceInset = size * 0.085;
        var radius = size * 0.29;
        var labelRadius = size * 0.355;
        var tickOuter = size * 0.33;
        var ratio = Math.Clamp(_remaining.TotalMinutes / MaxMinutes, 0, 1);

        if (ratio > 0.001)
        {
            TimerCanvas.Children.Add(CreateSector(center, radius, ratio));
        }

        for (var minute = 0; minute < 60; minute++)
        {
            var degrees = minute * 6 - 90;
            var major = minute % 5 == 0;
            var inner = tickOuter - (major ? size * 0.032 : size * 0.018);
            var p1 = PointOnCircle(center, inner, degrees);
            var p2 = PointOnCircle(center, tickOuter, degrees);

            TimerCanvas.Children.Add(new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.Black,
                StrokeThickness = major ? 2.2 : 1.1,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });
        }

        for (var minute = 0; minute < 60; minute += 5)
        {
            var point = PointOnCircle(center, labelRadius, minute * 6 - 90);
            AddLabel(minute.ToString(CultureInfo.InvariantCulture), point, size * 0.04);
        }

        DrawPointer(center, radius, ratio, size);
        DrawKnob(center, size);

        AddBrand(size, faceInset);
    }

    private void DrawPointer(Point center, double radius, double ratio, double size)
    {
        var degrees = 360 * ratio - 90;
        var end = PointOnCircle(center, radius * 1.05, degrees);
        var tip = PointOnCircle(center, radius * 1.14, degrees);
        var baseCenter = PointOnCircle(center, size * 0.028, degrees + 180);
        var left = PointOnCircle(baseCenter, size * 0.045, degrees + 90);
        var right = PointOnCircle(baseCenter, size * 0.045, degrees - 90);

        var shadowOffset = new Vector(size * 0.012, size * 0.018);
        var shadowFigure = new PathFigure { StartPoint = left + shadowOffset, IsClosed = true };
        shadowFigure.Segments.Add(new LineSegment(tip + shadowOffset, true));
        shadowFigure.Segments.Add(new LineSegment(right + shadowOffset, true));
        shadowFigure.Segments.Add(new LineSegment(baseCenter + shadowOffset, true));

        TimerCanvas.Children.Add(new Path
        {
            Data = new PathGeometry(new[] { shadowFigure }),
            Fill = new SolidColorBrush(Color.FromArgb(48, 20, 28, 38))
        });

        var figure = new PathFigure { StartPoint = left, IsClosed = true };
        figure.Segments.Add(new LineSegment(tip, true));
        figure.Segments.Add(new LineSegment(right, true));
        figure.Segments.Add(new LineSegment(baseCenter, true));

        TimerCanvas.Children.Add(new Path
        {
            Data = new PathGeometry(new[] { figure }),
            Fill = new SolidColorBrush(Color.FromArgb(215, 235, 240, 246)),
            Stroke = new SolidColorBrush(Color.FromArgb(190, 134, 150, 168)),
            StrokeThickness = size * 0.005
        });

        TimerCanvas.Children.Add(new Line
        {
            X1 = center.X,
            Y1 = center.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = new SolidColorBrush(Color.FromArgb(125, 116, 132, 150)),
            StrokeThickness = size * 0.009,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }

    private void DrawKnob(Point center, double size)
    {
        var knobSize = size * 0.125;
        var shadow = new Ellipse
        {
            Width = knobSize * 1.08,
            Height = knobSize * 1.08,
            Fill = new SolidColorBrush(Color.FromArgb(70, 20, 28, 38))
        };
        TimerCanvas.Children.Add(shadow);
        Canvas.SetLeft(shadow, center.X - knobSize * 0.46);
        Canvas.SetTop(shadow, center.Y - knobSize * 0.36);

        var knob = new Ellipse
        {
            Width = knobSize,
            Height = knobSize,
            Fill = new LinearGradientBrush(Color.FromRgb(239, 244, 249), Color.FromRgb(168, 181, 196), 135),
            Stroke = new SolidColorBrush(Color.FromRgb(142, 156, 172)),
            StrokeThickness = size * 0.006
        };
        TimerCanvas.Children.Add(knob);
        Canvas.SetLeft(knob, center.X - knobSize / 2);
        Canvas.SetTop(knob, center.Y - knobSize / 2);
    }

    private void AddLabel(string text, Point center, double fontSize)
    {
        var label = new TextBlock
        {
            Text = text,
            Foreground = Brushes.Black,
            FontWeight = FontWeights.SemiBold,
            FontSize = fontSize,
            FontFamily = new FontFamily("Segoe UI"),
            TextAlignment = TextAlignment.Center
        };
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        TimerCanvas.Children.Add(label);
        Canvas.SetLeft(label, center.X - label.DesiredSize.Width / 2);
        Canvas.SetTop(label, center.Y - label.DesiredSize.Height / 2);
    }

    private void AddBrand(double size, double inset)
    {
        var label = new TextBlock
        {
            Text = "VISUAL TIMER",
            Foreground = new SolidColorBrush(Color.FromRgb(130, 138, 148)),
            FontSize = size * 0.023,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Segoe UI")
        };
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        TimerCanvas.Children.Add(label);
        Canvas.SetLeft(label, size - inset - label.DesiredSize.Width - size * 0.04);
        Canvas.SetTop(label, size - inset - label.DesiredSize.Height - size * 0.026);
    }

    private Path CreateSector(Point center, double radius, double ratio)
    {
        var sweepAngle = Math.Min(359.99, 360 * ratio);
        var start = PointOnCircle(center, radius, -90);
        var end = PointOnCircle(center, radius, -90 + sweepAngle);

        var figure = new PathFigure { StartPoint = center, IsClosed = true };
        figure.Segments.Add(new LineSegment(start, true));
        figure.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = sweepAngle > 180
        });

        return new Path
        {
            Data = new PathGeometry(new[] { figure }),
            Fill = new SolidColorBrush(_gaugeColor)
        };
    }

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private static Color BlendWith(Color baseColor, Color tint, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(baseColor.R + (tint.R - baseColor.R) * amount),
            (byte)Math.Round(baseColor.G + (tint.G - baseColor.G) * amount),
            (byte)Math.Round(baseColor.B + (tint.B - baseColor.B) * amount));
    }

    private void PresetColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string colorText })
        {
            _gaugeColor = (Color)ColorConverter.ConvertFromString(colorText);
            SaveUserSettings();
            DrawTimer();
        }
    }

    private void PickColorButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(_gaugeColor.R, _gaugeColor.G, _gaugeColor.B),
            FullOpen = true
        };

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            _gaugeColor = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
            SaveUserSettings();
            DrawTimer();
        }
    }

    private void ThemeControl_Changed(object sender, RoutedEventArgs e) => ApplyTheme();

    private void ApplyTheme()
    {
        var dark = DarkModeMenuItem?.IsChecked == true;
        WindowBackgroundBrush = new SolidColorBrush(dark ? Color.FromRgb(24, 27, 31) : Color.FromRgb(246, 248, 251));
        ForegroundBrush = new SolidColorBrush(dark ? Color.FromRgb(242, 245, 248) : Color.FromRgb(28, 34, 42));
        PanelBrush = new SolidColorBrush(dark ? Color.FromRgb(44, 49, 56) : Color.FromRgb(255, 255, 255));
        DrawTimer();
    }

    private void TopmostControl_Changed(object sender, RoutedEventArgs e)
    {
        Topmost = TopmostMenuItem.IsChecked;
    }

    private void BorderlessControl_Changed(object sender, RoutedEventArgs e)
    {
        var floating = BorderlessMenuItem.IsChecked;
        SettingsMenu.Visibility = floating ? Visibility.Collapsed : Visibility.Visible;
        ControlsPanel.Visibility = floating ? Visibility.Collapsed : Visibility.Visible;
        WindowStyle = floating ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        ResizeMode = floating ? ResizeMode.NoResize : ResizeMode.CanResize;
        Topmost = floating || TopmostMenuItem.IsChecked;
        TopmostMenuItem.IsChecked = Topmost;
        Root.Margin = floating ? new Thickness(10) : new Thickness(24);
        DrawTimer();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && BorderlessMenuItem.IsChecked)
        {
            BorderlessMenuItem.IsChecked = false;
        }
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        var settingsPath = GetSettingsPath();

        var aboutWindow = new Window
        {
            Title = "About Visual Progress Timer",
            Owner = this,
            Width = 420,
            Height = 330,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = WindowBackgroundBrush,
            Icon = Icon
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(26),
            VerticalAlignment = VerticalAlignment.Stretch
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Visual Progress Timer",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = ForegroundBrush
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Version {version}",
            Margin = new Thickness(0, 4, 0, 18),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(114, 125, 138))
        });

        panel.Children.Add(new TextBlock
        {
            Text = "A simple 60-minute visual timer for Windows. Drag the clock face to set time, then press Start.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            LineHeight = 21,
            Foreground = ForegroundBrush
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Settings are saved to:\n{settingsPath}",
            Margin = new Thickness(0, 18, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            LineHeight = 18,
            Foreground = new SolidColorBrush(Color.FromRgb(114, 125, 138))
        });

        panel.Children.Add(new Button
        {
            Content = "OK",
            Width = 82,
            Margin = new Thickness(0, 26, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true,
            IsCancel = true
        });

        ((Button)panel.Children[^1]).Click += (_, _) => aboutWindow.Close();
        aboutWindow.Content = panel;
        aboutWindow.ShowDialog();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => DrawTimer();

    private void NotifyTimeUp()
    {
        PlayAlarmSound();

        _notifyIcon?.Dispose();
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Visible = true,
            BalloonTipTitle = "Visual Progress Timer",
            BalloonTipText = "Time is up."
        };
        _notifyIcon.ShowBalloonTip(4000);

        var cleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        cleanupTimer.Tick += (_, _) =>
        {
            cleanupTimer.Stop();
            _notifyIcon?.Dispose();
            _notifyIcon = null;
        };
        cleanupTimer.Start();
    }

    private void PlayAlarmSound()
    {
        var windowsMediaPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
        var candidates = new[]
        {
            "Alarm01.wav",
            "Alarm02.wav",
            "Alarm03.wav",
            "Windows Notify Calendar.wav"
        };

        foreach (var candidate in candidates)
        {
            var path = System.IO.Path.Combine(windowsMediaPath, candidate);
            if (!System.IO.File.Exists(path))
            {
                continue;
            }

            _alarmPlayer?.Stop();
            _alarmPlayer?.Dispose();
            _alarmPlayer = new SoundPlayer(path);
            _alarmPlayer.Play();
            return;
        }

        PlaySystemSoundPattern();
    }

    private static void PlaySystemSoundPattern()
    {
        var count = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        timer.Tick += (_, _) =>
        {
            if (count >= 5)
            {
                timer.Stop();
                return;
            }

            SystemSounds.Exclamation.Play();
            count++;
        };
        SystemSounds.Exclamation.Play();
        count++;
        timer.Start();
    }

    private void LoadUserSettings()
    {
        try
        {
            var path = GetSettingsPath();
            if (!System.IO.File.Exists(path))
            {
                return;
            }

            var json = System.IO.File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            if (settings is null)
            {
                return;
            }

            var minutes = Math.Clamp(settings.Minutes, 1, MaxMinutes);
            _duration = TimeSpan.FromMinutes(minutes);
            _remaining = _duration;

            if (!string.IsNullOrWhiteSpace(settings.GaugeColor))
            {
                _gaugeColor = (Color)ColorConverter.ConvertFromString(settings.GaugeColor);
            }
        }
        catch
        {
            _duration = TimeSpan.FromMinutes(45);
            _remaining = _duration;
            _gaugeColor = Color.FromRgb(32, 42, 68);
        }
    }

    private void SaveUserSettings()
    {
        try
        {
            var path = GetSettingsPath();
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            var settings = new UserSettings
            {
                Minutes = Math.Clamp((int)Math.Round(_duration.TotalMinutes), 1, MaxMinutes),
                GaugeColor = _gaugeColor.ToString(CultureInfo.InvariantCulture)
            };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }
        catch
        {
            // Settings persistence should never prevent timer use.
        }
    }

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(appData, "VisualProgressTimer", "settings.json");
    }

    private sealed class UserSettings
    {
        public int Minutes { get; set; } = 45;

        public string GaugeColor { get; set; } = "#FF202A44";
    }
}
