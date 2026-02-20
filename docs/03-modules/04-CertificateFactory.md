# Certificate Factory
Certificate factory is a library that allows creating self-signed certificates, and certificate validators.

## Methods:
1. `static X509Certificate2 CreateSelfSignedCertificate(string commonName, string certName, string password)`
    - Creates a certificate into `certName.pfx` file with a password and common name.
    - Returns the created and saved certificate.
2. `static RemoteCertificateValidationCallback CreateThumbprintValidator(string expectedThumbprint, string expectedCommonName)`
    - Creates a certificate validator from thumbprint and common name.
3. `static RemoteCertificateValidationCallback CreateAcceptingValidator()`
    - Creates a certificate validator that accepts any remote certificate.