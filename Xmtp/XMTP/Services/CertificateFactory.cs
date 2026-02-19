using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Xmtp
{
    public static class CertificateFactory
    {
        public static X509Certificate2 CreateSelfSignedCertificate(string commonName, string certName, string password)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={commonName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            X509Certificate2 cert = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddYears(1));

            File.WriteAllBytes($"{certName}.pfx", cert.Export(X509ContentType.Pfx, password));

            return cert;
        }

        public static RemoteCertificateValidationCallback CreateThumbprintValidator(
        string expectedThumbprint,
        string expectedCommonName)
        {
            return (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                if (certificate == null)
                    return false;

                X509Certificate2 cert = new X509Certificate2(certificate);

                bool thumbprintValid = cert.Thumbprint.Equals(
                    expectedThumbprint,
                    StringComparison.OrdinalIgnoreCase);

                bool commonNameValid = cert.Subject.Contains(
                    $"CN={expectedCommonName}");

                return thumbprintValid && commonNameValid;
            };
        }

        public static RemoteCertificateValidationCallback CreateAcceptingValidator()
        {
            return (sender, certificate, chain, sslPolicyErrors) => true;
        }
    }
}

