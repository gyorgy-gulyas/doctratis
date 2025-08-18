using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IAM.Identities.Tests
{
    [TestClass]
    public partial class LoginIF_v1_Tests
    {
        private ILoginIF_v1 Sut => TestMain.ServiceProvider.GetRequiredService<ILoginIF_v1>();
        private IIdentityAdminIF_v1 Admin => TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

        [TestInitialize]
        public async Task Setup()
        {
            await TestMain.DeleteAllData();
        }

        [TestMethod]
        public Task IAM_Admin_Init()
        {
            Assert.IsNotNull(Sut);

            return Task.CompletedTask;
        }
    }
}

