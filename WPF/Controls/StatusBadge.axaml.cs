using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WPF.Controls;

public partial class StatusBadge : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<StatusBadge, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<IBrush?> DotBrushProperty =
        AvaloniaProperty.Register<StatusBadge, IBrush?>(nameof(DotBrush));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public IBrush? DotBrush
    {
        get => GetValue(DotBrushProperty);
        set => SetValue(DotBrushProperty, value);
    }

    public StatusBadge() => InitializeComponent();
}
