using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibrary
{
    public class GenerateCert2
    {
        static string FileName = "SensorMForGrps";

        public static async Task NewCert()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "/SensorMForGrps.pfx"))
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                if (store.Certificates.Any(x => x.Subject.Contains(FileName)))
                {
                    store.Close();
                    store.Dispose();
                    return;
                }
            }
            await buildSelfSignedServerCertificateAsync();
        }


        public static async Task buildSelfSignedServerCertificateAsync()
        {
            var ips = await System.Net.Dns.GetHostAddressesAsync(System.Net.Dns.GetHostName());
            string CertificateName = FileName;
            string myIP = ips[2].ToString();
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(ips[2]);
            IPAddress address = IPAddress.Parse("127.0.0.1");
            sanBuilder.AddIpAddress(address);
            sanBuilder.AddDnsName(myIP);
            sanBuilder.AddDnsName(Environment.MachineName);

            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={CertificateName}");

            var passwordForCertificateProtection = new SecureString();
            foreach (var item in "SensorM")
            {
                passwordForCertificateProtection.AppendChar(item);
            }

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));


                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddYears(10)));
                //certificate.FriendlyName = CertificateName;

                File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/{FileName}.pfx", certificate.Export(X509ContentType.Pfx, "SensorM"));

                // Create Base 64 encoded CER (public key only)
                File.WriteAllText($"{Directory.GetCurrentDirectory()}/{FileName}.cer",
                    "-----BEGIN CERTIFICATE-----\r\n"
                    + Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                    + "\r\n-----END CERTIFICATE-----");

                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                if (store.Certificates.Any(x => x.Subject == certificate.Subject))
                {
                    store.Remove(store.Certificates.First(x => x.Subject == certificate.Subject));
                }

                store.Add(certificate); //where cert is an X509Certificate object
                store.Close();
                store.Dispose();
            }
        }
    }

    internal class GenerateCert
    {
        public static async Task SetCertAsync()
        {

            //if (File.Exists(Directory.GetCurrentDirectory() + "/SensorM.pfx"))
            //   return;

            var ips = await System.Net.Dns.GetHostAddressesAsync(System.Net.Dns.GetHostName());

            var rsaKey = RSA.Create(2048);
            string subject = "CN=127.0.0.1";//insert config ip
            var certificateRequest = new CertificateRequest(
                subject,
                rsaKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
                );

            certificateRequest.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(
                        certificateAuthority: false,
                        hasPathLengthConstraint: false,
                        pathLengthConstraint: 0,
                        critical: true
                    )
                );


            certificateRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        keyUsages:
                        X509KeyUsageFlags.NonRepudiation |
                            X509KeyUsageFlags.DigitalSignature
                            | X509KeyUsageFlags.KeyEncipherment,
                        critical: false
                    )
                );


            certificateRequest.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(
                        key: certificateRequest.PublicKey,
                        critical: false
                    )
                );
            certificateRequest.CertificateExtensions.Add(
                new X509Extension(
                    new AsnEncodedData(
                        "Subject Alternative Name",
                        Encoding.ASCII.GetBytes("127.0.0.1")
                    ),
                    false
                )
            );

            var expireAt = DateTimeOffset.Now.AddYears(100);

            var certificate = certificateRequest.CreateSelfSigned(DateTimeOffset.Now, expireAt);

            var exportableCertificate = new X509Certificate2(
                    certificate.Export(X509ContentType.Cert),
                    (string)null,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                ).CopyWithPrivateKey(rsaKey);

            var passwordForCertificateProtection = new SecureString();
            foreach (var item in "Sensor-M")
            {
                passwordForCertificateProtection.AppendChar(item);
            }
            //нужно указать путь
            File.WriteAllBytes(Directory.GetCurrentDirectory() + "/SensorM.pfx",
                    exportableCertificate.Export(
                        X509ContentType.Pfx,
                        passwordForCertificateProtection
                    )
                );


            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(exportableCertificate); //where cert is an X509Certificate object
            store.Close();
        }

        public static async Task MakeCert()
        {

            if (File.Exists(Directory.GetCurrentDirectory() + "/SensorMGrpc.pfx"))
                return;

            var ips = await System.Net.Dns.GetHostAddressesAsync(System.Net.Dns.GetHostName());
            var options = new GenerateCertificateOptions
            (
                pathToSave: Directory.GetCurrentDirectory() + "/",
                commonName: "SensorMGrpc",
                fileName: "SensorMGrpc",
                password: "Sensor-M",
                5
            );

            var rsa = RSA.Create(4096);
            var req = new CertificateRequest($"cn={options.CommonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(options.Years));
            var path = Path.Combine(options.PathToSave, options.CertificateFileName);

            // Create PFX (PKCS #12) with private key
            File.WriteAllBytes($"{path}.pfx", cert.Export(X509ContentType.Pfx, options.Password));

            // Create Base 64 encoded CER (public key only)
            File.WriteAllText($"{path}.cer",
                "-----BEGIN CERTIFICATE-----\r\n"
                + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                + "\r\n-----END CERTIFICATE-----");

            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert); //where cert is an X509Certificate object
            store.Close();
        }

        public static async Task NewCert()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "/SensorMNew.pfx"))
                return;

            await buildSelfSignedServerCertificateAsync();
        }


        public static async Task buildSelfSignedServerCertificateAsync()
        {
            string FileName = "SensorMNew";
            var ips = await System.Net.Dns.GetHostAddressesAsync(System.Net.Dns.GetHostName());
            string CertificateName = "127.0.0.1";
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(new System.Net.IPAddress(0x0100007F));
            sanBuilder.AddDnsName("127.0.0.1");
            sanBuilder.AddDnsName(Environment.MachineName);

            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={CertificateName}");

            var passwordForCertificateProtection = new SecureString();
            foreach (var item in "SensorM")
            {
                passwordForCertificateProtection.AppendChar(item);
            }

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));


                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                //certificate.FriendlyName = CertificateName;

                File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/{FileName}.pfx", certificate.Export(X509ContentType.Pfx, "SensorM"));

                // Create Base 64 encoded CER (public key only)
                File.WriteAllText($"{Directory.GetCurrentDirectory()}/{FileName}.cer",
                    "-----BEGIN CERTIFICATE-----\r\n"
                    + Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                    + "\r\n-----END CERTIFICATE-----");

                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate); //where cert is an X509Certificate object
                store.Close();
            }
        }
    }

    public class GenerateCertificateOptions
    {
        public GenerateCertificateOptions(string pathToSave, string commonName, string fileName, string password, int years = 1)
        {
            CommonName = commonName;
            if (string.IsNullOrEmpty(CommonName))
            {
                throw new ArgumentNullException(nameof(CommonName));
            }

            PathToSave = pathToSave;
            if (string.IsNullOrEmpty(PathToSave))
            {
                throw new ArgumentNullException(nameof(PathToSave));
            }

            Password = password;
            if (string.IsNullOrEmpty(Password))
            {
                throw new ArgumentNullException(nameof(Password));
            }

            CertificateFileName = fileName;
            if (string.IsNullOrEmpty(CertificateFileName))
            {
                throw new ArgumentNullException(nameof(CertificateFileName));
            }

            Years = years;
            if (Years <= 0)
            {
                Years = 1;
            }
        }

        public string CommonName { get; }
        public string PathToSave { get; }
        public string Password { get; }
        public string CertificateFileName { get; }
        public int Years { get; }
    }
}
