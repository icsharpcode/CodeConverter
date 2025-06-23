
internal partial class IndexedPropertyWithTrivia
{
    // a
    // b
    public int get_P(int i)
    {
        // 1
        int x = 1; // 2
        return default;
        // 3
    }

    // c
    public void set_P(int i, int value)
    {
        // 4
        int x = 1; // 5
                   // 6
        x = value + i; // 7
                       // 8
                       // d
    }
}