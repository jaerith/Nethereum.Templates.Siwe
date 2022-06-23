# Nethereum SIWE Template

The Nethereum SIWE template provides an starting point of signing and authentication using Ethereum accounts and the standard SIWE message.
The templates provides the following use cases, and how SIWE can be implemented using the Nethereum libraries.
+ Rest Api 
+ Blazor Wasm + Rest Api
+ Blazor Server side (standalone)
+ Maui (Future template with Rest Api)
+ Avalonia (Future template with Rest Api)

## SIWE Message, signing and recovery (Overall process)

A SIWE Message is a standard message that a user signs with their private key, the message is presented in plain text to the user. The message contains different attributes including the Domain, Address, Uri, Expiry etc. The issuer of the message can authenticate the signer (user), by matching the recovered address from the signed message to their user records. To prevent replay attacks a unique nonce (random value) is created for each session.

More information can be found here https://eips.ethereum.org/EIPS/eip-4361

```csharp
public class SiweMessage
    {
        /// <summary>
        /// RFC 4501 dns authority that is requesting the signing.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Ethereum address performing the signing conformant to capitalization
        /// encoded checksum specified in EIP-55 where applicable.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Human-readable ASCII assertion that the user will sign, and it must not contain `\n`. 
        /// </summary>
        public string Statement { get; set; }

        /// <summary>
        /// RFC 3986 URI referring to the resource that is the subject of the signing
        /// (as in the __subject__ of a claim).
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Current version of the message. 
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Randomized token used to prevent replay attacks, at least 8 alphanumeric characters. 
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        ///  ISO 8601 datetime string of the current time. 
        /// </summary>
        public string IssuedAt { get; set; }

        /// <summary>
        /// ISO 8601 datetime string that, if present, indicates when the signed authentication message is no longer valid. 
        /// </summary>
        public string ExpirationTime { get; set; }

        /// <summary>
        /// ISO 8601 datetime string that, if present, indicates when the signed authentication message will become valid. 
        /// </summary>
        public string NotBefore { get; set; }

        /// <summary>
        /// System-specific identifier that may be used to uniquely refer to the sign-in request
        /// </summary>
      
        public string RequestId { get; set; }

        /// <summary>
        /// EIP-155 Chain ID to which the session is bound, and the network where, Contract Accounts must be resolved
        /// </summary>
        public string ChainId { get; set; }

        /// <summary>
        /// List of information or references to information the user wishes to have resolved as part of authentication by the relying party. They are expressed as RFC 3986 URIs separated by `\n- `
        /// </summary>
        public List<string> Resources { get; set; }

```

## Rest Api 
The Rest Api sample template demonstrates the following:

### Generate a new Siwe message with a random Nonce

