using Fido2NetLib.Objects;
using Fido2NetLib;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using PasskeyHelper.Data;
using Microsoft.EntityFrameworkCore;
using AuthenticatorTransport = PasskeyHelper.Data.AuthenticatorTransport;
using PasskeyHelper.Models.Passkey;

namespace PasskeyHelper.Handlers;

public class Fido2LoginHandler
{
    private readonly IFido2 _fido2 = default!;
    private readonly AttestationStateService _attestationStateService = default!;
    private readonly UserManager<ApplicationUser> _userManager = default!;
    private readonly ApplicationDbContext _applicationDbContext;

    public Fido2LoginHandler(IFido2 fido2, AttestationStateService attestationStateService, UserManager<ApplicationUser> userManager, ApplicationDbContext applicationDbContext)
    {
        _fido2 = fido2;
        _attestationStateService = attestationStateService;
        _userManager = userManager;
        _applicationDbContext = applicationDbContext;
    }

    internal async Task<Results<ProblemHttpResult, Ok<VerifyAssertionResult>>> CreateAssertion(AuthenticatorAssertionRawResponse assertionResponse, CancellationToken cancellationToken = default)
    {
        var json = _attestationStateService.Get(Constants.Common.Fido2AttestationOptionsKey);

        if (string.IsNullOrEmpty(json))
        {
            return TypedResults.Problem($"{nameof(Constants.Common.Fido2AttestationOptionsKey)} konnte nicht gelesen werden."); ;
        }

        var options = AssertionOptions.FromJson(json);

        var credential = await _userManager.Users
            .SelectMany(user => user.PublicKeyCredentials)
            .Include(credential => credential.DevicePublicKeys)
            .SingleOrDefaultAsync(credential => credential.Id == assertionResponse.Id, cancellationToken);

        if (credential is null)
        {
            return TypedResults.Problem("Es wurde kein Crential zu dem User gefunden.");
        }

        var user = await _userManager.FindByIdAsync(credential.UserId);

        if (user is null)
        {
            return TypedResults.Problem("User nicht gefunden.");
        }

        var assertionResult = await _fido2.MakeAssertionAsync(
            assertionResponse,
            options,
            credential.PublicKey,
            credential.DevicePublicKeys.Select(key => key.Value).ToList(),
            credential.SignatureCounter,
            async (@params, cancellationToken) =>
                await _userManager.Users
                    .Where(user => user.Id == new Guid(@params.UserHandle).ToString())
                    .SelectMany(user => user.PublicKeyCredentials)
                    .AnyAsync(credential => credential.Id == @params.CredentialId, cancellationToken),
            cancellationToken);

        credential.SignatureCounter = assertionResult.SignCount;

        if (assertionResult.DevicePublicKey is not null)
        {
            credential.DevicePublicKeys.Add(new DevicePublicKey
            {
                PublicKeyCredentialId = assertionResult.CredentialId,
                Value = assertionResult.DevicePublicKey
            });
        }

        _applicationDbContext.PublicKeyCredentials.Update(credential);
        await _applicationDbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(assertionResult);
    }

    internal async Task<Ok<AssertionOptions>> CreateAssertionOptions(CreateAssertionOptionsInputModel input, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = _userManager.NormalizeName(input.UserName);

        var allowedCredentials = await _userManager.Users
            .Where(user => user.NormalizedUserName == normalizedUserName)
            .SelectMany(user => user.PublicKeyCredentials)
            .Select(credential => new PublicKeyCredentialDescriptor(credential.Id))
            .ToListAsync(cancellationToken);

        var extensions = new AuthenticationExtensionsClientInputs
        {
            Extensions = true,
            UserVerificationMethod = true,
            DevicePubKey = new AuthenticationExtensionsDevicePublicKeyInputs()
        };

        var options = _fido2.GetAssertionOptions(
            allowedCredentials,
            input.UserVerification,
            extensions);

        _attestationStateService.Set(Constants.Common.Fido2AttestationOptionsKey, options.ToJson());

        return TypedResults.Ok(options);
    }

