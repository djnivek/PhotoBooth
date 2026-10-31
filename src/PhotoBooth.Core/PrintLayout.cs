namespace PhotoBooth.Core;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);
public sealed record SheetLayout(int Width, int Height, int Dpi, IReadOnlyList<PixelRect> Photos);

public static class PrintLayout
{
    // Same landscape 6x4 master and 5mm safe margin. Squares are never cropped;
    // the remaining right-hand area belongs to the party artwork, not the photo.
    public static SheetLayout Metamorphosis(int count)
    {
        if (count is not (1 or 4)) throw new ArgumentOutOfRangeException(nameof(count));
        var side = count == 1 ? 1080 : 528;
        var cells = Enumerable.Range(0, count).Select(i => new PixelRect(
            60 + (i % 2) * (side + 24), 60 + (i / 2) * (side + 24), side, side)).ToArray();
        return new SheetLayout(1800, 1200, 300, Array.AsReadOnly(cells));
    }

    // Logical 6 x 4 inch landscape master. Driver bleed/imageable area is calibrated on Windows.
    public static SheetLayout FourBySix(int count, int safeMargin = 60, int gutter = 24)
    {
        if (count is not (1 or 4)) throw new ArgumentOutOfRangeException(nameof(count));
        if (safeMargin is < 0 or > 240) throw new ArgumentOutOfRangeException(nameof(safeMargin));
        if (gutter is < 0 or > 120) throw new ArgumentOutOfRangeException(nameof(gutter));
        const int width = 1800, height = 1200, dpi = 300;
        var availableWidth = width - 2 * safeMargin;
        var availableHeight = height - 2 * safeMargin;
        if (count == 1)
            return new SheetLayout(width, height, dpi, Array.AsReadOnly(new[] { new PixelRect(safeMargin, safeMargin, availableWidth, availableHeight) }));
        var cellWidth = (availableWidth - gutter) / 2;
        var cellHeight = (availableHeight - gutter) / 2;
        var cells = Enumerable.Range(0, 4).Select(i => new PixelRect(
            safeMargin + (i % 2) * (cellWidth + gutter),
            safeMargin + (i / 2) * (cellHeight + gutter), cellWidth, cellHeight)).ToArray();
        return new SheetLayout(width, height, dpi, Array.AsReadOnly(cells));
    }
}
