
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue && oldDays.HasValue || true ? newDays.HasValue && oldDays.HasValue ? newDays.Value > oldDays.Value : null : (bool?)false);
}