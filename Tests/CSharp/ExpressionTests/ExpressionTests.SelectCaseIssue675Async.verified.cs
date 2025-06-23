
public partial class EnumTest
{
    public enum UserInterface
    {
        Unknown,
        Spectrum,
        Wisdom
    }

    public void OnLoad(UserInterface? ui)
    {
        int activity = 0;
        switch (ui)
        {
            case object _ when ui is null:
                {
                    activity = 1;
                    break;
                }
            case UserInterface.Spectrum:
                {
                    activity = 2;
                    break;
                }
            case UserInterface.Wisdom:
                {
                    activity = 3;
                    break;
                }

            default:
                {
                    activity = 4;
                    break;
                }
        }
    }
}