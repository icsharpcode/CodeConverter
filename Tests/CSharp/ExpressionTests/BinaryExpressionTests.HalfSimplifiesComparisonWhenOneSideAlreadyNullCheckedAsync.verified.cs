
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue ? oldDays.HasValue ? newDays < oldDays.Value : null : (bool?)false);
}