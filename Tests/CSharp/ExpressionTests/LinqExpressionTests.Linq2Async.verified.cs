public static void Linq40()
{
    int[] numbers = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
    var numberGroups = from n in numbers
                       group n by (n % 5) into g
                       let __groupByKey1__ = g.Key
                       select new { Remainder = __groupByKey1__, Numbers = g };

    foreach (var g in numberGroups)
    {
        Console.WriteLine($"Numbers with a remainder of {g.Remainder} when divided by 5:");

        foreach (var n in g.Numbers)
            Console.WriteLine(n);
    }
}