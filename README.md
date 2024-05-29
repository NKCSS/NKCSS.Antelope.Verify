![Lumpi-Nick](https://rp.naw.io/img/lumpinick.png)
# NKCSS.Antelope.Verify
The [Cloud Wallet](https://mycloudwallet.com) allows you to generate a signed nonce when loggin in, making the whole process invisible to the user. This proof can then be verified using ECDSA. Anchor has a similar feature, where it will sign a specific transaction to generate an aditional proof that can be verified using ECDSA.

You want to use this proof on your backend (e.g. game server) to validate the client is who they say they are (never trust user input).

Unity games are usually written in C#, yet there are no C# solutions out there to validate these proofs. Solutions I've seen are to run a separate nodejs service to do the validation, which isn't very user friendly.

This was the reason why this library was created. A C# library that can run natively on backend stacks for game servers to validate these proofs.

The library cam accept Signatures in a common to Antelope format (e.g. "SIG_K1_...") or the raw decoded bytes, public keys in both old & new formats ("EOS..." and "PUB_K1_...") or the raw decoded bytes and the data can be specified either as a UTF8-string (e.g. MyCloudWallet proof) or byte array (Anchor serialized transaction, for which a custom serializer was included to handle the Anchor generated proof).

The code uses [Cryptography.ECDSA.Secp256k1](https://www.nuget.org/packages/Cryptography.ECDSA.Secp256k1) and the [Portable.BouncyCastle](https://www.nuget.org/packages/Portable.BouncyCastle) crypto libraries to do some of the ECDSA heavy lifting.

To help with implementing this, a 2-stage example has been added to the repository. One, where you have both a CloudWallet and Anchor login that is validated server-side, and 2nd, where you then use these server-validated identities to have a multiplayer emoji chat.

Funded by a [Wax Labs Grant](https://labs.wax.io/proposals/196)

## How to use

The application has been pre-configured to work on the [Wax blockchain](https://wax.io), with support for both MainNet and Testnet, but it should be fine to work on other chains Anchor supports.

Basic structure of the example is as followed: `./wwwwroot/index.html` is a very basic HTML file that holds login buttons and the Emoji chat front-end. Style file can be found in `./wwwroot/css/site.css`, client-side logic in `./wwwroot/js/site.js` and the backend logic in `./Hubs/WaxHub.cs`. All the logic resides in a SignalR Hub (.NET websocket solution).

Application flow is as followed:

`index.html` page is loaded in the browser, this builds the websocket connection on load. It will retrieve the supported emoji's for the chat and build the rest of the font-end based on that.

If configured for Wax MainNet (default), you'll have an option to login with your MyCloudWallet account. On success, this will pass the `.proof` component to the backend for server-side validation.

To have `waxjs` generate a proof you can validate on the backend, you need to pass in a server-controlled value that will be part of the message that gets signed. In this case, we use the `connectionId` that the SignalR hub assigned to us. This changes every time the client connects to the Hub. We do this by passing `ws.connection.connectionId` to `wax.login()` like: `const userAccount = await wax.login(ws.connection.connectionId);`.

A secondary option, would be to use Anchor. Anchor does not allow you to send a server-controlled nonce, but it will sign a fake-transaction with an expiration 1 minute in the future coded into it, preventing old proofs from being reused (provided you validate this on your backend as well, which this library does for you).

To pass the data to the backend from the Anchor proof, we pack up various datapoints into an object:

```js
{
        publickey: identity.session.publicKey.toString(),
        permission: session.auth.permission.toString(),
        wallet: session.auth.actor.toString(),
        // 'Z' needs to be added to make sure the passed expiration is treated as UTC by javascript. We pass the seconds to the backend, but the value returns ms, so we divide by 1000.
        expiration: new Date(proof.expiration + 'Z').getTime() / 1000,
        scope: proof.scope,
        signature: proof.signature
    }
```

We also prepare the `proof` object like this, to not have to call `.toString()` on every property, but that's also a valid way to go about it.

```js
var proof = JSON.parse(JSON.stringify(identity.proof));
```

## Notes

### Delay

To show the difference between client & server-side validation, I added a 1-second delay to the server-side part, which is only for illustration purposes; feel free to remove it when implementing this yourself.

```csharp
// We add a delay here so you can see the secondary verification on the front-end.
Thread.Sleep(1000);
```

### TestNet

Switching between TestNet and MainNet is done by changing `UseTestNet` in `./Hubs/WaxHub.cs`.

### proof.wax public key

The public key for `proof.wax` is currently hardcoded as there really shouldn't be a reason to rotate this. Should the need arise, you can find it in the constant called `ProofDotWaxActivePublicKey` which resides in this class: `NKCSS.Antelope.Verify.Verifier`.
You could also modify the code to periodically check this, but as I said, there currently is no need for this.

### nonce

In the example I built, I used the SignalR ConnectionId as the nonce; this changes every time you reconnect so it's pretty good to fend against replay attacks, as the server will compare against their connectionId, something the client can't set themselves. 
If you were to use a different mechanism, try to add a time component in your nonce so you can make sure it isn't an old proof that gets re-transmitted, or a different mechanism to make sure the nonce is server-generated and not something the client can control/re-use.

### emoji's

The emoji's the client can use in the demo are defined in the Hub in variable `allowedEmotes`. You can add/remove things here as you please.

## Contact me

If you have any additional questions, feel free to hit me up.

[![Twitter URL](https://img.shields.io/twitter/url/https/twitter.com/NKCSS.svg?style=social&label=Follow%20%40NKCSS)](https://twitter.com/NKCSS) 
[![Telegram](https://img.shields.io/badge/Telegram-2CA5E0?style=for-the-badge&logo=telegram&logoColor=white)
](https://t.me/NicksTechdom)[![YouTube](https://img.shields.io/badge/YouTube-%23FF0000.svg?style=for-the-badge&logo=YouTube&logoColor=white)
](https://nick.yt)