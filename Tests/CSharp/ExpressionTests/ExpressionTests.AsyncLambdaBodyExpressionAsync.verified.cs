using System;
using System.Threading.Tasks;

internal partial class TestClass
{
    private async void TestMethod()
    {
        Func<Task<int>> test0 = async () => 2;
        Func<int, Task<int>> test1 = async a => a * 2;
        Func<int, int, Task<double>> test2 = async (a, b) =>
        {
            if (b > 0)
                return a / (double)b;
            return 0d;
        };

        Func<int, int, Task<int>> test3 = async (a, b) => a % b;
        Func<Task<int>> test4 = async () =>
        {
            int i = 2;
            int x = 3;
            return 3;
        };

        await test1(3);
    }
}