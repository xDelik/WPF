using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using WPF.Models;
using WPF.ViewModels;

namespace WPF.Views;

public partial class CalendarView : UserControl
{
    private const double EdgeZone = 8;
    private const double ClickThreshold = 4;

    private static readonly Cursor ResizeCursor = new(StandardCursorType.SizeWestEast);
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);

    private enum DragMode { None, Move, ResizeLeft, ResizeRight }

    private DragMode _mode = DragMode.None;
    private CalendarBar? _bar;
    private Border? _barBorder;
    private Grid? _rowPanel;
    private Point _pressPoint;
    private double _dayWidth;
    private double _baseWidth;
    private int _deltaDays;
    private bool _dragMoved;

    public CalendarView() => InitializeComponent();

    private CalendarViewModel? Vm => DataContext as CalendarViewModel;

    private static DragMode ModeForPosition(Border border, double x, CalendarBar bar)
    {
        if (bar.Reservation.Status != ReservationStatus.Confirmed) return DragMode.None;

        var edge = Math.Min(EdgeZone, border.Bounds.Width / 3);
        if (x <= edge && !bar.LeftClipped) return DragMode.ResizeLeft;
        if (x >= border.Bounds.Width - edge && !bar.RightClipped) return DragMode.ResizeRight;
        return DragMode.Move;
    }

    private void OnBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not CalendarBar bar) return;
        if (!e.GetCurrentPoint(border).Properties.IsLeftButtonPressed) return;

        _rowPanel = border.FindAncestorOfType<Grid>();
        if (_rowPanel is null) return;

        _bar = bar;
        _barBorder = border;
        _pressPoint = e.GetPosition(_rowPanel);
        _dayWidth = _rowPanel.Bounds.Width / CalendarViewModel.WindowDays;
        _baseWidth = border.Bounds.Width;
        _deltaDays = 0;
        _dragMoved = false;
        _mode = ModeForPosition(border, e.GetPosition(border).X, bar);

        e.Pointer.Capture(border);
        e.Handled = true;
    }

    private void OnBarPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Border border) return;

        if (_bar is null || !ReferenceEquals(border, _barBorder))
        {
            if (border.DataContext is CalendarBar hoverBar)
                border.Cursor = ModeForPosition(border, e.GetPosition(border).X, hoverBar) switch
                {
                    DragMode.ResizeLeft or DragMode.ResizeRight => ResizeCursor,
                    _ => HandCursor
                };
            return;
        }

        if (_rowPanel is null) return;
        var pos = e.GetPosition(_rowPanel);
        if (Math.Abs(pos.X - _pressPoint.X) > ClickThreshold || Math.Abs(pos.Y - _pressPoint.Y) > ClickThreshold)
            _dragMoved = true;
        if (_mode == DragMode.None || !_dragMoved) return;

        _deltaDays = ClampDelta((int)Math.Round((pos.X - _pressPoint.X) / _dayWidth));
        ApplyPreview();
    }

    private int ClampDelta(int delta)
    {
        if (_bar is null) return 0;
        var start = _bar.StartColumn;
        var end = _bar.StartColumn + _bar.SpanColumns;
        return _mode switch
        {
            DragMode.Move => Math.Clamp(delta, -start, CalendarViewModel.WindowDays - end),
            DragMode.ResizeLeft => Math.Clamp(delta, -start, _bar.SpanColumns - 1),
            DragMode.ResizeRight => Math.Clamp(delta, 1 - _bar.SpanColumns, CalendarViewModel.WindowDays - end),
            _ => 0
        };
    }

    private void ApplyPreview()
    {
        if (_barBorder is null) return;
        var dx = _deltaDays * _dayWidth;
        _barBorder.Opacity = 0.7;
        switch (_mode)
        {
            case DragMode.Move:
                _barBorder.RenderTransform = new TranslateTransform(dx, 0);
                break;
            case DragMode.ResizeLeft:
                _barBorder.HorizontalAlignment = HorizontalAlignment.Right;
                _barBorder.Width = _baseWidth - dx;
                break;
            case DragMode.ResizeRight:
                _barBorder.HorizontalAlignment = HorizontalAlignment.Left;
                _barBorder.Width = _baseWidth + dx;
                break;
        }
    }

    private void OnBarPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_bar is null || _barBorder is null) return;

        var bar = _bar;
        var mode = _mode;
        var delta = _deltaDays;
        var moved = _dragMoved;
        ResetDrag();
        e.Pointer.Capture(null);

        if (!moved)
        {
            Vm?.OpenReservationCommand.Execute(bar.Reservation);
            return;
        }
        if (mode == DragMode.None || delta == 0) return;

        var r = bar.Reservation;
        var (checkIn, checkOut) = mode switch
        {
            DragMode.Move => (r.CheckInDate.AddDays(delta), r.CheckOutDate.AddDays(delta)),
            DragMode.ResizeLeft => (r.CheckInDate.AddDays(delta), r.CheckOutDate),
            _ => (r.CheckInDate, r.CheckOutDate.AddDays(delta))
        };
        Vm?.TryReschedule(r, checkIn, checkOut);
    }

    private void OnBarPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => ResetDrag();

    private Room? _cellRoom;
    private UniformGrid? _cellPanel;
    private Border? _selectionBorder;
    private int _cellStartDay;
    private int _cellEndDay;

    private void OnCellPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border cell || cell.DataContext is not CalendarCell data) return;
        if (!e.GetCurrentPoint(cell).Properties.IsLeftButtonPressed) return;

        _cellPanel = cell.FindAncestorOfType<UniformGrid>();
        _selectionBorder = cell.FindAncestorOfType<Grid>()?.Children
            .OfType<Border>()
            .FirstOrDefault(b => b.Classes.Contains("rangeSelection"));
        if (_cellPanel is null || _selectionBorder is null) return;

        _cellRoom = data.Room;
        _cellStartDay = DayAt(e.GetPosition(_cellPanel).X);
        _cellEndDay = _cellStartDay;
        UpdateSelection();

        e.Pointer.Capture(cell);
        e.Handled = true;
    }

    private void OnCellPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_cellRoom is null || _cellPanel is null) return;
        _cellEndDay = DayAt(e.GetPosition(_cellPanel).X);
        UpdateSelection();
    }

    private void OnCellPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_cellRoom is null) return;

        var room = _cellRoom;
        var first = Math.Min(_cellStartDay, _cellEndDay);
        var last = Math.Max(_cellStartDay, _cellEndDay);
        ResetCellDrag();
        e.Pointer.Capture(null);

        if (Vm is { } vm)
            vm.SelectRange(room, vm.WindowStart.AddDays(first), vm.WindowStart.AddDays(last + 1));
    }

    private void OnCellPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => ResetCellDrag();

    private int DayAt(double x)
    {
        if (_cellPanel is null) return 0;
        var dayWidth = _cellPanel.Bounds.Width / CalendarViewModel.WindowDays;
        return Math.Clamp((int)(x / dayWidth), 0, CalendarViewModel.WindowDays - 1);
    }

    private void UpdateSelection()
    {
        if (_selectionBorder is null) return;
        var first = Math.Min(_cellStartDay, _cellEndDay);
        var last = Math.Max(_cellStartDay, _cellEndDay);
        Grid.SetColumn(_selectionBorder, first);
        Grid.SetColumnSpan(_selectionBorder, last - first + 1);
        _selectionBorder.IsVisible = true;
    }

    private void ResetCellDrag()
    {
        if (_selectionBorder is not null) _selectionBorder.IsVisible = false;
        _cellRoom = null;
        _cellPanel = null;
        _selectionBorder = null;
    }

    private void ResetDrag()
    {
        if (_barBorder is not null)
        {
            _barBorder.RenderTransform = null;
            _barBorder.Width = double.NaN;
            _barBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
            _barBorder.Opacity = 1;
        }
        _bar = null;
        _barBorder = null;
        _rowPanel = null;
        _mode = DragMode.None;
        _deltaDays = 0;
        _dragMoved = false;
    }
}
