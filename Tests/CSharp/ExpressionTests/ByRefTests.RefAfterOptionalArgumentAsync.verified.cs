
public void S([Optional, DefaultParameterValue(0)] int a, [Optional, DefaultParameterValue(0)] ref int b)
{
    int argb = 0;
    S(b: ref argb);
}