To generate a new siwe message a DefaultSiweMessage class is in place, here you can put your website, statement, expiry, etc
The message is created using the Nethereum SiweMessageService that has been configured with the default [InMemorySessionNonceStorage](https://github.com/Nethereum/Nethereum/blob/master/src/Nethereum.Siwe/InMemorySessionNonceStorage.cs), which is used to store and validate SIWE messages mapped to their nonces as unique identifier. This can be replaced with your custom repository that implements ISessionStorage.
The Nonce is randomly generated by Nethereum using the SiweMessageService.

```csharp
 [AllowAnonymous]
[HttpPost("newsiwemessage")]
public IActionResult GenerateNewSiweMessage([FromBody] string address)
{
    var message = new DefaultSiweMessage();
    message.SetExpirationTime(DateTime.Now.AddMinutes(10));
    message.SetNotBefore(DateTime.Now);
    message.Address = address.ConvertToEthereumChecksumAddress();
    return Ok(_siweMessageService.BuildMessageToSign(message));
}
````

### Authenticating a User
To authentication a user, the signed message will be sent to the Rest API. 
In this example the whole message is validated as follows:

```csharp
[AllowAnonymous]
[HttpPost("authenticate")]
public async Task<IActionResult> Authenticate(AuthenticateRequest authenticateRequest)
{
    var siweMessage = SiweMessageParser.Parse(authenticateRequest.SiweEncodedMessage);
    var signature = authenticateRequest.Signature;
    var validUser = await _siweMessageService.IsUserAddressRegistered(siweMessage);
    if (validUser)
    {
        if (await _siweMessageService.IsMessageSignatureValid(siweMessage, signature))
        {
            if (_siweMessageService.IsMessageTheSameAsSessionStored(siweMessage))
            {
                if (_siweMessageService.HasMessageDateStartedAndNotExpired(siweMessage))
                {
                    var token = _siweJwtAuthorisationService.GenerateToken(siweMessage, signature);
                    return Ok(new AuthenticateResponse
                    {
                        Address = siweMessage.Address,
                        Jwt = token
                    });
                }
                ModelState.AddModelError("Unauthorized", "Expired token");
                return Unauthorized(ModelState);
            }
            ModelState.AddModelError("Unauthorized", "Matching Siwe message with nonce not found");
            return Unauthorized(ModelState);
        }
        ModelState.AddModelError("Unauthorized", "Invalid Signature");
        return Unauthorized(ModelState);
    }

    ModelState.AddModelError("Unauthorized", "Invalid User");
    return Unauthorized(ModelState);
}

```

### IUserService
The first check validates the user is registered (or valid) using Nethereum IUserService ``` var validUser = await _siweMessageService.IsUserAddressRegistered(siweMessage);```.
Your user service can validate the user is a registered user in a smart contract or internal database.
Nethereum provides a preset ERC721BalanceEthereumUserService, that validates that the user has an ERC721 token (NFT balance) https://github.com/Nethereum/Nethereum/blob/master/src/Nethereum.Siwe/UserServices/ERC721BalanceEthereumUserService.cs

### Creation of a JWT
To enable the reusability of other JWT middleware we create a JWT that stores the values of the SIWE message and uses the same expiration, issuance, etc of the SiweMessage.

DateTimes are stored for both SIWE and the JWT due to precission issues on milliseconds. JWT are defaulted to 0, so we won't be able to recreate the message to validate the signature.

```csharp
 public string GenerateToken(SiweMessage siweMessage, string signature)
        {

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                                                     new Claim(ClaimTypeAddress, siweMessage.Address) ,
                                                     new Claim(ClaimTypeNonce, siweMessage.Nonce),
                                                     new Claim(ClaimTypeSignature, signature),
                                                     new Claim(ClaimTypeSiweExpiry, siweMessage.ExpirationTime),
                                                     new Claim(ClaimTypeSiweIssuedAt, siweMessage.IssuedAt),
                                                     new Claim(ClaimTypeSiweNotBefore, siweMessage.NotBefore),
                                            }),


                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            if (!string.IsNullOrEmpty(siweMessage.ExpirationTime))
            {
                tokenDescriptor.Expires = GetIso8602AsDateTime(siweMessage.ExpirationTime);
            }
            if (!string.IsNullOrEmpty(siweMessage.IssuedAt))
            {
                tokenDescriptor.IssuedAt = GetIso8602AsDateTime(siweMessage.IssuedAt);

            }
            if (!string.IsNullOrEmpty(siweMessage.NotBefore))
            {
                tokenDescriptor.NotBefore = GetIso8602AsDateTime(siweMessage.NotBefore);
            }

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
```

### Validation of a JWT using the Middleware
Here the validation is done for both the JWT and the SIWE message that is reconstructed with the values from the JWT and validated against the signature, and generic validations

If wanted here other validations could be checked per request like ```IsUserAddressRegistered``` if checking the balance of an NFT or just simply user registration in a db.
```csharp
public async Task<SiweMessage> ValidateToken(string token)
        {
            if (token == null)
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var address = jwtToken.Claims.First(x => x.Type == ClaimTypeAddress).Value;
                var nonce = jwtToken.Claims.First(x => x.Type == ClaimTypeNonce).Value;
                var issuedAt = jwtToken.Claims.First(x => x.Type == ClaimTypeSiweIssuedAt).Value;
                var expiry = jwtToken.Claims.First(x => x.Type == ClaimTypeSiweExpiry).Value;
                var notBefore = jwtToken.Claims.First(x => x.Type == ClaimTypeSiweNotBefore).Value;



                var signature = jwtToken.Claims.First(x => x.Type == ClaimTypeSignature).Value;



                var siweMessage = new DefaultSiweMessage
                {
                    Address = address,
                    Nonce = nonce,
                    ExpirationTime = expiry,
                    IssuedAt = issuedAt,
                    NotBefore = notBefore
                };
                //We could use the values stored in the jwt token but if NotBefore or Expiration are not set this will be defaulted
                //and we may not want to expire and renew it
                //also milliseconds are not set in the jwtToken so this causes a validation failure, for this to match milliseconds have to be zero

                Debug.WriteLine(SiweMessageStringBuilder.BuildMessage(siweMessage));
                if (await _siweMessageService.IsMessageSignatureValid(siweMessage, signature))
                {
                    if (_siweMessageService.IsMessageTheSameAsSessionStored(siweMessage))
                    {
                        if (_siweMessageService.HasMessageDateStartedAndNotExpired(siweMessage))
                        {
                            return siweMessage;
                        }

                    }
                }

                return null;
            }
            catch
            {
                // return null if validation fails
                return null;
            }
        }
    }
