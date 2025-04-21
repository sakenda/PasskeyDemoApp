using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PasskeyHelper.Data;
using PasskeyHelper.Models;
using AuthenticatorTransport = PasskeyHelper.Data.AuthenticatorTransport;

namespace PasskeyHelper.Handlers;

public partial class Fido2RegisterHandler
{
    private readonly IFido2 _fido2 = default!;
    private readonly AttestationStateService _attestationStateService = default!;
    private readonly UserManager<ApplicationUser> _userManager = default!;

    public Fido2RegisterHandler(IFido2 fido2, AttestationStateService attestationStateService, UserManager<ApplicationUser> userManager)
    {
        _fido2 = fido2;
        _attestationStateService = attestationStateService;
        _userManager = userManager;
    }

    public Ok<CredentialCreateOptions> CreateAttestationOptions(CreateAttestationOptionsInputModel input)
    {
        var user = new Fido2User
        {
            Name = input.UserName,
            Id = Guid.NewGuid().ToByteArray(),
            DisplayName = input.UserName,
        };

        var authenticatorSelection = new AuthenticatorSelection
        {
            AuthenticatorAttachment = input.AuthenticatorAttachment,
            ResidentKey = input.ResidentKey,
            UserVerification = input.UserVerification,
        };

        var attestationPrefference = input.AttestationType.ToEnum<AttestationConveyancePreference>();

        var extensions = new AuthenticationExtensionsClientInputs
        {
            Extensions = true,
            UserVerificationMethod = true,
            CredProps = true,
        };

        var options = _fido2.RequestNewCredential(
            user,
            new List<PublicKeyCredentialDescriptor>(),
            authenticatorSelection,
            attestationPrefference,
            extensions);

        _attestationStateService.Set(Constants.Common.Fido2AttestationOptionsKey, options.ToJson());

        return TypedResults.Ok(options);
    }

    public async Task<Results<ProblemHttpResult, Ok<RegisteredPublicKeyCredential>>> CreateAttestation(AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken = default)
    {
        var json = _attestationStateService.Get(Constants.Common.Fido2AttestationOptionsKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return TypedResults.Problem("Json konnte nicht konvertiert werden.");
        }

        var options = CredentialCreateOptions.FromJson(json);

        var credentialResult = await _fido2.MakeNewCredentialAsync(
            attestationResponse,
            options,
            async (@params, cancellationToken) => await _userManager.Users
                .SelectMany(user => user.PublicKeyCredentials)
                .AllAsync(credential => credential.Id != @params.CredentialId, cancellationToken),
        cancellationToken);

        if (credentialResult.Result is null)
        {
            return TypedResults.Problem(credentialResult.ErrorMessage!);
        }

        var credential = GetCredential(credentialResult);
        var user = GetApplicationUser(credentialResult, credential);

        var identityResult = await _userManager.CreateAsync(user);
        if (!identityResult.Succeeded)
        {
            return TypedResults.Problem(identityResult.ToString());
        }

        return TypedResults.Ok(credentialResult.Result);
    }

    private static ApplicationUser GetApplicationUser(MakeNewCredentialResult credentialResult, PublicKeyCredential credential)
    {
        return new ApplicationUser
        {
            Id = new Guid(credentialResult.Result!.User.Id).ToString(),
            UserName = credentialResult.Result.User.Name,
            PublicKeyCredentials = { credential },
        };
    }

    private PublicKeyCredential GetCredential(MakeNewCredentialResult credentialResult)
    {
        var credential = new PublicKeyCredential
        {
            Id = credentialResult.Result!.Id,
            PublicKey = credentialResult.Result.PublicKey,
            SignatureCounter = credentialResult.Result.SignCount,
            IsBackupEligible = credentialResult.Result.IsBackupEligible,
            IsBackedUp = credentialResult.Result.IsBackedUp,
            AttestationObject = credentialResult.Result.AttestationObject,
            AttestationClientDataJson = credentialResult.Result.AttestationClientDataJson,
            AttestationFormat = credentialResult.Result.AttestationFormat,
            AaGuid = credentialResult.Result.AaGuid,
            UserId = new Guid(credentialResult.Result.User.Id).ToString(),
        };

        foreach (var authenticatorTransport in credentialResult.Result.Transports)
        {
            credential.AuthenticatorTransports.Add(new AuthenticatorTransport
            {
                PublicKeyCredentialId = credentialResult.Result.Id,
                Value = authenticatorTransport,
            });
        }

        if (credentialResult.Result.DevicePublicKey is not null)
        {
            credential.DevicePublicKeys.Add(new DevicePublicKey
            {
                PublicKeyCredentialId = credentialResult.Result.Id,
                Value = credentialResult.Result.DevicePublicKey,
            });
        }

        return credential;
    }

}
