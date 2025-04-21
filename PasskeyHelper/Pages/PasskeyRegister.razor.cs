using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.JSInterop;
using PasskeyHelper.Handlers;
using System.Text.Json;
using static PasskeyHelper.PasskeyRegister;

namespace PasskeyHelper.Pages;

public partial class PasskeyRegister
{
    private string errorMessage = "";
    private CreateAttestationOptionsInputModel model = new() { UserName = "" };
    private DotNetObjectReference<PasskeyRegister>? dotNetRef;
    private IJSObjectReference? js;

    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public Fido2RegisterHandler Fido2Handler { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Parameter] public string ReturnUrl { get; set; } = "/";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            js = await JS.InvokeAsync<IJSObjectReference>("import", Constants.JSScriptNames.RegisterScriptPath);
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnInitialized()
    {
        dotNetRef = DotNetObjectReference.Create(this);
        base.OnInitialized();
    }

    private async Task Register()
    {
        var result = Fido2Handler.CreateAttestationOptions(model);
        var publicKey = result.Value;

        if (js is not null)
        {
            await js.InvokeVoidAsync(Constants.JSScriptNames.FunctionStartAttestation, publicKey, dotNetRef);
        }
    }

    [JSInvokable]
    public async Task CompleteAttestation(string json)
    {
        var dto = JsonSerializer.Deserialize<AttestationDto>(json);

        if (dto is null)
        {
            throw new InvalidOperationException("Invalid JSON response from JavaScript.");
        }

        var id = Base64Url.Decode(dto.Id);
        var rawId = Base64Url.Decode(dto.RawId);
        var type = dto.Type switch
        {
            "public-key" => PublicKeyCredentialType.PublicKey,
            _ => throw new ArgumentOutOfRangeException(nameof(dto.Type))
        };
        var rk = dto.Extensions.CredProps.Rk;
        var attestationObject = Base64Url.Decode(dto.Response.AttestationObject);
        var clientDataJson = Base64Url.Decode(dto.Response.ClientDataJSON);
        var transports = dto.Response.Transports?
            .Select(t => t switch
            {
                "usb" => Fido2NetLib.Objects.AuthenticatorTransport.Usb,
                "nfc" => Fido2NetLib.Objects.AuthenticatorTransport.Nfc,
                "ble" => Fido2NetLib.Objects.AuthenticatorTransport.Ble,
                "smart-card" => Fido2NetLib.Objects.AuthenticatorTransport.SmartCard,
                "hybrid" => Fido2NetLib.Objects.AuthenticatorTransport.Hybrid,
                "internal" => Fido2NetLib.Objects.AuthenticatorTransport.Internal,
                _ => throw new ArgumentOutOfRangeException(nameof(t), $"Unknown transport: {t}")
            })
            .ToArray();

        var attestation = new AuthenticatorAttestationRawResponse
        {
            Id = id,
            RawId = rawId,
            Type = type,
            ClientExtensionResults = new AuthenticationExtensionsClientOutputs
            {
                CredProps = new CredentialPropertiesOutput()
                {
                    Rk = rk
                },
            },
            Response = new AuthenticatorAttestationRawResponse.AttestationResponse
            {
                AttestationObject = attestationObject,
                ClientDataJson = clientDataJson,
                Transports = transports
            }
        };

        var resultCreateAttestation = await Fido2Handler.CreateAttestation(attestation!);
        if (resultCreateAttestation.Result is ProblemHttpResult problem)
        {
            errorMessage = problem.ProblemDetails.Detail!;
            StateHasChanged();
            return;
        }
        else if (resultCreateAttestation.Result is Ok<RegisteredPublicKeyCredential> result)
        {
            await Task.Yield();
            NavigationManager.NavigateTo($"{Constants.PageRoutes.PasskeySignInRedirect}?email={result.Value!.User.Name}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (js != null)
        {
            await js.DisposeAsync();
        }

        dotNetRef?.Dispose();
    }

}
