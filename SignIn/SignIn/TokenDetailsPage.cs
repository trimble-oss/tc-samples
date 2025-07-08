namespace SignIn
{
    using System;

    using Trimble.Identity;
    using Xamarin.Forms;

    /// <summary>
    /// Token details page.
    /// </summary>
    public class TokenDetailsPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenDetailsPage"/> class.
        /// </summary>
        public TokenDetailsPage(AuthenticationContext authCtx, TokenCacheItem token)
        {
            Button refresh, clear;
            Label time;

            this.Title = "Token Details";

            this.Content = new StackLayout
            {
                Padding = 20,
                Spacing = 20,

                Children =
                {
                    new Label { Text = token.DisplayableId },
                    new Label { Text = token.UniqueId },
                    (time = new Label 
                    { 
                        Text = token.ExpiresOn.ToLocalTime().ToString(), 
                        TextColor = token.ExpiresOn.ToUniversalTime() > DateTimeOffset.UtcNow ? Color.Green : Color.Red
                    }),
                    
                    (refresh = new Button
                    {
                        Text = "Refresh",
                        TextColor = Color.White,
                        BackgroundColor = Color.FromHex("77D065"),
                    }),

                    (clear = new Button
                    {
                        Text = "Delete token",
                        TextColor = Color.White,
                        BackgroundColor = Color.FromHex("77D065"),
                    }),
                },
            };

            refresh.Clicked += async (sender, args) =>
            {
                this.IsBusy = true;

                try
                {
                    var errorMessage = string.Empty;

                    try
                    {
                        var accessToken = await authCtx.AcquireTokenSilentAsync(new UserIdentifier(token.UniqueId, UserIdentifierType.UniqueId));
                        accessToken = await authCtx.AcquireTokenByRefreshTokenAsync(accessToken);
                        time.Text = accessToken.ExpiresOn.ToLocalTime().ToString();
                        time.TextColor = Color.Green;
                    }
                    catch (AuthenticationException e)
                    {
                        errorMessage = e.Message;
                    }

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        await this.DisplayAlert("Sign-in failed", errorMessage, "OK");
                    }
                }
                finally
                {
                    this.IsBusy = false;
                }
            };

            clear.Clicked += (sender, args) =>
            {
                authCtx.TokenCache.DeleteItem(token);
                this.Navigation.PopAsync();
            };
        }
    }
}
