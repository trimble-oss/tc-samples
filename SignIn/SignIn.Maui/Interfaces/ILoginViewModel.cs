namespace SignIn.Maui
{
    public interface ILoginViewModel
    {
        event Action SignOut;

        void DoSilentLogin();

        void DoLogOut();
    }
}