```

## Blazor Wasm
The template uses a basis Metamask as the unique Ethereum Host Provider, and is the one responsible to Sign the SIWE Message.
When a user is connected to Metamask the user is presented with the option to Login, this first calls the rest Api to generate the SIWE message and assign a new Nonce, as seen in the Rest API. Then the user is prompted to sign the message as plain text returned from the server, once signed is submitted back to the rest api, which validates the message matches the one stored and signature, and then creates the JWT. The JWT is stored in the local storage and then reuse each time a call is make to the server. If the user disconnects from MM is automatically logout from the server and JWT removed from local storage.


###  SiweAuthenticationWasmStateProvider
The SiweAuthenticationWasmStateProvider is the extended version of EthereumAuthenticationStateProvider, the custom Ethereum Authentication State Provider used when an account is Connected creating the claims of "EthereumConnected" for that account.

The SiweAuthenticationWasmStateProvider is responsible for the interaction with the RestApi to create the SiweMessages, Authentication and storage of JWTs using the provided IAccessTokenService. In this scenario we use the LocalStorageAccessTokenService https://github.com/Nethereum/Nethereum.Siwe-Template/blob/main/ExampleProjectSiwe.Wasm/Services/LocalStorageAccessTokenService.cs

```csharp

public async Task AuthenticateAsync(string address)
{
    if (EthereumHostProvider == null || !EthereumHostProvider.Available)
    {
        throw new Exception("Cannot authenticate user, an Ethereum host is not available");
    }

    var siweMessage = await _siweUserLoginService.GenerateNewSiweMessage(address);
    var signedMessage = await EthereumHostProvider.SignMessageAsync(siweMessage);
    await AuthenticateAsync(SiweMessageParser.Parse(siweMessage), signedMessage);
}

public async Task AuthenticateAsync(SiweMessage siweMessage, string signature)
{
    var authenticateResponse = await _siweUserLoginService.Authenticate(siweMessage, signature);
    if (authenticateResponse.Jwt != null && authenticateResponse.Address.IsTheSameAddress(siweMessage.Address))
    {
        await _accessTokenService.SetAccessTokenAsync(authenticateResponse.Jwt);
        await MarkUserAsAuthenticated();
    }
    else
    {
        throw new Exception("Invalid authentication response");
    }

}

public async Task<SiweMessage> GenerateNewSiweMessage(string adddress)
{
    var message = await _siweUserLoginService.GenerateNewSiweMessage(adddress);
    return SiweMessageParser.Parse(message);
}

public async Task MarkUserAsAuthenticated()
{
    var user = await GetUserAsync();
    var claimsPrincipal = GenerateSiweClaimsPrincipal(user);

    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
}

