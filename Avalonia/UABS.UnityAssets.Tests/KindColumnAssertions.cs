using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace UABS.UnityAssets.Tests;

internal static class KindColumnAssertions
{
    public static void VerifyBoundedResizing(DataGrid grid)
    {
        DataGridColumn kind = grid.Columns[0];
        Window window = Assert.IsType<Window>(TopLevel.GetTopLevel(grid));
        Assert.Equal(0, kind.DisplayIndex);
        Assert.True(grid.CanUserResizeColumns);
        Assert.True(kind.CanUserResize);
        Assert.Equal(52, kind.Width.Value);
        Assert.Equal(80, kind.MaxWidth);
        Assert.Equal(new DataGrid().MinColumnWidth, grid.MinColumnWidth);
        Assert.Equal(new DataGridTemplateColumn().MinWidth, kind.MinWidth);

        Resize(window, grid, 500);
        Assert.Equal(80, kind.ActualWidth, precision: 1);
        Resize(window, grid, -500);
        Assert.Equal(0, kind.ActualWidth, precision: 1);

        DataGridRow[] rows = grid.GetVisualDescendants().OfType<DataGridRow>()
            .Where(row => row.Index >= 0).ToArray();
        Assert.NotEmpty(rows);
        foreach (DataGridRow row in rows)
        {
            DataGridCell cell = row.GetVisualDescendants().OfType<DataGridCell>().First();
            Assert.True(cell.ClipToBounds);
            // Avalonia either arranges a zero-width cell or virtualizes it out while
            // retaining its previous bounds. Neither can paint an icon into Name.
            Assert.True(!cell.IsEffectivelyVisible || cell.Bounds.Width == 0);
        }

        // Avalonia's adjacent header left edge resizes the preceding zero-width column.
        Resize(window, grid, 52, restoreHidden: true);
        Assert.Equal(52, kind.ActualWidth, precision: 1);
        grid.Columns[1].Width = new DataGridLength(280);
        grid.UpdateLayout();
        Assert.Equal(52, kind.ActualWidth, precision: 1);
    }

    private static void Resize(Window window, DataGrid grid, double delta, bool restoreHidden = false)
    {
        DataGridColumnHeader header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>()
            .Single(candidate => ReferenceEquals(candidate.Content, grid.Columns[restoreHidden ? 1 : 0].Header));
        Point start = header.TranslatePoint(new Point(restoreHidden ? 1 : header.Bounds.Width - 1, header.Bounds.Height / 2), window)!.Value;
        Point end = new(start.X + delta, start.Y);
        window.MouseMove(start, RawInputModifiers.None);
        window.MouseDown(start, MouseButton.Left, RawInputModifiers.None);
        window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        window.MouseUp(end, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        grid.UpdateLayout();
    }
}
