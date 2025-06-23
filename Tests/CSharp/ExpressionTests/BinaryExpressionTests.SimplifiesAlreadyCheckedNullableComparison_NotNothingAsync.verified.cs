
private bool TestMethod(int? newDays, int? oldDays)
{
    return newDays is not null && oldDays is not null && newDays == oldDays;
}