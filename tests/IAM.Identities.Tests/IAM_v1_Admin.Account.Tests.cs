using Microsoft.Extensions.DependencyInjection;

namespace IAM.Identities.Tests
{
    public partial class IdentityAdminIF_v1_Tests
    {
        [TestMethod]
        public async Task IAM_Admin_createAccount_success()
        {
            var sut = GetSystemUnderTest();

            var result = await sut.createAccount(TestMain.ctx, "test_user", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(result.IsSuccess());

            var user = result.Value;
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.id);
            Assert.AreNotEqual(DateTime.MinValue, user.LastUpdate);
            Assert.IsNotNull(user.data);
            Assert.AreEqual(IIdentityAdminIF_v1.AccountTypes.User, user.data.Type);
            Assert.AreEqual("test_user", user.data.Name);
            Assert.IsTrue(user.data.isActive);
            Assert.AreEqual(0, user.data.contacts.Count);
        }

        [TestMethod]
        public async Task IAM_Admin_createAccount_then_can_fetch_it_back()
        {
            var sut = GetSystemUnderTest();

            var create = await sut.createAccount(TestMain.ctx, "alice", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(create.IsSuccess());

            var acc = create.Value;
            Assert.IsNotNull(acc.id);

            var fetched = await sut.getAccount(TestMain.ctx, acc.id);
            Assert.IsTrue(fetched.IsSuccess());
            Assert.AreEqual(acc.id, fetched.Value.id);
            Assert.AreEqual("alice", fetched.Value.data.Name);
            Assert.AreEqual(IIdentityAdminIF_v1.AccountTypes.User, fetched.Value.data.Type);
        }

        [TestMethod]
        public async Task IAM_Admin_createAccount_fail_already()
        {
            var admin_if = TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

            var result_1 = await admin_if.createAccount(TestMain.ctx, "test_user", IIdentityAdminIF_v1.AccountTypes.User);
            var result_2 = await admin_if.createAccount(TestMain.ctx, "test_user", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(result_1.IsSuccess());
            Assert.IsTrue(result_2.IsFailed());

            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, result_2.Error.Status);
            Assert.IsTrue(result_2.Error.MessageText.Contains("already"));
            Assert.IsTrue(result_2.Error.MessageText.Contains("test_user"));
        }


        [TestMethod]
        public async Task IAM_Admin_getAccount_success()
        {
            var sut = GetSystemUnderTest();

            var create = await sut.createAccount(TestMain.ctx, "bob", IIdentityAdminIF_v1.AccountTypes.InternalService);
            Assert.IsTrue(create.IsSuccess());

            var get = await sut.getAccount(TestMain.ctx, create.Value.id);
            Assert.IsTrue(get.IsSuccess());
            Assert.AreEqual("bob", get.Value.data.Name);
            Assert.AreEqual(IIdentityAdminIF_v1.AccountTypes.InternalService, get.Value.data.Type);
            Assert.IsTrue(get.Value.data.isActive);
        }

        [TestMethod]
        public async Task IAM_Admin_getAccount_fail_notfound()
        {
            var sut = GetSystemUnderTest();

            var get = await sut.getAccount(TestMain.ctx, Guid.NewGuid().ToString("N"));
            Assert.IsTrue(get.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.NotFound, get.Error.Status);
            Assert.IsTrue(get.Error.MessageText.Contains("not", StringComparison.OrdinalIgnoreCase)
                          || get.Error.MessageText.Contains("found", StringComparison.OrdinalIgnoreCase));
        }

        // --- getAllAccount ---

        [TestMethod]
        public async Task IAM_Admin_getAllAccount_success_empty()
        {
            var sut = GetSystemUnderTest();

            var list = await sut.getAllAccount(TestMain.ctx);
            Assert.IsTrue(list.IsSuccess());
            Assert.AreEqual(0, list.Value.Count);
        }

        [TestMethod]
        public async Task IAM_Admin_getAllAccount_success_with_items()
        {
            var sut = GetSystemUnderTest();

            var a1 = await sut.createAccount(TestMain.ctx, "charlie", IIdentityAdminIF_v1.AccountTypes.User);
            var a2 = await sut.createAccount(TestMain.ctx, "daisy", IIdentityAdminIF_v1.AccountTypes.ExternalSystem);
            Assert.IsTrue(a1.IsSuccess());
            Assert.IsTrue(a2.IsSuccess());

            var list = await sut.getAllAccount(TestMain.ctx);
            Assert.IsTrue(list.IsSuccess());
            // legalább a két frissen létrehozott benne van
            Assert.IsTrue(list.Value.Count >= 2);
            Assert.IsTrue(list.Value.Any(x => x.Name == "charlie"));
            Assert.IsTrue(list.Value.Any(x => x.Name == "daisy"));
        }

        // --- updateAccount ---

        [TestMethod]
        public async Task IAM_Admin_updateAccount_success_change_name_and_preserve_type()
        {
            var sut = GetSystemUnderTest();

            var created = await sut.createAccount(TestMain.ctx, "eve", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(created.IsSuccess());

            var before = created.Value;

            var updateDto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = "eve_renamed",
                Type = before.data.Type,          // marad User
                isActive = true,                  // marad aktív
                contacts = new()
            };

            var updated = await sut.updateAccount(TestMain.ctx, before.id, before.etag, updateDto);
            Assert.IsTrue(updated.IsSuccess());
            Assert.AreEqual("eve_renamed", updated.Value.data.Name);
            Assert.AreEqual(before.data.Type, updated.Value.data.Type);
            Assert.IsNotNull(updated.Value.etag);
            // ha az implementáció frissíti az etag-et, akkor általában változik:
            if (!string.IsNullOrEmpty(before.etag))
                Assert.AreNotEqual(before.etag, updated.Value.etag);
        }

        [TestMethod]
        public async Task IAM_Admin_updateAccount_fail_name_already_exists()
        {
            var sut = GetSystemUnderTest();

            var a1 = await sut.createAccount(TestMain.ctx, "foo", IIdentityAdminIF_v1.AccountTypes.User);
            var a2 = await sut.createAccount(TestMain.ctx, "bar", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(a1.IsSuccess());
            Assert.IsTrue(a2.IsSuccess());

            var dupName = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = "foo", // már létezik
                Type = a2.Value.data.Type,
                isActive = a2.Value.data.isActive,
                contacts = new()
            };

            var fail = await sut.updateAccount(TestMain.ctx, a2.Value.id, a2.Value.etag, dupName);
            Assert.IsTrue(fail.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, fail.Error.Status);
            Assert.IsTrue(fail.Error.MessageText.Contains("already", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(fail.Error.MessageText.Contains("foo"));
        }

        [TestMethod]
        public async Task IAM_Admin_updateAccount_fail_not_found()
        {
            var sut = GetSystemUnderTest();
            var dto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = "idontexist",
                Type = IIdentityAdminIF_v1.AccountTypes.InternalService,
                isActive = true,
                contacts = new()
            };

            var res = await sut.updateAccount(TestMain.ctx, "missing-id", "any-etag", dto);
            Assert.IsTrue(res.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.NotFound, res.Error.Status);
        }

        [TestMethod]
        public async Task IAM_Admin_updateAccount_fail_rename_to_existing_name()
        {
            var sut = GetSystemUnderTest();

            var a1 = await sut.createAccount(TestMain.ctx, "user", IIdentityAdminIF_v1.AccountTypes.User);
            var a2 = await sut.createAccount(TestMain.ctx, "another", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(a1.IsSuccess()); Assert.IsTrue(a2.IsSuccess());

            var dto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = "user",
                Type = a2.Value.data.Type,
                isActive = a2.Value.data.isActive,
                contacts = new()
            };

            var res = await sut.updateAccount(TestMain.ctx, a2.Value.id, a2.Value.etag, dto);
            Assert.IsTrue(res.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, res.Error.Status);
        }

        [TestMethod]
        public async Task IAM_Admin_UpdateAccount_Contacts_Add_Modify_Remove_Clear_roundtrip()
        {
            var sut = GetSystemUnderTest();

            // 0) létrehozás, kezdetben nincs contact
            var created = await sut.createAccount(TestMain.ctx, "contacts_user", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(created.IsSuccess(), created.Error?.MessageText);
            Assert.AreEqual(0, created.Value.data.contacts.Count);

            // 1) Hozzáadás: két kontakt (work, home) – elvárt ordering: ahogy küldjük
            var addDto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = created.Value.data.Name,
                Type = created.Value.data.Type,
                isActive = created.Value.data.isActive,
                contacts = new List<IIdentityAdminIF_v1.ContactInfo>()
                {
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "work", email = "work@example.com", phoneNumber = "+361111111"},
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "home", email = "home@example.com", phoneNumber = "+362222222"},
                },
            };

            var afterAdd = await sut.updateAccount(TestMain.ctx, created.Value.id, created.Value.etag, addDto);
            Assert.IsTrue(afterAdd.IsSuccess(), afterAdd.Error?.MessageText);
            Assert.AreEqual(2, afterAdd.Value.data.contacts.Count);
            Assert.AreEqual("work", afterAdd.Value.data.contacts[0].contactType);
            Assert.AreEqual("home", afterAdd.Value.data.contacts[1].contactType);

            // read-back ellenőrzés
            var read1 = await sut.getAccount(TestMain.ctx, created.Value.id);
            Assert.IsTrue(read1.IsSuccess());
            CollectionAssert.AreEqual(
                new[] { "work", "home" },
                read1.Value.data.contacts.Select(c => c.contactType).ToArray(),
                "Contact ordering should be preserved");

            // 2) Módosítás/replace: cseréljük a 'home' emailt, és hozzáadunk egy 'mobile' kontaktot
            var modifyDto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = read1.Value.data.Name,
                Type = read1.Value.data.Type,
                isActive = read1.Value.data.isActive,
                contacts = new()
                {
                    // változatlan
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "work", email = "work@example.com", phoneNumber = "+361111111"},
                    // email csere
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "home", email = "new_home@example.com", phoneNumber = "+362222222"},
                    // új
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "mobile", email = "new_home@example.com", phoneNumber = "+363333333"},
                }
            };

            var afterModify = await sut.updateAccount(TestMain.ctx, read1.Value.id, read1.Value.etag, modifyDto);
            Assert.IsTrue(afterModify.IsSuccess(), afterModify.Error?.MessageText);
            Assert.AreEqual(3, afterModify.Value.data.contacts.Count);
            Assert.AreEqual("new_home@example.com", afterModify.Value.data.contacts[1].email);
            Assert.AreEqual("mobile", afterModify.Value.data.contacts[2].contactType);

            // read-back ellenőrzés
            var read2 = await sut.getAccount(TestMain.ctx, created.Value.id);
            Assert.IsTrue(read2.IsSuccess());
            Assert.AreEqual(3, read2.Value.data.contacts.Count);
            Assert.IsTrue(read2.Value.data.contacts.Any(c => c.contactType == "home" && c.email == "new_home@example.com"));
            Assert.IsTrue(read2.Value.data.contacts.Any(c => c.contactType == "mobile" && c.phoneNumber == "+363333333"));

            // 3) Törlés: távolítsuk el a 'home'-ot, a többiek maradnak
            var removeDto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = read2.Value.data.Name,
                Type = read2.Value.data.Type,
                isActive = read2.Value.data.isActive,
                contacts = new()
                {
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "work", email = "work@example.com", phoneNumber = "+361111111"},
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "mobile", email = "mobile@example.com", phoneNumber = "+363333333"},
                }
            };

            var afterRemove = await sut.updateAccount(TestMain.ctx, read2.Value.id, read2.Value.etag, removeDto);
            Assert.IsTrue(afterRemove.IsSuccess(), afterRemove.Error?.MessageText);
            CollectionAssert.AreEquivalent(
                new[] { "work", "mobile" },
                afterRemove.Value.data.contacts.Select(c => c.contactType).ToArray(),
                "Contact set after removal is incorrect");

            var read3 = await sut.getAccount(TestMain.ctx, created.Value.id);
            Assert.IsTrue(read3.IsSuccess());
            Assert.AreEqual(2, read3.Value.data.contacts.Count);
            Assert.IsFalse(read3.Value.data.contacts.Any(c => c.contactType == "home"));

            // 4) Teljes clear: üres lista küldése felülírja a korábbit -> 0 kontakt
            var clearDto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = read3.Value.data.Name,
                Type = read3.Value.data.Type,
                isActive = read3.Value.data.isActive,
                contacts = new() // üres
            };

            var afterClear = await sut.updateAccount(TestMain.ctx, read3.Value.id, read3.Value.etag, clearDto);
            Assert.IsTrue(afterClear.IsSuccess(), afterClear.Error?.MessageText);
            Assert.AreEqual(0, afterClear.Value.data.contacts.Count);

            var read4 = await sut.getAccount(TestMain.ctx, created.Value.id);
            Assert.IsTrue(read4.IsSuccess());
            Assert.AreEqual(0, read4.Value.data.contacts.Count);
        }

        [TestMethod]
        public async Task IAM_Admin_UpdateAccount_Contacts_Idempotent_when_same_payload()
        {
            var sut = GetSystemUnderTest();
            // Létrehozás + két kontakt
            var created = await sut.createAccount(TestMain.ctx, "idem_user", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(created.IsSuccess());

            var dto = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = "idem_user",
                Type = IIdentityAdminIF_v1.AccountTypes.User,
                isActive = true,
                contacts = new()
                {
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "work", email = "work@example.com", phoneNumber = "+361111111"},
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "home", email = "home@example.com", phoneNumber = "+362222222"},
                }
            };

            var upd1 = await sut.updateAccount(TestMain.ctx, created.Value.id, created.Value.etag, dto);
            Assert.IsTrue(upd1.IsSuccess());

            // Ugyanaz a payload ismét – elvárás: siker, tartalmilag változatlan lista
            var upd2 = await sut.updateAccount(TestMain.ctx, upd1.Value.id, upd1.Value.etag, dto);
            Assert.IsTrue(upd2.IsSuccess());
            Assert.AreEqual(2, upd2.Value.data.contacts.Count);
            CollectionAssert.AreEqual(
                upd1.Value.data.contacts.Select(c => (c.contactType, c.email, c.phoneNumber)).ToArray(),
                upd2.Value.data.contacts.Select(c => (c.contactType, c.email, c.phoneNumber)).ToArray(),
                "Idempotent update should keep the same contact list and order");
        }

        [TestMethod]
        public async Task IAM_Admin_UpdateAccount_Contacts_Order_is_preserved()
        {
            var sut = GetSystemUnderTest();
            var created = await sut.createAccount(TestMain.ctx, "order_user", IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(created.IsSuccess());

            var ordered = new IIdentityAdminIF_v1.AccountDataDTO
            {
                Name = "order_user",
                Type = IIdentityAdminIF_v1.AccountTypes.User,
                isActive = true,
                contacts = new()
                {
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "c1", email = "1@x", phoneNumber = "1"},
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "c2", email = "2@x", phoneNumber = "2"},
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "c3", email = "3@x", phoneNumber = "3"},
                    new IIdentityAdminIF_v1.ContactInfo(){  contactType = "c4", email = "4@x", phoneNumber = "4"},
                }
            };

            var upd = await sut.updateAccount(TestMain.ctx, created.Value.id, created.Value.etag, ordered);
            Assert.IsTrue(upd.IsSuccess(), upd.Error?.MessageText);

            var fetched = await sut.getAccount(TestMain.ctx, created.Value.id);
            Assert.IsTrue(fetched.IsSuccess());

            CollectionAssert.AreEqual(
                new[] { "c1", "c2", "c3", "c4" },
                fetched.Value.data.contacts.Select(c => c.contactType).ToArray(),
                "Contact order changed unexpectedly");
        }
    }
}

