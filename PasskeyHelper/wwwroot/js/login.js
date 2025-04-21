
export async function startAssertion(publicKey, dotNetRef) {

    function coerceToArrayBuffer(thing, name) {
        if (typeof thing === "string") {
            thing = thing.replace(/-/g, "+").replace(/_/g, "/");
            const str = window.atob(thing);
            const bytes = new Uint8Array(str.length);
            for (let i = 0; i < str.length; i++) {
                bytes[i] = str.charCodeAt(i);
            }
            thing = bytes;
        }

        if (Array.isArray(thing)) {
            thing = new Uint8Array(thing);
        }

        if (thing instanceof Uint8Array) {
            thing = thing.buffer;
        }

        if (!(thing instanceof ArrayBuffer)) {
            throw new TypeError("could not coerce '" + name + "' to ArrayBuffer");
        }

        return thing;
    }

    function coerceToBase64Url(thing) {
        if (Array.isArray(thing)) {
            thing = Uint8Array.from(thing);
        }

        if (thing instanceof ArrayBuffer) {
            thing = new Uint8Array(thing);
        }

        if (thing instanceof Uint8Array) {
            let str = "";
            for (let i = 0; i < thing.byteLength; i++) {
                str += String.fromCharCode(thing[i]);
            }
            thing = window.btoa(str);
        }

        if (typeof thing !== "string") {
            throw new Error("could not coerce to string");
        }

        return thing.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    }

    publicKey.challenge = coerceToArrayBuffer(publicKey.challenge, "challenge");

    for (const allowCredential of publicKey.allowCredentials) {
        allowCredential.id = coerceToArrayBuffer(allowCredential.id, "allowCredential.id");
    }

    const credential = await navigator.credentials.get({ publicKey });

    const assertion = {
        id: credential.id,
        rawId: coerceToBase64Url(credential.rawId),
        type: credential.type,
        extensions: credential.getClientExtensionResults(),
        response: {
            authenticatorData: coerceToBase64Url(credential.response.authenticatorData),
            clientDataJSON: coerceToBase64Url(credential.response.clientDataJSON),
            signature: coerceToBase64Url(credential.response.signature)
        }
    };

    var json = JSON.stringify(assertion);
    await dotNetRef.invokeMethodAsync("CompleteAssertion", json);
}
