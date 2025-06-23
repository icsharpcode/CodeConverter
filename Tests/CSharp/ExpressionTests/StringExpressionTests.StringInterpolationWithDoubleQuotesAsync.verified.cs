using System;

namespace InnerNamespace
{
    public partial class Test
    {
        public string StringInter(string t, DateTime dt)
        {
            string a = $"pre{t} t";
            string b = $"pre{t} \" t";
            string c = $@"pre{t} ""\ t";
            string d = $"pre{t + "\""} \" t";
            string e = $@"pre{t + "\""} ""\ t";
            string f = $"pre{{escapedBraces}}{dt,4:hh}";
            return a + b + c + d + e + f;
        }
    }
}