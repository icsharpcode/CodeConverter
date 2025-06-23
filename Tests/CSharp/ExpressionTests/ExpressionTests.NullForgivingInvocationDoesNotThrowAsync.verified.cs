using System;

public partial class AClass
{
    public static void Identify(ITraceMessageTalker talker)
    {
        talker?.IdentifyTalker(IdentityTraceMessage());
    }

    private static object IdentityTraceMessage()
    {
        throw new NotImplementedException();
    }
}

public partial interface ITraceMessageTalker
{
    object IdentifyTalker(object v);
}