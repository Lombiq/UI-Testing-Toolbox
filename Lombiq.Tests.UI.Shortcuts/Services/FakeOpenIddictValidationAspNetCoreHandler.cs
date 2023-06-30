using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using static OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreConstants;

namespace Lombiq.Tests.UI.Shortcuts.Services;

/// <summary>
/// Provides the logic necessary to extract, validate and handle OpenID Connect requests.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class FakeOpenIddictValidationAspNetCoreHandler : AuthenticationHandler<OpenIddictValidationAspNetCoreOptions>,
    IAuthenticationRequestHandler
{
    private readonly IOpenIddictValidationDispatcher _dispatcher;
    private readonly IOpenIddictValidationFactory _factory;

    public FakeOpenIddictValidationAspNetCoreHandler(
        IOpenIddictValidationDispatcher dispatcher,
        IOpenIddictValidationFactory factory,
        IOptionsMonitor<OpenIddictValidationAspNetCoreOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc/>
    public async Task<bool> HandleRequestAsync()
    {
        // Note: the transaction may be already attached when replaying an ASP.NET Core request
        // (e.g when using the built-in status code pages middleware with the re-execute mode).
        var transaction = Context.Features.Get<OpenIddictValidationAspNetCoreFeature>()?.Transaction;
        if (transaction is null)
        {
            // Create a new transaction and attach the HTTP request to make it available to the ASP.NET Core handlers.
            transaction = await _factory.CreateTransactionAsync();
            transaction.Properties[typeof(HttpRequest).FullName!] = new WeakReference<HttpRequest>(Request);

            // Attach the OpenIddict validation transaction to the ASP.NET Core features
            // so that it can retrieved while performing challenge/forbid operations.
            Context.Features.Set(new OpenIddictValidationAspNetCoreFeature { Transaction = transaction });
        }

        var context = new OpenIddictValidationEvents.ProcessRequestContext(transaction)
        {
            CancellationToken = Context.RequestAborted,
        };

        await _dispatcher.DispatchAsync(context);

        if (context.IsRequestHandled)
        {
            return true;
        }

        if (context.IsRequestSkipped)
        {
            return false;
        }

        if (context.IsRejected)
        {
            var notification = new OpenIddictValidationEvents.ProcessErrorContext(transaction)
            {
                CancellationToken = Context.RequestAborted,
                Error = context.Error ?? OpenIddictConstants.Errors.InvalidRequest,
                ErrorDescription = context.ErrorDescription,
                ErrorUri = context.ErrorUri,
                Response = new OpenIddictResponse(),
            };

            await _dispatcher.DispatchAsync(notification);

            if (notification.IsRequestHandled)
            {
                return true;
            }

            if (notification.IsRequestSkipped)
            {
                return false;
            }

            throw new InvalidOperationException("ID0111");
        }

        return false;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var transaction = Context.Features.Get<OpenIddictValidationAspNetCoreFeature>()?.Transaction ??
            throw new InvalidOperationException("ID0166");

        // Note: in many cases, the authentication token was already validated by the time this action is called
        // (generally later in the pipeline, when using the pass-through mode). To avoid having to re-validate it,
        // the authentication context is resolved from the transaction. If it's not available, a new one is created.
        var context = transaction.GetProperty<OpenIddictValidationEvents.ProcessAuthenticationContext>(
            typeof(OpenIddictValidationEvents.ProcessAuthenticationContext).FullName!);
        if (context is null)
        {
            context = new OpenIddictValidationEvents.ProcessAuthenticationContext(transaction)
            {
                CancellationToken = Context.RequestAborted,
            };

            await _dispatcher.DispatchAsync(context);

            // Store the context object in the transaction so it can be later retrieved by handlers
            // that want to access the authentication result without triggering a new authentication flow.
            transaction.SetProperty(typeof(OpenIddictValidationEvents.ProcessAuthenticationContext).FullName!, context);
        }

        if (context.IsRequestHandled || context.IsRequestSkipped)
        {
            return AuthenticateResult.NoResult();
        }

        if (context.IsRejected)
        {
            // Note: the missing_token error is special-cased to indicate to ASP.NET Core
            // that no authentication result could be produced due to the lack of token.
            // This also helps reducing the logging noise when no token is specified.
            if (string.Equals(context.Error, OpenIddictConstants.Errors.MissingToken, StringComparison.Ordinal))
            {
                return AuthenticateResult.NoResult();
            }

            var properties = new AuthenticationProperties(new Dictionary<string, string>
            {
                [Properties.Error] = context.Error,
                [Properties.ErrorDescription] = context.ErrorDescription,
                [Properties.ErrorUri] = context.ErrorUri,
            });

            var data = new
            {
                context.IsRejected,
                IdentityName = context.AccessTokenPrincipal?.Identity?.Name,
                context.AccessToken,
                context.Request,
                context.Error,
                context.ErrorDescription,
                context.ErrorUri,
                context.ExtractAccessToken,
                context.RejectAccessToken,
                context.ValidateAccessToken,
                context.RequireAccessToken,
                context.Configuration,
                Options = new
                {
                    context.Options?.Audiences,
                    context.Options?.ClientId,
                    context.Options?.ClientSecret,
                    context.Options?.Issuer,
                    context.Options?.ConfigurationEndpoint,
                    context.Options?.EncryptionCredentials,
                    context.Options?.ValidationType,
                    context.Options?.EnableAuthorizationEntryValidation,
                    context.Options?.EnableTokenEntryValidation,
                },
                Transaction = new
                {
                    context.Transaction?.Response,
                },
                context.EndpointType,
            };

            context.Logger.LogError("ID0113: {Data}", JsonConvert.SerializeObject(data));
            return AuthenticateResult.Fail("ID0113: " + JsonConvert.SerializeObject(data), properties);
        }
        else
        {
            // A single main claims-based principal instance can be attached to an authentication ticket.
            // To return the most appropriate one, the principal is selected based on the endpoint type.
            // Independently of the selected main principal, all principals resolved from validated tokens
            // are attached to the authentication properties bag so they can be accessed from user code.
            var principal = context.EndpointType is OpenIddictValidationEndpointType.Unknown
                ? context.AccessTokenPrincipal
                : null;

            if (principal is null)
            {
                return AuthenticateResult.NoResult();
            }

            var properties = new AuthenticationProperties
            {
                ExpiresUtc = principal.GetExpirationDate(),
                IssuedUtc = principal.GetCreationDate(),
            };

            List<AuthenticationToken> tokens = null;

            // Attach the tokens to allow any ASP.NET Core component (e.g a controller)
            // to retrieve them (e.g to make an API request to another application).

            if (!string.IsNullOrEmpty(context.AccessToken))
            {
                tokens = new(capacity: 1)
                {
                    new AuthenticationToken
                    {
                        Name = Tokens.AccessToken,
                        Value = context.AccessToken,
                    },
                };
            }

            if (context.AccessTokenPrincipal is not null)
            {
                properties.SetParameter(Properties.AccessTokenPrincipal, context.AccessTokenPrincipal);
            }

            if (tokens is { Count: > 0 })
            {
                properties.StoreTokens(tokens);
            }

            return AuthenticateResult.Success(new AuthenticationTicket(
                principal,
                properties,
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme));
        }
    }

    /// <inheritdoc/>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var transaction = Context.Features.Get<OpenIddictValidationAspNetCoreFeature>()?.Transaction ??
                          throw new InvalidOperationException("ID0166");

        transaction.Properties[typeof(AuthenticationProperties).FullName!] = properties ?? new AuthenticationProperties();

        var context = new OpenIddictValidationEvents.ProcessChallengeContext(transaction)
        {
            CancellationToken = Context.RequestAborted,
            Response = new OpenIddictResponse(),
        };

        await _dispatcher.DispatchAsync(context);

        if (context.IsRequestHandled || context.IsRequestSkipped)
        {
            return;
        }

        if (context.IsRejected)
        {
            var notification = new OpenIddictValidationEvents.ProcessErrorContext(transaction)
            {
                CancellationToken = Context.RequestAborted,
                Error = context.Error ?? OpenIddictConstants.Errors.InvalidRequest,
                ErrorDescription = context.ErrorDescription,
                ErrorUri = context.ErrorUri,
                Response = new OpenIddictResponse(),
            };

            await _dispatcher.DispatchAsync(notification);

            if (notification.IsRequestHandled || context.IsRequestSkipped)
            {
                return;
            }

            throw new InvalidOperationException("ID0111");
        }
    }

    /// <inheritdoc/>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => HandleChallengeAsync(properties);
}
