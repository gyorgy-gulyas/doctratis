using Microsoft.Extensions.DependencyInjection;

namespace IAM.Identities.Tests
{
    public class IdentityAdminIF_v1_Tests
    {
        [TestMethod]
        public static void IAM_Admin_Init(TestContext context)
        {
            var admin_if = TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

            Assert.IsNotNull(admin_if);
        }
    }
}
