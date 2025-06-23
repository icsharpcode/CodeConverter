
private bool TestMethod(int? newDays, int? oldDays)
{
    return newDays.HasValue && oldDays.HasValue && newDays != oldDays;
}