```

####  ClaimsPrincipal

The claims created after authentication are the following, "EthereumConnected" the same as in the EthereumAuthenticationStateProvider and  "SiweAuthenticated",
the EthereumAddress is set as the NameIdentifier.

```csharp
private ClaimsPrincipal GenerateSiweClaimsPrincipal(User currentUser)
{
    //create a claims
    var claimName = new Claim(ClaimTypes.Name, currentUser.UserName);
    var claimEthereumAddress = new Claim(ClaimTypes.NameIdentifier, currentUser.EthereumAddress);
    var claimEthereumConnectedRole = new Claim(ClaimTypes.Role, "EthereumConnected");
    var claimSiweAuthenticatedRole = new Claim(ClaimTypes.Role, "SiweAuthenticated");

    //create claimsIdentity
    var claimsIdentity = new ClaimsIdentity(new[] { claimEthereumAddress, claimName, claimEthereumConnectedRole, claimSiweAuthenticatedRole }, "siweAuth");
    //create claimsPrincipal
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

    return claimsPrincipal;
}
```

#### Claims in Blazor
An example on how to use the Claims in Blazor is the following, in which restrict access to the Erc20Transfer
```xml
       <AuthorizeView Roles="SiweAuthenticated">
             <Authorized Context="siweAuth">
                    <Erc20Transfer></Erc20Transfer>
             </Authorized>
        </AuthorizeView>
```
Or remove a link from the navigation
```xml
<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Home
            </NavLink>
        </li>
        <AuthorizeView Roles="SiweAuthenticated">
            <li class="nav-item px-3">
                <NavLink class="nav-link" href="orders" Match="NavLinkMatch.All">
                    <span class="oi oi-grid-four-up" aria-hidden="true"></span>My Orders
                </NavLink>
            </li>
        </AuthorizeView>
    </ul>
</div>
```

## Blazor Server side
The Blazor server side, it is much simpler as we don't require a RestApi for authentication, everything is part of the same application.
The SiweAuthenticationServerStateProvider is responsible to orchestrate now with the NethereumSiweAuthenticatorService, which is part of the Nethereum.UI https://github.com/Nethereum/Nethereum/blob/master/src/Nethereum.UI/NethereumSiweAuthenticatorService.cs

```csharp
     public async Task AuthenticateAsync(string address = null)
        {
            
            if (EthereumHostProvider == null  || !EthereumHostProvider.Available)
            {
                throw new Exception("Cannot authenticate user, an Ethereum host is not available");
            }

            if (string.IsNullOrEmpty(address))
            {
                address = await EthereumHostProvider.GetProviderSelectedAccountAsync();
            }
            var siweMessage = new DefaultSiweMessage();
            siweMessage.Address = address.ConvertToEthereumChecksumAddress();
            siweMessage.SetExpirationTime(DateTime.Now.AddMinutes(10));
            siweMessage.SetNotBefore(DateTime.Now);
            var fullMessage = await nethereumSiweAuthenticatorService.AuthenticateAsync(siweMessage);
            await _accessTokenService.SetAccessTokenAsync(SiweMessageStringBuilder.BuildMessage(fullMessage));
            await MarkUserAsAuthenticated();
        }

```

In this scenario we store directly the SiweMessage using the ProtectedSessionStorageAccessTokenService https://github.com/Nethereum/Nethereum.Siwe-Template/blob/main/ExampleProjectSiwe.Server/Services/ProtectedSessionStorageAccessTokenService.cs to maintain the FrontEnd session.
And validation of the SiweMessage can be done when getting the AuthenticationState.

```csharp
public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var currentUser = await GetUserAsync();

            if (currentUser != null && currentUser.EthereumAddress != null)
            {
                var claimsPrincipal = GenerateSiweClaimsPrincipal(currentUser);
                return new AuthenticationState(claimsPrincipal);
            }
            await _accessTokenService.RemoveAccessTokenAsync();
            return await base.GetAuthenticationStateAsync();
        }
```

### MAUI and Avalonia examples 
Authentication for Maui and Avalonia will be the similar to Wasm using the Rest Api and SecuredStorage. If using an standalone application, the Blazor Hybrid or Avalonia Desktop as an starting point until having a specific example.

