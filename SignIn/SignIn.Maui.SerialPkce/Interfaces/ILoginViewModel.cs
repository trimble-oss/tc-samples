namespace SignIn.Maui.SerialPkce
{
    public interface ILoginViewModel
    {
        event Action SignOut;

        void DoSilentLogin();

        void DoLogOut();
    }
}
