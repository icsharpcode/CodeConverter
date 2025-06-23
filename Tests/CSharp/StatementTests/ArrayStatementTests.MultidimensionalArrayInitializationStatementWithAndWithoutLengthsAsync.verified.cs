
internal partial class TestClass
{
    private void TestMethod()
    {
        int[,] a = new int[,] { { 1, 2 }, { 3, 4 } };
        int[,] b = new int[2, 2] { { 1, 2 }, { 3, 4 } };
        int[,,] c = new int[,,] { { { 1 } } };
        int[,,] d = new int[1, 1, 1] { { { 1 } } };
        int[][,] e = new int[][,] { };
        int[][,] f = new int[0][,];
        int[][,] g = new int[1][,];
    }
}