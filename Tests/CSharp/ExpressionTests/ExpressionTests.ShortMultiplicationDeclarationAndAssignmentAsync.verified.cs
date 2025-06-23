
public partial class Compound
{
    public void Operators()
    {
        short aShort = 123;
        short anotherShort = 234;
        short x = (short)(aShort * anotherShort);
        x *= aShort; // Implicit cast in C# due to compound operator
        x = (short)(aShort * x);
    }
}