private static void SimpleQuery()
{
    int[] numbers = new[] { 7, 9, 5, 3, 6 };
    var res = from n in numbers
              where n > 5
              select n;
    foreach (var n in res)
        Console.WriteLine(n);
}