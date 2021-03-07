using System.Net;
using System.Threading;
using System.Threading.Tasks;
using F1Interface.Contracts;
using F1Interface.Domain;
using F1Interface.Domain.Responses;
using Microsoft.Extensions.Logging;
using PlaywrightSharp;

namespace F1Interface
{
    public sealed class AuthenticationService : ServiceBase<AuthenticationService>, IAuthenticationService
    {
        internal static readonly ViewportSize ViewportSize = new ViewportSize { Height = 969, Width = 1920 };
        public const string IdentityProvider = "/api/identity-providers/iden_732298a17f9c458890a1877880d140f3/";
        private const string LoginSelector = "#loginform input[type=text].txtLogin";
        private const string PasswordSelector = "#loginform input[type=password].txtPassword";
        private const string LoginButtonSelector = "#loginform button[type=submit]";
        private const string ConsentButtonSelector = "button#truste-consent-button";

        public AuthenticationService(ILogger<AuthenticationService> logger, IBrowser browser)
            : base(logger, browser)
        {
        }

        /// <summary>
        /// Authenticate against the F1 login page.
        /// </summary>
        /// <param name="login">Username/Email to authenticate with</param>
        /// <param name="password">Password to authenticate with</param>
        /// <param name="cancellationToken">Possible cancellation request token</param>
        /// <returns>Authentication response values</returns>
        /// <exception cref="AuthenticationException">Authentication failure exception</exception>
        public async Task<AuthenticationResponse> AuthenticateAsync(string login, string password, CancellationToken cancellationToken = default)
        {
#if DEBUG
            logger.LogTrace("Creating new page for authentication");
#endif
            IPage page = await NewPageAsync();
            logger.LogDebug("Created new page for authentication");

            AuthenticationResponse response;
            try
            {
                response = await AuthenticateAsync(page, login, password, cancellationToken)
                            .ConfigureAwait(false);
            }
            finally
            {
#if DEBUG
                logger.LogTrace("Closing single-use page after authentication");
#endif
                await page.CloseAsync()
                            .ConfigureAwait(false);

                logger.LogDebug("Closed single-use page after authentication");
            }

            return response;
        }
        /// <summary>
        /// Authenticate against the F1 login page.
        /// </summary>
        /// <param name="page">Page to login through.</param>
        /// <param name="login">Username/Email to authenticate with</param>
        /// <param name="password">Password to authenticate with</param>
        /// <param name="cancellationToken">Possible cancellation request token</param>
        /// <returns>Authentication response values</returns>
        /// <exception cref="AuthenticationException">Authentication failure exception</exception>
        public async Task<AuthenticationResponse> AuthenticateAsync(IPage page, string login, string password, CancellationToken cancellationToken = default)
        {

#if DEBUG
            logger.LogTrace("Navigating to {Endpoint} to authenticate {User}", Endpoints.LoginPage, login);
#endif
            await page.GoToAsync(Endpoints.LoginPage, LifecycleEvent.DOMContentLoaded)
                .ConfigureAwait(false);

            logger.LogTrace("Navigated to page {Endpoint} to authenticate {User}", Endpoints.LoginPage, login);

            await RandomDelay(cancellationToken)
                .ConfigureAwait(false);
            await ClickButtonAsync(page, ConsentButtonSelector)
                .ConfigureAwait(false);

            await RandomDelay(cancellationToken)
                .ConfigureAwait(false);
            await TypeAsync(page, LoginSelector, login, cancellationToken)
                .ConfigureAwait(false);

            await RandomDelay(cancellationToken)
                .ConfigureAwait(false);
            await TypeAsync(page, PasswordSelector, password, cancellationToken)
                .ConfigureAwait(false);

            await RandomDelay(cancellationToken)
                .ConfigureAwait(false);
            Task<IResponse> responseTask = page.WaitForResponseAsync(Endpoints.AuthenticationByPassword);
            Task clickTask = ClickButtonAsync(page, LoginButtonSelector);
#if DEBUG
            logger.LogTrace("Awaiting click & response");
#endif

            Task.WaitAll(responseTask, clickTask);
#if DEBUG
            logger.LogDebug("Response received for {Endpoint}", Endpoints.AuthenticationByPassword);
#endif

            switch (responseTask.Result.Status)
            {
                case HttpStatusCode.OK:
                    AuthenticationResponse response = await responseTask.Result.GetJsonAsync<AuthenticationResponse>()
                                .ConfigureAwait(false);
                    if (response != null && response.Session != null && !string.IsNullOrWhiteSpace(response.Session.Token))
                    {
                        logger.LogInformation("Authenticated {User} successfully.", login);
                        return response;
                    }

                    throw new HttpException("Authentication failed as an invalid/incomprehensive response was retrieved.", HttpStatusCode.Forbidden);
                case HttpStatusCode.Unauthorized:
                    throw new HttpException("Invalid credentials provided.", responseTask.Result.Status);
            }

            throw new F1InterfaceException("Unhandled response (this shouldn't happen!)");
        }
    }
}