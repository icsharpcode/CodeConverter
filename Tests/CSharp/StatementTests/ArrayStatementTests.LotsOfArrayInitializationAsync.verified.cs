
internal partial class TestClass
{
    private void TestMethod()
    {
        // Declare a single-dimension array of 5 numbers.
        var numbers1 = new int[5];

        // Declare a single-dimension array and set its 4 values.
        int[] numbers2 = new int[] { 1, 2, 4, 8 };

        // Declare a 6 x 6 multidimensional array.
        var matrix1 = new double[6, 6];

        // Declare a 4 x 3 multidimensional array and set array element values.
        int[,] matrix2 = new int[4, 3] { { 1, 2, 3 }, { 2, 3, 4 }, { 3, 4, 5 }, { 4, 5, 6 } };

        // Combine rank specifiers with initializers of various kinds
        double[,] rankSpecifiers = new double[2, 2] { { 1.0d, 2.0d }, { 3.0d, 4.0d } };
        double[,] rankSpecifiers2 = new double[2, 2];

        // Declare a jagged array
        double[][] sales = new double[12][];
    }
}