    internal async Task<Results<BadRequest, Ok<RegisteredPublicKeyCredential>>> CreateAttestation(
        AuthenticatorAttestationRawResponse attestationResponse,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken = default)
    {
        var json = _attestationStateService.Get(Constants.Common.Fido2AttestationOptionsKey);

        if (string.IsNullOrEmpty(json))
        {
            return TypedResults.BadRequest();
        }

        var options = CredentialCreateOptions.FromJson(json);

        var credentialResult = await _fido2.MakeNewCredentialAsync(
            attestationResponse,
            options,
            async (@params, cancellationToken) =>
                await userManager.Users
                    .SelectMany(user => user.PublicKeyCredentials)
                    .AllAsync(credential => credential.Id != @params.CredentialId, cancellationToken),
            cancellationToken);

        if (credentialResult.Result is null)
        {
            throw new Exception(credentialResult.ErrorMessage);
        }

        var credential = new PublicKeyCredential
        {
            Id = credentialResult.Result.Id,
            PublicKey = credentialResult.Result.PublicKey,
            SignatureCounter = credentialResult.Result.SignCount,
            IsBackupEligible = credentialResult.Result.IsBackupEligible,
            IsBackedUp = credentialResult.Result.IsBackedUp,
            AttestationObject = credentialResult.Result.AttestationObject,
            AttestationClientDataJson = credentialResult.Result.AttestationClientDataJson,
            AttestationFormat = credentialResult.Result.AttestationFormat,
            AaGuid = credentialResult.Result.AaGuid,
            UserId = new Guid(credentialResult.Result.User.Id).ToString()
        };

        foreach (var authenticatorTransport in credentialResult.Result.Transports)
        {
            credential.AuthenticatorTransports.Add(new AuthenticatorTransport
            {
                PublicKeyCredentialId = credentialResult.Result.Id,
                Value = authenticatorTransport
            });
        }

        if (credentialResult.Result.DevicePublicKey is not null)
        {
            credential.DevicePublicKeys.Add(new DevicePublicKey
            {
                PublicKeyCredentialId = credentialResult.Result.Id,
                Value = credentialResult.Result.DevicePublicKey
            });
        }

        var user = new ApplicationUser
        {
            Id = new Guid(credentialResult.Result.User.Id).ToString(),
            UserName = credentialResult.Result.User.Name,
            PublicKeyCredentials = { credential }
        };

        var identityResult = await userManager.CreateAsync(user);

        if (!identityResult.Succeeded)
        {
            throw new Exception(identityResult.ToString());
        }

        return TypedResults.Ok(credentialResult.Result);
    }

    internal Ok<CredentialCreateOptions> CreateAttestationOptions(CreateAttestationOptionsInputModel input)
    {
        var user = new Fido2User
        {
            Name = input.UserName,
            Id = Guid.NewGuid().ToByteArray(),
            DisplayName = input.UserName
        };

        var authenticatorSelection = new AuthenticatorSelection
        {
            AuthenticatorAttachment = input.AuthenticatorAttachment,
            ResidentKey = input.ResidentKey,
            UserVerification = input.UserVerification
        };

        var attestationPreference = input.AttestationType.ToEnum<AttestationConveyancePreference>();

        var extensions = new AuthenticationExtensionsClientInputs
        {
            Extensions = true,
            UserVerificationMethod = true,
            DevicePubKey = new AuthenticationExtensionsDevicePublicKeyInputs { Attestation = input.AttestationType },
            CredProps = true
        };

        var options = _fido2.RequestNewCredential(
            user,
            new List<PublicKeyCredentialDescriptor>(),
            authenticatorSelection,
            attestationPreference,
            extensions);

        _attestationStateService.Set(Constants.Common.Fido2AttestationOptionsKey, options.ToJson());

        return TypedResults.Ok(options);
    }

    internal string GetAssertUserEmail(VerifyAssertionResult verifyAssertionResult)
    {
        var pubKeycred = _applicationDbContext.PublicKeyCredentials.Where(x => x.Id == verifyAssertionResult.CredentialId).SingleOrDefault();
        var user = _applicationDbContext.Users
            .Where(x => pubKeycred != null && pubKeycred.UserId == x.Id)
            .SingleOrDefault();
        
        if (user is null)
            return "";

        return user.UserName ?? "";
    }

}
