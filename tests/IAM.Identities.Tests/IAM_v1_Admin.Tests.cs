using Microsoft.Extensions.DependencyInjection;

namespace IAM.Identities.Tests
{
    [TestClass]
    public partial class IdentityAdminIF_v1_Tests
    {
        private IIdentityAdminIF_v1 GetSystemUnderTest() =>
            TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

        [TestInitialize]
        public async Task Setup()
        {
            await TestMain.DeleteAllData();
        }

        [TestMethod]
        public Task IAM_Admin_Init()
        {
            var sut = GetSystemUnderTest();
            Assert.IsNotNull(sut);

            return Task.CompletedTask;
        }

    }
}

