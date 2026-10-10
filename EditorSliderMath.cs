namespace SprocketJsonEditor;
internal static class EditorSliderMath
{
    internal static bool IsValid(EditorNumberRange range)=>double.IsFinite(range.Minimum)&&double.IsFinite(range.Maximum)
        &&double.IsFinite(range.Step)&&range.Minimum<range.Maximum&&range.Step>0
        &&range.Minimum>=-float.MaxValue&&range.Maximum<=float.MaxValue;
    internal static double Snap(double value,EditorNumberRange range)
    {
        if(!IsValid(range)||!double.IsFinite(value))throw new ArgumentOutOfRangeException(nameof(value));
        var clamped=Math.Clamp(value,range.Minimum,range.Maximum);
        return Math.Clamp(range.Minimum+Math.Round((clamped-range.Minimum)/range.Step)*range.Step,range.Minimum,range.Maximum);
    }
}
