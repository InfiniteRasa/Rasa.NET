using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Rasa.Api
{
    using Config;

    /// <summary>
    /// The REST API's certificate, loaded from the files ApiConfig.Rest.Tls names, and what a
    /// TLS handshake is made with.
    ///
    /// The certificate is a PKCS#12 file (.pfx, .p12) with its key inside, opened with
    /// CertificatePassword; or a PEM file, with its key in the same file or in KeyPath, and
    /// CertificatePassword for a key that is encrypted. Any more certificates in either are
    /// sent with it as its chain.
    /// </summary>
    public sealed class ApiTls
    {
        /// <summary>The server's certificate, with its private key.</summary>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>The protocols a handshake may settle on.</summary>
        public SslProtocols Protocols { get; private set; }

        /// <summary>What <see cref="SslStream.AuthenticateAsServerAsync(SslServerAuthenticationOptions, System.Threading.CancellationToken)"/> is given.</summary>
        public SslServerAuthenticationOptions Options { get; private set; }

        /// <summary>The settings and the files this was loaded from, as <see cref="SignatureOf"/> has them.</summary>
        public string Signature { get; private set; }

        /// <summary>
        /// The protocols from MinimumProtocol: "Tls12" (or "1.2", or nothing) is TLS 1.2 and
        /// 1.3; "Tls13" (or "1.3") is TLS 1.3 alone. False for anything else, which is read as
        /// "Tls12".
        /// </summary>
        public static bool TryProtocols(string minimum, out SslProtocols protocols)
        {
            protocols = SslProtocols.Tls12 | SslProtocols.Tls13;

            var wanted = (minimum ?? "").Trim().ToLowerInvariant().Replace("tls", "").Replace("v", "").Replace(" ", "").Replace("_", ".");

            switch (wanted)
            {
                case "":
                case "12":
                case "1.2":
                    return true;

                case "13":
                case "1.3":
                    protocols = SslProtocols.Tls13;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// The settings and the state of the files they name, as one string: a certificate is
        /// loaded again when this is no longer what it was loaded under, which a renewed file
        /// makes it.
        /// </summary>
        public static string SignatureOf(ApiTlsConfig config)
        {
            config ??= new ApiTlsConfig();

            return string.Join("|",
                config.CertificatePath, StampOf(config.CertificatePath),
                config.KeyPath, StampOf(config.KeyPath),
                config.CertificatePassword, config.MinimumProtocol);
        }

        private static string StampOf(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            try
            {
                var file = new FileInfo(path);

                return file.Exists ? $"{file.LastWriteTimeUtc.Ticks}:{file.Length}" : "missing";
            }
            catch (Exception)
            {
                return "unreadable";
            }
        }

        /// <summary>Loads the certificate. Null, with what was wrong, when it cannot be used.</summary>
        public static ApiTls Load(ApiTlsConfig config, out string problem)
        {
            config ??= new ApiTlsConfig();
            problem = null;

            var signature = SignatureOf(config);

            if (string.IsNullOrWhiteSpace(config.CertificatePath))
            {
                problem = "Tls.CertificatePath is not set";
                return null;
            }

            if (!File.Exists(config.CertificatePath))
            {
                problem = $"the certificate file \"{config.CertificatePath}\" does not exist";
                return null;
            }

            if (!string.IsNullOrWhiteSpace(config.KeyPath) && !File.Exists(config.KeyPath))
            {
                problem = $"the key file \"{config.KeyPath}\" does not exist";
                return null;
            }

            try
            {
                var password = string.IsNullOrEmpty(config.CertificatePassword) ? null : config.CertificatePassword;
                var chain = new X509Certificate2Collection();
                X509Certificate2 certificate;

                if (!string.IsNullOrWhiteSpace(config.KeyPath) || IsPem(config.CertificatePath))
                {
                    var keyPath = string.IsNullOrWhiteSpace(config.KeyPath) ? null : config.KeyPath;

                    using var pem = password == null
                        ? X509Certificate2.CreateFromPemFile(config.CertificatePath, keyPath)
                        : X509Certificate2.CreateFromEncryptedPemFile(config.CertificatePath, password, keyPath);

                    // A key read from PEM is held in memory only, which Windows will not make
                    // a TLS handshake with: through PKCS#12 it is one it will.
                    certificate = X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), null);

                    var all = new X509Certificate2Collection();

                    all.ImportFromPemFile(config.CertificatePath);

                    foreach (var other in all)
                        if (!string.Equals(other.Thumbprint, certificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                            chain.Add(other);
                }
                else
                {
                    var all = X509CertificateLoader.LoadPkcs12CollectionFromFile(config.CertificatePath, password);

                    certificate = all.FirstOrDefault(candidate => candidate.HasPrivateKey);

                    if (certificate == null)
                    {
                        problem = $"\"{config.CertificatePath}\" holds no certificate with a private key";
                        return null;
                    }

                    foreach (var other in all)
                        if (!ReferenceEquals(other, certificate))
                            chain.Add(other);
                }

                if (!certificate.HasPrivateKey)
                {
                    problem = $"the certificate in \"{config.CertificatePath}\" has no private key: put it in the same file or name it in Tls.KeyPath";
                    return null;
                }

                TryProtocols(config.MinimumProtocol, out var protocols);

                return new ApiTls
                {
                    Certificate = certificate,
                    Protocols = protocols,
                    Signature = signature,
                    Options = new SslServerAuthenticationOptions
                    {
                        // Its chain is what came with it: nothing is fetched to complete it.
                        ServerCertificateContext = SslStreamCertificateContext.Create(certificate, chain, offline: true),
                        EnabledSslProtocols = protocols,
                        ClientCertificateRequired = false
                    }
                };
            }
            catch (Exception e)
            {
                problem = $"the certificate in \"{config.CertificatePath}\" could not be loaded ({e.Message})";
                return null;
            }
        }

        private static bool IsPem(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension == ".pfx" || extension == ".p12")
                return false;

            using var stream = File.OpenRead(path);

            var start = new byte[4096];
            var read = stream.Read(start, 0, start.Length);

            return System.Text.Encoding.ASCII.GetString(start, 0, read).Contains("-----BEGIN");
        }
    }
}
