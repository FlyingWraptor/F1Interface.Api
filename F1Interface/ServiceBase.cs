using System;
using System.Threading;
using System.Threading.Tasks;
using F1Interface.Domain;
using Microsoft.Extensions.Logging;
using PlaywrightSharp;

namespace F1Interface
{
    public abstract class ServiceBase<T> where T : ServiceBase<T>
    {
#if DEBUG
        private static bool EmulateRealDelays => !XUnitUtils.IsUnitTesting;
#else
        private static readonly bool EmulateRealDelays = true;
#endif

        protected static readonly Random random = new Random();

        protected readonly ILogger<T> logger;
        protected readonly IBrowser browser;

        internal ServiceBase(ILogger<T> logger, IBrowser browser)
        {
            if (browser == null)
            {
                throw new ArgumentNullException("Provided IApiBrowser cannot be null!");
            }

            this.browser = browser;
            this.logger = logger;
        }

        /// <summary>
        /// Create a random task delay
        /// </summary>
        /// <param name="cancellationToken">Taks cancellation request token</param>
        internal Task RandomDelay(CancellationToken cancellationToken = default)
            => RandomDelay(200, 1500, cancellationToken);
        /// <summary>
        /// Create a random task delay
        /// </summary>
        /// <param name="minimum">Minimum amount of milliseconds to wait</param>
        /// <param name="maximum">Maximum amount of milliseconds to wait</param>
        /// <param name="cancellationToken">Taks cancellation request token</param>
        internal Task RandomDelay(int minimum, int maximum, CancellationToken cancellationToken = default)
            => Task.Delay((EmulateRealDelays ? random.Next(minimum, maximum) : 0),
                cancellationToken);
        /// <summary>
        /// Create a new page using a random user-agent and the preconfigured viewport
        /// </summary>
        /// <param name="browser">\_(^_^)_/</param>
        /// <param name="random">Random generator to use, defaults to a newly created one</param>
        /// <returns>The created page</returns>
        internal Task<IPage> NewPageAsync()
            => browser.NewPageAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize { Height = 979, Width = 1920 },
                UserAgent = Constants.UserAgents[random.Next(Constants.UserAgents.Length)],
                AcceptDownloads = false
            });
        /// <summary>
        /// Click a button on the provided page
        /// </summary>
        /// <param name="page">Page to click in</param>
        /// <param name="selector">Document element selector to click the correct button/field</param>
        internal Task ClickButtonAsync(IPage page, string selector)
            => page.ClickAsync(selector, (EmulateRealDelays ? random.Next(50, 250) : 0));

        internal async Task TypeAsync(IPage page, string selector, string text, CancellationToken cancellationToken = default)
        {
            IElementHandle handle = await page.QuerySelectorAsync(selector)
                .ConfigureAwait(false);

            if (handle != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    await RandomDelay(15, 100, cancellationToken)
                        .ConfigureAwait(false);
                    await handle.PressAsync(text[i].ToString())
                        .ConfigureAwait(false);
                }
            }
        }
    }
}