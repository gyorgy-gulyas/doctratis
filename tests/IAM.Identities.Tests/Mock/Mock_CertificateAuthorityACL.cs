using ServiceKit.Net;
using System.Text;

namespace IAM.Identities.Tests.Mock
{
    internal class Mock_CertificateAuthorityACL : ICertificateAuthorityACL
    {
        private readonly HashSet<string> _revokedSerials = new();

        Task<Response<byte[]>> ICertificateAuthorityACL.signCsr(CallingContext ctx, string csrPem, string profile)
        {
            // Logoljuk a hívást (akár teszthez)
            Console.WriteLine($"[MOCK] signCsr called with profile={profile}");

            // Dummy byte[] tartalom (nem valós cert)
            var dummyCert = Encoding.UTF8.GetBytes("-----BEGIN CERTIFICATE-----\nMOCK_CERT_DATA\n-----END CERTIFICATE-----");

            return Response.Success(dummyCert).AsTask();
        }

        Task<Response<bool>> ICertificateAuthorityACL.revoke(CallingContext ctx, string serialNumber, string reason)
        {
            Console.WriteLine($"[MOCK] revoke called with serial={serialNumber}, reason={reason}");

            _revokedSerials.Add(serialNumber);
            return Response.Success(true).AsTask();
        }
    }
}
