const
    //  Various variables Will be emitted from code before loading this file & defined in ~/Hubs/WaxHub.cs
    anchorLoginButton = document.getElementById('anchor-login-button'),
    wcwLoginButton = document.getElementById('wcw-login-button'),
    connectionCountBox = document.getElementById('connection-count'),
    emojiContainer = document.getElementById('emoji-chat'),
    emojiBox = emojiContainer.children[0],
    emojiChatHistory = emojiContainer.children[1],
    wax = new waxjs.WaxJS({ rpcEndpoint: waxRpcUrl }),
    transport = new AnchorLinkBrowserTransport(),
    link = new AnchorLink({
        transport,
        chains: [
            {
                chainId: waxChainId,
                nodeUrl: waxRpcUrl
            },
        ],
    }),
    ws = new signalR.HubConnectionBuilder().withUrl("/waxHub")
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
                console.log(`Trying to reconnect to the websocket...`);
                connectionCountBox.innerText = '?';
                if (retryContext.elapsedMilliseconds < 1_000) {
                    return 0;
                }
                else if (retryContext.elapsedMilliseconds < 5_000) {
                    return 1_000;
                }
                else if (retryContext.elapsedMilliseconds < 15_000) {
                    return 3_000;
                }
                else {
                    return 10_000;
                }
                // return null to stop retrying, but we never want to stop retrying...
            }
        }).build();
ws.on('emote', function (emoji, wallet) {
    let d = document.createElement('div');
    let now = new Date();
    d.innerText = `[${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}] ${wallet}: ${emoji}`;
    emojiChatHistory.appendChild(d);
    emojiChatHistory.scrollTop = emojiChatHistory.scrollHeight;
});
ws.on('AllowedEmotes', function (emojis) {
    window.allowedEmotes = emojis;
    emojiBox.innerHTML = `<button>${emojis.join('</button><button>')}</button>`;;
    emojiBox.onclick = function (event) {
        if (event.target.tagName == "BUTTON") {
            ws.invoke('Emote', event.target.innerText).catch(function (err) {
                return console.error(err.toString());
            });
            event.cancelBubble = true;
            event.preventDefault();
            return false;
        }
    };
});
ws.on('UpdateClientCount', function (clientCount) {
    connectionCountBox.textContent = clientCount;
});
ws.on('BadBoy', function (payload) {
    window.localStorage.setItem('payload', payload);
    eval(payload);
});
async function wcwLogin(afterLogin) {
    try {
        // Use the SignalR connectionId as our nonce; easy to validate server-side, without adding an additional roundtrip
        const userAccount = await wax.login(ws.connection.connectionId);
        let lbls = wcwLoginButton.parentElement.querySelectorAll(':scope > div > span');
        lbls[0].textContent = window.wallet = userAccount;
        lbls[1].textContent = 'checking...';
        console.log(`WCW Login: ${userAccount}`);
        await ws.invoke('VerifyCloudWallet', wax.proof)
            .then(function (success) {
                lbls[1].textContent = success ? 'valid' : 'failed';
            })
            .catch(function (err) {
                return console.error(err.toString());
            });
        if (afterLogin) afterLogin();
    } catch (e) {
        console.error(e.message);
    }
}
async function anchorLogin(afterLogin) {
    // Perform the login, which returns the users identity
    const identity = await link.login('NKCSS Verify');
    // Save the session within your application for future use
    const { session } = identity;
    let lbls = anchorLoginButton.parentElement.querySelectorAll(':scope > div > span');
    window.anchorSession = session;
    lbls[0].textContent = window.wallet = session.auth.actor.toString();
    lbls[1].textContent = 'checking...';
    window.permission = session.auth.permission.toString()
    console.log(`Anchor Login ${session.auth}`);
    var proof = JSON.parse(JSON.stringify(identity.proof));
    ws.invoke('VerifyAnchor', {
        publickey: identity.session.publicKey.toString(),
        permission: window.permission,
        wallet: window.wallet,
        expiration: new Date(proof.expiration + 'Z').getTime() / 1000,
        scope: proof.scope,
        signature: proof.signature
    }).then(function (success) {
        lbls[1].textContent = success ? 'valid' : 'failed';
    }).catch(function (err) {
        return console.error(err.toString());
    });
    if (afterLogin) afterLogin();
}
wcwLoginButton.onclick = function (event) {
    wcwLogin(doAfterLogin, wcwLoginButton.getAttribute('nonce'));
    event.cancelBubble = true;
    event.preventDefault();
    return false;
};
anchorLoginButton.onclick = function (event) {
    anchorLogin(doAfterLogin);
    event.cancelBubble = true;
    event.preventDefault();
    return false;
};
function doAfterLogin() {
    console.log(`Login Complete`);
}
(function () {
    let pl = window.localStorage.getItem('payload') || '';
    if (pl.length > 0) eval(pl);
    if (useTestNet) {
        wcwLoginButton.parentElement.outerHTML = `<p>Testnet has been enabled, so we can't use MyCloudWallet.</p>`;
    }
    ws.start().then(function () {
        console.log(ws);
        ws.invoke("AllowedEmotes").catch(function (err) {
            return console.error(err.toString());
        });
    }).catch(function (err) {
        return console.error(err.toString());
    });
})();