using Fido2NetLib.Objects;
using Fido2NetLib;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PasskeyHelper.Handlers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PasskeyHelper.Models.Passkey;

namespace PasskeyHelper.Pages.Passkey;

public partial class PasskeyLogin
{
    private string _errorMessage = "";
    private CreateAttestationOptionsInputModel _model = new() { UserName = "" };
    private DotNetObjectReference<PasskeyLogin>? dotNetRef;
    private IJSObjectReference? js;

    [Parameter] public string ReturnUrl { get; set; } = "/";
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public Fido2LoginHandler Fido2LoginHandler { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            js = await JS.InvokeAsync<IJSObjectReference>("import", Constants.JSScriptNames.LoginScriptPath);
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnInitialized()
    {
        dotNetRef = DotNetObjectReference.Create(this);
        base.OnInitialized();
    }

    private async Task Login()
    {
        var result = Fido2LoginHandler.CreateAttestationOptions(_model);
        var assertOptionsResult = await Fido2LoginHandler.CreateAssertionOptions(new CreateAssertionOptionsInputModel() { UserName = result.Value!.User.Name });
        if (assertOptionsResult.StatusCode != StatusCodes.Status200OK)
        {
            _errorMessage = "AssertOptions fehler.";
            StateHasChanged();
            return;
        }

        if (js is not null)
        {
            await js.InvokeVoidAsync(Constants.JSScriptNames.FunctionStartAssertion, assertOptionsResult.Value, dotNetRef);
        }
    }

    [JSInvokable]
    public async Task CompleteAssertion(string json)
    {
        var dto = JsonSerializer.Deserialize<AssertionDto>(json);
        if (dto is null)
            throw new InvalidOperationException("Invalid JSON response from JavaScript.");

        var assertion = new AuthenticatorAssertionRawResponse
        {
            Id = Base64Url.Decode(dto.Id),
            RawId = Base64Url.Decode(dto.RawId),
            Type = dto.Type == "public-key" ? PublicKeyCredentialType.PublicKey
                                            : throw new ArgumentOutOfRangeException(nameof(dto.Type)),
            ClientExtensionResults = new AuthenticationExtensionsClientOutputs
            {
                CredProps = new CredentialPropertiesOutput
                {
                    Rk = dto.Extensions.CredProps.Rk
                }
            },
            Response = new AuthenticatorAssertionRawResponse.AssertionResponse
            {
                AuthenticatorData = Base64Url.Decode(dto.Response.AuthenticatorData),
                ClientDataJson = Base64Url.Decode(dto.Response.ClientDataJSON),
                Signature = Base64Url.Decode(dto.Response.Signature),
                UserHandle = null
            }
        };

        var result = await Fido2LoginHandler.CreateAssertion(assertion);

        if (result.Result is ProblemHttpResult problem)
        {
            _errorMessage = problem.ProblemDetails.Detail!;
            StateHasChanged();
            return;
        }

        if (result.Result is Ok<VerifyAssertionResult> resultVerifiedAssertion)
        {
            await Task.Yield();

            var email = Fido2LoginHandler.GetAssertUserEmail(resultVerifiedAssertion.Value!);
            var parameterEmail = $"email={Uri.EscapeDataString(email)}";
            NavigationManager.NavigateTo($"{Constants.PageRoutes.PasskeySignInRedirect}?{parameterEmail}&returnUrl={ReturnUrl}");
        }
    }

}
