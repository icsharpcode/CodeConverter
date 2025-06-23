using System;
using System.Collections.Generic;

public partial class Classinator769
{
    private Dictionary<int, string> _dictionary = new Dictionary<int, string>();

    private void AccessDictionary()
    {
        if (_dictionary[2] == "StringyMcStringface")
        {
            Console.WriteLine("It is true");
        }
    }
}