using Microsoft.Extensions.DependencyInjection;

namespace IAM.Identities.Tests
{
    [TestClass]
    public partial class IdentityAdminIF_v1_Ldap_Tests
    {
        private IIdentityAdminIF_v1 Sut =>
           TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

        [TestInitialize]
        public async Task Setup()
        {
            await TestMain.DeleteAllData();
        }

        // --- helpers ---------------------------------------------------------

        private static IIdentityAdminIF_v1.LdapDomainDTO Ldap(
            string name = "corp.local",
            string description = "Main AD",
            string netbios = "CORP",
            bool ldaps = true,
            string baseDn = "DC=corp,DC=local",
            string svcUser = "svc_ldap",
            string svcPwd = "secret123",
            params (string host, int port)[] controllers)
        {
            var dto = new IIdentityAdminIF_v1.LdapDomainDTO
            {
                name = name,
                description = description,
                netbiosName = netbios,
                baseDn = baseDn,
                useSecureLdap = ldaps,
                serviceAccountUser = svcUser,
                serviceAccountPassword = svcPwd,
                domainControllers = new()
            };

            if (controllers == null || controllers.Length == 0)
            {
                dto.domainControllers.Add(new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = "dc1.corp.local", port = ldaps ? 636 : 389 });
                dto.domainControllers.Add(new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = "dc2.corp.local", port = ldaps ? 636 : 389 });
            }
            else
            {
                foreach (var c in controllers)
                    dto.domainControllers.Add(new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = c.host, port = c.port });
            }

            return dto;
        }

        // --- RegisterLdapDomain ---------------------------------------------

        [TestMethod]
        public async Task Ldap_Register_success_minimal()
        {
            var res = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap());
            Assert.IsTrue(res.IsSuccess(), res.Error?.MessageText);

            var dto = res.Value;
            Assert.IsFalse(string.IsNullOrEmpty(dto.id));
            Assert.IsFalse(string.IsNullOrEmpty(dto.etag));
            Assert.AreNotEqual(DateTime.MinValue, dto.LastUpdate);
            Assert.AreEqual("corp.local", dto.name);
            Assert.AreEqual("CORP", dto.netbiosName);
            Assert.AreEqual("DC=corp,DC=local", dto.baseDn);
            Assert.IsTrue(dto.useSecureLdap);
            Assert.AreEqual(2, dto.domainControllers.Count);
        }

        [TestMethod]
        public async Task Ldap_Register_fail_missing_name()
        {
            var bad = Ldap(name: "");
            var res = await Sut.RegisterLdapDomain(TestMain.ctx, bad);

            Assert.IsTrue(res.IsFailed(), "Missing domain name should be rejected.");
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, res.Error.Status);
        }

        // --- GetAllRegisteredLdapDomain -------------------------------------

        [TestMethod]
        public async Task Ldap_GetAll_empty_then_with_items()
        {
            var list0 = await Sut.GetAllRegisteredLdapDomain(TestMain.ctx);
            Assert.IsTrue(list0.IsSuccess());
            Assert.AreEqual(0, list0.Value.Count);

            var r1 = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "corp.local", netbios: "CORP"));
            var r2 = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "lab.local", netbios: "LAB", description: "Lab AD"));
            Assert.IsTrue(r1.IsSuccess()); Assert.IsTrue(r2.IsSuccess());

            var list = await Sut.GetAllRegisteredLdapDomain(TestMain.ctx);
            Assert.IsTrue(list.IsSuccess());
            Assert.IsTrue(list.Value.Count >= 2);

            var corp = list.Value.Single(x => x.name == "corp.local");
            var lab = list.Value.Single(x => x.name == "lab.local");
            Assert.IsFalse(string.IsNullOrEmpty(corp.id));
            Assert.AreEqual("Lab AD", lab.description);
        }

        // --- GetRegisteredLdapDomain ----------------------------------------

        [TestMethod]
        public async Task Ldap_GetById_success()
        {
            var created = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "getme.local", netbios: "GETME"));
            Assert.IsTrue(created.IsSuccess());

            var byId = await Sut.GetRegisteredLdapDomain(TestMain.ctx, created.Value.id);
            Assert.IsTrue(byId.IsSuccess());
            Assert.AreEqual("getme.local", byId.Value.name);
            Assert.AreEqual("GETME", byId.Value.netbiosName);
            Assert.AreEqual(created.Value.id, byId.Value.id);
        }

        [TestMethod]
        public async Task Ldap_GetById_fail_notfound()
        {
            var byId = await Sut.GetRegisteredLdapDomain(TestMain.ctx, Guid.NewGuid().ToString("N"));
            Assert.IsTrue(byId.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.NotFound, byId.Error.Status);
        }

        // --- UpdateRegisteredLdapDomain -------------------------------------

        [TestMethod]
        public async Task Ldap_Update_success_change_desc_toggle_ldaps_change_controllers()
        {
            var created = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "upd.local", ldaps: true));
            Assert.IsTrue(created.IsSuccess());

            var dto = created.Value;
            dto.description = "Updated description";
            dto.useSecureLdap = false; // toggle
            dto.domainControllers = new()
            {
                new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = "newdc1.upd.local", port = 389 },
                new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = "newdc2.upd.local", port = 389 },
                new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = "newdc3.upd.local", port = 389 },
            };

            var updated = await Sut.UpdateRegisteredLdapDomain(TestMain.ctx, dto);
            Assert.IsTrue(updated.IsSuccess(), updated.Error?.MessageText);

            Assert.AreEqual("Updated description", updated.Value.description);
            Assert.IsFalse(updated.Value.useSecureLdap);
            Assert.AreEqual(3, updated.Value.domainControllers.Count);
            Assert.IsTrue(updated.Value.domainControllers.Any(c => c.host == "newdc3.upd.local"));
            // etag/LastUpdate változhat
            if (!string.IsNullOrEmpty(created.Value.etag))
                Assert.AreNotEqual(created.Value.etag, updated.Value.etag);
            Assert.IsTrue(updated.Value.LastUpdate >= created.Value.LastUpdate);
        }

        [TestMethod]
        public async Task Ldap_Update_fail_notfound()
        {
            var ghost = Ldap(name: "ghost.local");
            ghost.id = "does-not-exist";
            ghost.etag = "any";
            var res = await Sut.UpdateRegisteredLdapDomain(TestMain.ctx, ghost);
            Assert.IsTrue(res.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.NotFound, res.Error.Status);
        }

        [TestMethod]
        public async Task Ldap_Update_fail_missing_required_fields()
        {
            var created = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "req.local"));
            Assert.IsTrue(created.IsSuccess());

            var dto = created.Value;
            dto.name = ""; // required
            var res = await Sut.UpdateRegisteredLdapDomain(TestMain.ctx, dto);

            Assert.IsTrue(res.IsFailed(), "Empty name should be rejected on update as well.");
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, res.Error.Status);
        }

        [TestMethod]
        public async Task Ldap_Register_fail_duplicate_name_case_insensitive()
        {
            var r1 = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "corp.local", netbios: "CORP"));
            Assert.IsTrue(r1.IsSuccess(), r1.Error?.MessageText);

            // ugyanaz a név más case-szel
            var r2 = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "CoRp.LoCaL", netbios: "CORP2"));
            Assert.IsTrue(r2.IsFailed(), "Case-insensitive uniqueness expected on LDAP domain name.");
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, r2.Error.Status);
        }

        // --- 2) Duplikált név: átnevezés (update) (case-insensitive) --------

        [TestMethod]
        public async Task Ldap_Update_fail_rename_to_existing_name_case_insensitive()
        {
            var a = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "a.local", netbios: "A"));
            var b = await Sut.RegisterLdapDomain(TestMain.ctx, Ldap(name: "b.local", netbios: "B"));
            Assert.IsTrue(a.IsSuccess()); Assert.IsTrue(b.IsSuccess());

            // próbáld a B-t átnevezni "A.Local"-ra
            var toUpdate = b.Value;
            toUpdate.name = "A.Local";

            var upd = await Sut.UpdateRegisteredLdapDomain(TestMain.ctx, toUpdate);
            Assert.IsTrue(upd.IsFailed(), "Renaming to existing domain name (case-insensitive) should fail.");
            // Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, upd.Error.Status);
        }

        // --- 3) Vegyes DC portok (389/636) engedettek és megmaradnak --------

        [TestMethod]
        public async Task Ldap_Register_success_mixed_dc_ports_allowed_and_persisted()
        {
            var dto = Ldap(
                name: "mix.local",
                ldaps: true, // ettől függetlenül vegyes portokat küldünk
                controllers: new (string, int)[]
                {
                    ("dc1.mix.local", 636),
                    ("dc2.mix.local", 389),
                    ("dc3.mix.local", 636),
                });

            var reg = await Sut.RegisterLdapDomain(TestMain.ctx, dto);
            Assert.IsTrue(reg.IsSuccess(), reg.Error?.MessageText);

            // ellenőrzés: pontosan a küldött portok maradnak
            var c = reg.Value.domainControllers;
            Assert.AreEqual(3, c.Count);
            Assert.IsTrue(c.Any(x => x.host == "dc1.mix.local" && x.port == 636));
            Assert.IsTrue(c.Any(x => x.host == "dc2.mix.local" && x.port == 389));
            Assert.IsTrue(c.Any(x => x.host == "dc3.mix.local" && x.port == 636));
        }

        // --- 4) GetAll csak összefoglaló: nincs érzékeny mező-szivárgás ------

        [TestMethod]
        public async Task Ldap_GetAll_summaries_only_no_sensitive_fields()
        {
            // létrehozás érzékeny mezőkkel
            var reg = await Sut.RegisterLdapDomain(TestMain.ctx,
                Ldap(name: "safe.local", svcUser: "svc_user", svcPwd: "SuperSecret!"));
            Assert.IsTrue(reg.IsSuccess(), reg.Error?.MessageText);

            // 1) Read full by id – biztos, hogy a részletes DTO tartalmaz érzékeny mezőt
            var full = await Sut.GetRegisteredLdapDomain(TestMain.ctx, reg.Value.id);
            Assert.IsTrue(full.IsSuccess());
            Assert.AreEqual("svc_user", full.Value.serviceAccountUser);
            Assert.AreEqual("SuperSecret!", full.Value.serviceAccountPassword);

            // 2) GetAll – összefoglalók jönnek vissza
            var list = await Sut.GetAllRegisteredLdapDomain(TestMain.ctx);
            Assert.IsTrue(list.IsSuccess());

            // ellenőrzés: megtaláljuk a safe.local-t
            var summary = list.Value.Single(x => x.name == "safe.local");
            Assert.IsFalse(string.IsNullOrEmpty(summary.id));
            Assert.AreEqual("safe.local", summary.name);

            // erős típus szerint itt amúgy sincs serviceAccountPassword mező,
            // de tegyünk rá egy refleksziós ellenőrzést is, hogy véletlen se szivárgjon:
            var summaryType = summary.GetType();
            Assert.IsNull(summaryType.GetProperty("serviceAccountUser"), "Summary DTO must not expose serviceAccountUser.");
            Assert.IsNull(summaryType.GetProperty("serviceAccountPassword"), "Summary DTO must not expose serviceAccountPassword.");
            Assert.IsNull(summaryType.GetProperty("baseDn"), "Summary DTO must not expose baseDn.");
            Assert.IsNull(summaryType.GetProperty("domainControllers"), "Summary DTO must not expose domainControllers.");
            Assert.IsNull(summaryType.GetProperty("useSecureLdap"), "Summary DTO must not expose useSecureLdap.");
        }
    }
}

