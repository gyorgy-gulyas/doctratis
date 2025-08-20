using IAM.Identities.Identity;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IdentityStoreContext _context;

        public AuthRepository(IdentityStoreContext context)
        {
            _context = context;
        }

        async Task<Response<Auth>> IAuthRepository.createAuth(CallingContext ctx, Auth auth)
        {
            try
            {
                await _context.Auths.Insert(auth);
            }
            catch (Exception ex)
            {
                return new(new Error() { Status = Statuses.BadRequest, MessageText = ex.Message, AdditionalInformation = ex.ToString() });
            }

            _context.Audit_Account(Core.Auditing.TrailOperations.Create, ctx, accountId: auth.accountId);

            return new(auth);
        }
		
		async Task<Response<Auth>> IAuthRepository.updateAuth(CallingContext ctx, Auth auth)
        {
            try
            {
                await _context.Auths.Update(auth);
            }
            catch (Exception ex)
            {
                return new(new Error() { Status = Statuses.BadRequest, MessageText = ex.Message, AdditionalInformation = ex.ToString() });
            }

            _context.Audit_Account(Core.Auditing.TrailOperations.Update, ctx, accountId: auth.accountId);

            return new(auth);
        }
		
		async Task<Response<Auth>> IAuthRepository.getAuth(CallingContext ctx, string accountId, string authId)
        {
            var auth = await _context.Auths.Find(accountId, authId);
            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account Auth for account '{accountId}' does not exist, with auth: '{authId}'" });

            return new(auth);
        }

        Task<Response<List<Auth>>> IAuthRepository.listAuthsForAccount(CallingContext ctx, string accountId)
        {
            var auths = _context
                .Auths
                .AsQueryable()
                .Where(ah => ah.PartitionKey == accountId)
                .ToList();

            return Response<List<Auth>>.Success(auths).AsTask();
        }

		Task<Response<List<Auth>>> IAuthRepository.listAuthsForAccountByMethod(CallingContext ctx, string accountId, Auth.Methods method)
        {
            var auths = _context
                .Auths
                .AsQueryable()
                .Where(ah =>
                    ah.PartitionKey == accountId &&
                    ah.method == method )
                .ToList();

            return Response<List<Auth>>.Success(auths).AsTask();
        }

		async Task<Response<EmailAuth>> IAuthRepository.getEmailAuth(CallingContext ctx, string accountId, string authId)
        {
            var auth = await _context.Auths.Find(accountId, authId);
            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account Auth for account '{accountId}' does not exist, with auth: '{authId}'" });

            if (auth.method != Auth.Methods.Email || auth is not EmailAuth emailAuth)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account Auth for account '{accountId}' is not an email auth, with auth: '{authId}'" });

            return new(emailAuth);
        }

		Task<Response<EmailAuth>> IAuthRepository.findEmailAuthByEmail(CallingContext ctx, string email)
        {
            email = email.Normalize().Trim().ToLower();

            var auth = _context
                .Auths
                .AsQueryable<EmailAuth,Auth>()
                .Where(ah => ah.email.ToLower() == email )
                .FirstOrDefault();

            return Response<EmailAuth>.Success(auth).AsTask();
        }

		async Task<Response<ADAuth>> IAuthRepository.getADAuth(CallingContext ctx, string accountId, string authId)
        {
            var auth = await _context.Auths.Find(accountId, authId);
            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account Auth for account '{accountId}' does not exist, with auth: '{authId}'" });

            if (auth.method != Auth.Methods.ActiveDirectory || auth is not ADAuth adAuth)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account Auth for account '{accountId}' is not an ActiveDirectory auth, with auth: '{authId}'" });

            return new(adAuth);
        }

		Task<Response<ADAuth>> IAuthRepository.findADAuthByDomainAndUser(CallingContext ctx, string ldapDomainId, string userName)
        {
            userName = userName.Normalize().Trim().ToLower();

            var auth = _context
                .Auths
                .AsQueryable<ADAuth,Auth>()
                .Where(ah =>
                    ah.LdapDomainId == ldapDomainId && 
                    ah.userName.ToLower() == userName)
                .FirstOrDefault();

            return Response<ADAuth>.Success(auth).AsTask();
        }

		Task<Response<List<ADAuth>>> IAuthRepository.listADAuthsByDomain(CallingContext ctx, string ldapDomainId)
        {
            var auths = _context
                .Auths
                .AsQueryable<ADAuth,Auth>()
                .Where(ah => ah.LdapDomainId == ldapDomainId )
                .ToList();

            return Response<List<ADAuth>>.Success(auths).AsTask();
        }

		async Task<Response<KAUAuth>> IAuthRepository.getKAUAuth(CallingContext ctx, string accountId, string authId)
        {
            var auth = await _context.Auths.Find(accountId, authId);
            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account Auth for account '{accountId}' does not exist, with auth: '{authId}'" });

            if (auth.method != Auth.Methods.KAU || auth is not KAUAuth kauAuth)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account Auth for account '{accountId}' is not an KAU auth, with auth: '{authId}'" });

            return new(kauAuth);
        }

		Task<Response<KAUAuth>> IAuthRepository.findKAUAuthByUserId(CallingContext ctx, string kauUserId)
        {
            var auth = _context
                .Auths
                .AsQueryable<KAUAuth, Auth>()
                .Where(ah => ah.KAUUserId == kauUserId)
                .FirstOrDefault();

            return Response<KAUAuth>.Success(auth).AsTask();
        }

		async Task<Response<CertificateAuth>> IAuthRepository.getCertificateAuth(CallingContext ctx, string accountId, string authId)
        {
            var auth = await _context.Auths.Find(accountId, authId);
            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account Auth for account '{accountId}' does not exist, with auth: '{authId}'" });

            if (auth.method != Auth.Methods.Certificate || auth is not CertificateAuth certAuth)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account Auth for account '{accountId}' is not an Certificate auth, with auth: '{authId}'" });

            return new(certAuth);
        }

		Task<Response<CertificateAuth>> IAuthRepository.findCertificateAuthByThumbprint(CallingContext ctx, string thumbprint)
        {
             var auth = _context
                .Auths
                .AsQueryable<CertificateAuth, Auth>()
                .Where(ah => ah.certificateThumbprint == thumbprint)
                .FirstOrDefault();

            return Response<CertificateAuth>.Success(auth).AsTask();
        }

		Task<Response<CertificateAuth>> IAuthRepository.findCertificateAuthBySerial(CallingContext ctx, string serialNumber)
        {
            serialNumber = serialNumber.Normalize().Trim();

            var auth = _context
                .Auths
                .AsQueryable<CertificateAuth, Auth>()
                .Where(ah => ah.serialNumber == serialNumber)
                .FirstOrDefault();

            return Response<CertificateAuth>.Success(auth).AsTask();
        }
    }
}