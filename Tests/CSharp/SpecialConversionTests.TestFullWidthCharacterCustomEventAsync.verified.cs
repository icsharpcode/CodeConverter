using System;

internal partial class ＴｅｓｔＣｌａｓｓ４５
{
    private event EventHandler ｂａｃｋｉｎｇＦｉｅｌｄ;

    public event EventHandler ＭｙＥｖｅｎｔ
    {
        add
        {
            ｂａｃｋｉｎｇＦｉｅｌｄ += value;
        }
        remove
        {
            ｂａｃｋｉｎｇＦｉｅｌｄ -= value;
        }
    } // ＲａｉｓｅＥｖｅｎｔ　ｍｏｖｅｓ　ｏｕｔｓｉｄｅ　ｔｈｉｓ　ｂｌｏｃｋ 'Workaround test code not noticing ’ symbol
    void OnＭｙＥｖｅｎｔ(object ｓｅｎｄｅｒ, EventArgs ｅ)
    {
        Console.WriteLine("Ｅｖｅｎｔ　Ｒａｉｓｅｄ");
    }

    public void ＲａｉｓｅＣｕｓｔｏｍＥｖｅｎｔ()
    {
        OnＭｙＥｖｅｎｔ(this, EventArgs.Empty);
    }
}