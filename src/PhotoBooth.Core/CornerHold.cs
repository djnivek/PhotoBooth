namespace PhotoBooth.Core;

/// <summary>Monotonic, one-shot hold. Leaving or releasing the corner cancels the gesture.</summary>
public sealed class CornerHold(TimeProvider? clock = null)
{
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private long? began;
    public void Begin() => began = time.GetTimestamp();
    public void Cancel() => began = null;
    public bool Poll()
    {
        if (began is not { } start || time.GetElapsedTime(start) < TimeSpan.FromSeconds(3)) return false;
        began = null; return true;
    }
}
