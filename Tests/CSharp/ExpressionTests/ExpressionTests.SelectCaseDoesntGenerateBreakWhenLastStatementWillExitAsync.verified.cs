using System;

public partial class Test
{
    public int OnLoad()
    {
        int x = 5;
        while (true)
        {
            switch (x)
            {
                case 0:
                    {
                        continue;
                    }
                case 1:
                    {
                        x = 1;
                        break;
                    }
                case 2:
                    {
                        return 2;
                    }
                case 3:
                    {
                        throw new Exception();
                    }
                case 4:
                    {
                        if (true)
                        {
                            x = 4;
                        }
                        else
                        {
                            return x;
                        }

                        break;
                    }
                case 5:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else
                        {
                            x = 5;
                        }

                        break;
                    }
                case 6:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            x = 6;
                        }
                        else
                        {
                            return x;
                        }

                        break;
                    }
                case 7:
                    {
                        if (true)
                        {
                            return x;
                        }

                        break;
                    }
                case 8:
                    {
                        if (true)
                            return x;
                        break;
                    }
                case 9:
                    {
                        if (true)
                            x = 9;
                        break;
                    }
                case 10:
                    {
                        if (true)
                            return x;
                        else
                            x = 10;
                        break;
                    }
                case 11:
                    {
                        if (true)
                            x = 11;
                        else
                            return x;
                        break;
                    }
                case 12:
                    {
                        if (true)
                            return x;
                        else
                            return x;
                    }
                case 13:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            continue;
                        }
                        else if (false)
                        {
                            throw new Exception();
                        }
                        else if (false)
                        {
                            break;
                        }
                        else
                        {
                            return x;
                        }
                    }
                case 14:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            break;
                        }

                        break;
                    }

                default:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else
                        {
                            return x;
                        }
                    }
            }
        }
        return x;
    }
}