using System;

internal partial class TestClass
{
    private static bool Log(string message)
    {
        Console.WriteLine(message);
        return false;
    }

    private void TestMethod(int number)
    {
        try
        {
            Console.WriteLine("try");
        }
        catch (Exception e)
        {
            Console.WriteLine("catch1");
        }
        catch
        {
            Console.WriteLine("catch all");
        }
        finally
        {
            Console.WriteLine("finally");
        }

        try
        {
            Console.WriteLine("try");
        }
        catch (NotImplementedException e2)
        {
            Console.WriteLine("catch1");
        }
        catch (Exception e) when (Log(e.Message))
        {
            Console.WriteLine("catch2");
        }

        try
        {
            Console.WriteLine("try");
        }
        finally
        {
            Console.WriteLine("finally");
        }
    }
}