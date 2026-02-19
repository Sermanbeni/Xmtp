# Security

To secure the protocol X509Certificate2 certificates can be used to create TLS and mTLS.
Can also be used with self-signed certificates with custom validator.

- Server configuration:
    - certificate: if provided, uses certificate and makes TLS
    - certificateValidationCallback: if provided and client authentication is true (uses mTLS) the custom validator will be used instead of the default. (for self-signed certificates)
    - useClientAuthentication: use mTLS or not

- Client configuration:
    - useTls: attempt to use TLS when connecting
    - certificateValidationCallback: if provided, it will be used to validate the server certificate instead of the default validator. (for self-signed certificates)
    - certificate: the certificate the client shows up (if the server requires mTLS)

Successful connection only happens when:
1. Server does not provide TLS and client connects without TLS or server provides TLS and client connects with TLS.
    - If only one is true the connection fails.
2. If TLS is used: the client must validate the server certificate with its validation callback
    - If not validated the connection fails.
3. If mTLS is used: both sides must validate the remote certificate
    - If either certificate is not validated the connection fails.

# Tools
CertificateFactory class is provided to:
1. Issue custom self-signed certificates with
2. Load generated certificates
3. Create validation callbacks with thumbprint and common name
4. Create accepting validation callback (accepts any certificate for TLS-only without authentication)
