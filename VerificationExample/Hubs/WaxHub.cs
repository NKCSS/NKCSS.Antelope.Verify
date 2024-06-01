using Microsoft.AspNetCore.SignalR;
using System.Text;

namespace VerificationExample.Hubs
{
    public class WaxHub : Hub
    {
        #region Constants
        internal const bool UseTestNet = false;
        /// <summary>
        /// Can be found in ./Properties/launchSettings.json under profiles.http.applicationUrl. But, if you deploy this to a server, you might want to set this to the expected URL as there might be a reverse proxy like NGINX in between.
        /// </summary>
        /// <remarks>
        /// If you don't want to do this check, set it to empty/null and it will be bypassed.
        /// </remarks>
        const string ExpectedReferer = "http://localhost:5260/";
        static readonly bool CheckCWReferrer = !string.IsNullOrWhiteSpace(ExpectedReferer);
        // Some defaults for Wax; untested on other chains, but should be fine if you change the values below.
        const string WaxTestNetChainId = "f16b1833c747c43682f4386fca9cbb327929334a762755ebec17f6f23c9b8a12";
        const string WaxMainNetChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4";
        const string WaxTestNetRpcUrl = "https://testnet.waxsweden.org";
        const string WaxMainNetRpcUrl = "https://wax.eosusa.io";
        internal const string WaxChainId = UseTestNet ? WaxTestNetChainId : WaxMainNetChainId;
        internal const string WaxRpcUrl = UseTestNet ? WaxTestNetRpcUrl : WaxMainNetRpcUrl;
        // We inject these settings to make sure we only have to define things once.
        internal const string ServerSideScriptName = "server-vars.js";
        internal static readonly string ServerSideScript = $@"
// Server-controlled variables; prevents defining the same multiple times.
const useTestNet = {WaxHub.UseTestNet.ToString().ToLowerInvariant()},
    waxChainId = '{WaxHub.WaxChainId}',
    waxRpcUrl = '{WaxHub.WaxRpcUrl}';
";
        #endregion
        /// <summary>
        /// This determines which emoji's are allowed to be sent by the clients. Feel free to expand this.
        /// </summary>
        HashSet<string> allowedEmotes = new HashSet<string> {
            "❤️",
            "👋",
            "💪",
            "👍",
            "😭",
            "🥰",
            "🔥",
            "🚀",
            "😎",
            "🎉",
            "👑",
            "Yo, Nick Kusters is AMAZING!"
        };
        /// <summary>
        /// Lock object to prevent multi-threading issues when dealing with connects & disconnects
        /// </summary>
        static object ConnectionIdLock = new object();
        /// <summary>
        ///  For tracking which clients verified with which wallets.
        /// </summary>
        /// <remarks>
        /// Does not persist.
        /// </remarks>
        static Dictionary<string, (string wallet, DateTime? firstAuth, DateTime? secondAuthRequested, DateTime? secondAuth)> connectionIds { get; set; } = new();
        public override async Task OnConnectedAsync()
        {
            int connectionCount;
            lock (ConnectionIdLock)
            {
                connectionIds.Add(Context.ConnectionId, (null, null, null, null));
                connectionCount = connectionIds.Count;
            }
            await base.OnConnectedAsync();
            await Clients.All.SendAsync(ClientEvents.UpdateClientCount, connectionCount);
        }
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            int connectionCount;
            lock (ConnectionIdLock)
            {
                if (connectionIds.TryGetValue(Context.ConnectionId, out var wallet) && !string.IsNullOrWhiteSpace(wallet.wallet))
                    Console.WriteLine($"{wallet} disconnected...");
                connectionIds.Remove(Context.ConnectionId);
                connectionCount = connectionIds.Count;
            }
            await base.OnDisconnectedAsync(ex);
            await Clients.All.SendAsync(ClientEvents.UpdateClientCount, connectionCount);
        }
        public async Task AllowedEmotes()
        {
            await Clients.Caller.SendAsync(ClientEvents.AllowedEmotes, allowedEmotes);
        }
        public async Task Emote(string emoji)
        {
            string wallet = connectionIds.TryGetValue(Context.ConnectionId, out var w) && w.firstAuth.HasValue ? w.wallet ?? "?" : "?";
            if (allowedEmotes.Contains(emoji))
            {
                // Make sure we only broadcast authenticated & validated client messages
                if (wallet != "?")
                {
                    // Emoji's have length 2; we just want to make sure it's not the big string one, which would require a lvl 2 auth for demo purposes here...
                    if (w.secondAuth.HasValue || emoji.Length <= 2)
                    {
                        await Clients.All.SendAsync(ClientEvents.Emote, emoji, wallet);
                    }
                    else if (DateTime.UtcNow.Subtract(w.firstAuth.Value).TotalSeconds > MinSecondsBetweenAuths)
                    {
                        w.secondAuthRequested = DateTime.UtcNow;
                        connectionIds[Context.ConnectionId] = w;
                        await Clients.Caller.SendAsync(ClientEvents.Emote, $"[only visible to you] This message requires a 2nd level of verification, please complete a secondary auth before you are able to do this.", wallet);                        
                        await Clients.Caller.SendAsync(ClientEvents.ReAuth);
                    }
                    else
                    {
                        await Clients.Caller.SendAsync(ClientEvents.Emote, $"[only visible to you] This message requires a 2nd level of verification, but not enough time has passed since your last authentication. Please try again in about a minute or so.", wallet);
                    }
                }
            }
            else
            {
                Console.WriteLine($"{wallet} has been a Bad Boy...");
                await Clients.Caller.SendAsync(ClientEvents.BadBoy, "document.body.innerHTML = ''; document.body.style.backgroundImage = 'url(https://vote.naw.io/img/nggyu.gif)';");
            }
        }
        const int MinSecondsBetweenAuths = 60;
        public async Task<bool> VerifyAnchor(NKCSS.Antelope.Verify.Anchor.VerifyMessage msg)
        {
            var exp = DateTime.UnixEpoch.AddSeconds(msg.expiration);
            if (exp < DateTime.UtcNow)
            {
                Console.WriteLine($"We received an expired ({exp.Subtract(DateTime.UtcNow)}) proof for {msg.wallet}");
                return false;
            }
            // We add a delay here so you can see the secondary verification on the front-end.
            Thread.Sleep(1000);
            bool isValid = NKCSS.Antelope.Verify.Verifier.Verify(msg, WaxChainId);
            Console.WriteLine($"[Anchor] {msg.wallet}@{msg.permission}: {isValid}");
            if (isValid)
            {
                if(!connectionIds.TryGetValue(Context.ConnectionId, out var c))
                {
                    c = (msg.wallet, DateTime.UtcNow, null, null);
                    connectionIds.Add(Context.ConnectionId, c);
                }
                else if(!c.firstAuth.HasValue)
                {
                    c.wallet = msg.wallet;
                    c.firstAuth = DateTime.UtcNow;
                    connectionIds[Context.ConnectionId] = c;
                }
                else if (!c.secondAuth.HasValue)
                {
                    if (!c.secondAuthRequested.HasValue)
                    {
                        Console.WriteLine($"{msg.wallet} sent in another autentication without us asking for it; this could be a replay attack (or they just triggered the login again on the site).");
                        await Clients.Caller.SendAsync(ClientEvents.Emote, $"[only visible to you] You are already logged in, there is no need to login again.", msg.wallet);
                        return false;
                    }
                    else if (DateTime.UtcNow.Subtract(c.secondAuthRequested.Value).TotalSeconds > MinSecondsBetweenAuths) {
                        Console.WriteLine($"{msg.wallet}'s second-stage auth came in too long ({DateTime.UtcNow.Subtract(c.secondAuthRequested.Value)} passed since the request was made)");
                        await Clients.Caller.SendAsync(ClientEvents.Emote, $"[only visible to you] You took too long to authenticate.", msg.wallet);
                        return false;
                    }
                    else if (DateTime.UtcNow.Subtract(c.firstAuth.Value).TotalSeconds > MinSecondsBetweenAuths)
                    {
                        c.secondAuth = DateTime.UtcNow;
                        connectionIds[Context.ConnectionId] = c;
                        Console.WriteLine($"{msg.wallet} verified lvl 2");
                        await Clients.Caller.SendAsync(ClientEvents.Emote, $"[only visible to you] thank you for completing stage-2 verification!", msg.wallet);
                    }
                    else
                    {
                        Console.WriteLine($"User tried to do a 2nd stage verification too early!");
                        await Clients.Caller.SendAsync(ClientEvents.Emote, $"[only visible to you] You can't do a 2nd stage verification yet because not enough time has passed since your last authentication", msg.wallet);
                        return false;
                    }
                }
                else
                {
                    // Already authenticated, no action needed really, they just hit the button again.
                }
                return true;
            }
            return false;
        }
        public async Task<bool> VerifyCloudWallet(NKCSS.Antelope.Verify.MyCloudWallet.TransparantVerifyMessage msg)
        {
            // We add a delay here so you can see the secondary verification on the front-end.
            Thread.Sleep(1000);
            bool isValid = NKCSS.Antelope.Verify.Verifier.Verify(msg, Context.ConnectionId);
            // Cloud Wallet is always 'active' permission
            Console.WriteLine($"[CloudWallet] {msg.userAccount}@active: {isValid}; msg: '{msg.ExpectedMessage}'");
            if (isValid && !msg.Altered)
            {
                if (!CheckCWReferrer || msg.referrer == ExpectedReferer)
                {
                    // For cloud wallet, we only need 1 level of auth, but you could incorproate this system if you wanted to have additional security.
                    var now = DateTime.UtcNow;
                    connectionIds[Context.ConnectionId] = (msg.userAccount, now, now, now);
                    return true;
                }
                else
                {
                    Console.WriteLine($"Everything checks out, except the referer. We expected '{ExpectedReferer}'  but got '{msg.referrer}'. This might be a replay attack, or you forgot to set it correctly.");
                    return false;
                }
            }
            return false;
        }
        internal static class ClientEvents
        {
            internal const string Emote = nameof(Emote);
            internal const string AllowedEmotes = nameof(AllowedEmotes);
            internal const string UpdateClientCount = nameof(UpdateClientCount);
            internal const string BadBoy = nameof(BadBoy);
            internal const string ReAuth = nameof(ReAuth);
        }
    }
}
