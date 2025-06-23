{
    int? newDays = 1;
    int? oldDays = default;

    if (newDays.HasValue && !oldDays.HasValue || newDays.HasValue && oldDays.HasValue && newDays != oldDays || !newDays.HasValue && oldDays.HasValue)

    {

        // Some code
    }
}