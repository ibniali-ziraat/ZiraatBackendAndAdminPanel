using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Core.Areas.Core.Controllers;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace ECommerce.Module.Core.Tests.Controllers
{
    public class LicensingComplianceDocumentTests : IDisposable
    {
        private readonly ComplianceDb _db = new();

        [Fact]
        public async Task PatchEmptyArrayDeletesRowsAndAllReadsReturnNoSupportingDocuments()
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            Assert.IsType<OkObjectResult>(await Controller().UpdateCompliance(record.Id, Patch(license.Id, new List<AdditionalDocumentViewModel>())));
            _db.ChangeTracker.Clear();
            var service = Service();
            Assert.Empty((await service.GetByIdAsync(record.Id)).Licenses.Single().AdditionalDocuments);
            Assert.Empty((await service.GetBySupplierProfileIdAsync(7)).Licenses.Single().AdditionalDocuments);
            Assert.Empty((await service.GetLicenseByIdAsync(license.Id)).AdditionalDocuments);
            Assert.Empty(_db.Set<AdditionalDocument>());
            Assert.Single(_db.Set<LicenseDocument>());
        }

        [Fact]
        public async Task PatchSubsetDeletesOnlyRemovedDocumentAndPreservesRetainedId()
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            var keep = license.AdditionalDocuments.Last();
            Assert.IsType<OkObjectResult>(await Controller().UpdateCompliance(record.Id,
                Patch(license.Id, new List<AdditionalDocumentViewModel> { new() { Id = keep.Id } })));
            _db.ChangeTracker.Clear();
            var saved = Assert.Single((await Service().GetLicenseByIdAsync(license.Id)).AdditionalDocuments);
            Assert.Equal(keep.Id, saved.Id);
            Assert.Equal("b.pdf", saved.FileName);
            Assert.Single(_db.Set<AdditionalDocument>());
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"additionalDocuments\":null}")]
        public async Task PatchOmittedOrNullDocumentsPreservesExistingRows(string json)
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            var model = JsonConvert.DeserializeObject<LicenseViewModel>(json);
            model.LicenseId = license.Id;
            model.LicenseNumber = "Updated";
            Assert.IsType<OkObjectResult>(await Controller().UpdateCompliance(record.Id,
                new LicensingComplianceViewModel { Licenses = new List<LicenseViewModel> { model } }));
            _db.ChangeTracker.Clear();
            var saved = await Service().GetLicenseByIdAsync(license.Id);
            Assert.Equal(2, saved.AdditionalDocuments.Count);
            Assert.Equal("Updated", saved.LicenseNumber);
        }

        [Fact]
        public async Task PutEmptyArrayAlsoDeletesSupportingDocuments()
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            Assert.IsType<OkObjectResult>(await Controller().UpdateLicense(license.Id,
                new LicenseViewModel { AdditionalDocuments = new List<AdditionalDocumentViewModel>() }));
            _db.ChangeTracker.Clear();
            Assert.Empty((await Service().GetLicenseByIdAsync(license.Id)).AdditionalDocuments);
            Assert.Empty(_db.Set<AdditionalDocument>());
        }

        [Fact]
        public async Task LegacyPayloadWithoutIdsMatchesFileWithinSameDocumentType()
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            var keep = license.AdditionalDocuments.Last();
            Assert.IsType<OkObjectResult>(await Controller().UpdateCompliance(record.Id,
                Patch(license.Id, new List<AdditionalDocumentViewModel>
                {
                    new() { DocumentType = keep.DocumentType, FileName = keep.FileName, FileUrl = keep.FileUrl }
                })));
            _db.ChangeTracker.Clear();
            Assert.Equal(keep.Id, Assert.Single((await Service().GetLicenseByIdAsync(license.Id)).AdditionalDocuments).Id);
        }

        [Fact]
        public async Task PatchCanAddAndRemoveInSameSave()
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            Assert.IsType<OkObjectResult>(await Controller().UpdateCompliance(record.Id,
                Patch(license.Id, new List<AdditionalDocumentViewModel>
                {
                    new() { DocumentType = "New type", FileName = "new.pdf", FileUrl = "/new.pdf" }
                })));
            _db.ChangeTracker.Clear();
            Assert.Equal("new.pdf", Assert.Single((await Service().GetLicenseByIdAsync(license.Id)).AdditionalDocuments).FileName);
            Assert.Single(_db.Set<AdditionalDocument>());
        }

        [Fact]
        public async Task DocumentIdFromAnotherLicenseIsRejectedWithoutDeletingAnything()
        {
            var record = await Seed();
            var other = await Seed();
            var foreignDoc = other.Licenses.Single().AdditionalDocuments.First();
            Assert.IsType<BadRequestObjectResult>(await Controller().UpdateCompliance(record.Id,
                Patch(record.Licenses.Single().Id, new List<AdditionalDocumentViewModel> { new() { Id = foreignDoc.Id } })));
            _db.ChangeTracker.Clear();
            Assert.Equal(4, await _db.Set<AdditionalDocument>().CountAsync());
        }

        [Fact]
        public async Task DuplicateDocumentIdIsRejectedWithoutDeletingAnything()
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            var id = license.AdditionalDocuments.First().Id;
            Assert.IsType<BadRequestObjectResult>(await Controller().UpdateCompliance(record.Id,
                Patch(license.Id, new List<AdditionalDocumentViewModel> { new() { Id = id }, new() { Id = id } })));
            _db.ChangeTracker.Clear();
            Assert.Equal(2, await _db.Set<AdditionalDocument>().CountAsync());
        }

        [Fact]
        public async Task NullDocumentEntryIsRejectedWithoutDeletingAnything()
        {
            var record = await Seed();
            Assert.IsType<BadRequestObjectResult>(await Controller().UpdateCompliance(record.Id,
                Patch(record.Licenses.Single().Id, new List<AdditionalDocumentViewModel> { null })));
            _db.ChangeTracker.Clear();
            Assert.Equal(2, await _db.Set<AdditionalDocument>().CountAsync());
        }

        [Fact]
        public async Task SubmittedRemovalPayloadClearsOnlyAdditionalDocumentRows()
        {
            var record = await Seed();
            var license = record.Licenses.Single();
            var json = JsonConvert.SerializeObject(new
            {
                licensingId = record.Id,
                supplierProfileId = 7,
                gstNumber = "07ABCDE1234F1Z5",
                panNumber = "ABCDE1234F",
                licenses = new[]
                {
                    new
                    {
                        licenseId = license.Id,
                        licenseType = "TRADE",
                        licenseNumber = "45234423453245",
                        startDate = "2026-09-14T00:00:00.000Z",
                        expiryDate = "2026-09-25T00:00:00.000Z",
                        licenseDocument = new { fileName = "creditcustomerss.png", fileUrl = "" },
                        additionalDocuments = Array.Empty<object>()
                    }
                }
            });
            var model = JsonConvert.DeserializeObject<LicensingComplianceViewModel>(json);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                Assert.IsType<OkObjectResult>(await Controller().UpdateCompliance(record.Id, model));
                _db.ChangeTracker.Clear();
                var read = await Controller().GetById(record.Id);
                var saved = Assert.IsType<LicensingCompliance>(Assert.IsType<OkObjectResult>(read.Result).Value);
                Assert.Empty(saved.Licenses.Single().AdditionalDocuments);
                Assert.Empty(_db.Set<AdditionalDocument>());
                Assert.NotNull(saved.Licenses.Single().LicenseDocument);
                Assert.Single(_db.Set<LicenseDocument>());
            }
        }

        private static LicensingComplianceViewModel Patch(long licenseId, ICollection<AdditionalDocumentViewModel> documents) => new()
        {
            Licenses = new List<LicenseViewModel> { new() { LicenseId = licenseId, AdditionalDocuments = documents } }
        };

        private async Task<LicensingCompliance> Seed()
        {
            var record = new LicensingCompliance
            {
                SupplierProfileId = 7,
                Licenses = new List<License>
                {
                    new()
                    {
                        LicenseType = "Dealer", LicenseNumber = "License",
                        LicenseDocument = new LicenseDocument { FileName = "primary.pdf", FileUrl = "/primary.pdf" },
                        AdditionalDocuments = new List<AdditionalDocument>
                        {
                            new() { DocumentType = "Supporting", FileName = "a.pdf", FileUrl = "/a.pdf" },
                            new() { DocumentType = "Supporting", FileName = "b.pdf", FileUrl = "/b.pdf" }
                        }
                    }
                }
            };
            await Service().AddAsync(record);
            return record;
        }

        private LicensingComplianceController Controller() => new(Service());

        private LicensingComplianceService Service() => new(Repo<LicensingCompliance>(), Repo<License>(),
            Repo<LicenseDocument>(), Repo<AdditionalDocument>());

        private IRepository<T> Repo<T>() where T : EntityBase
        {
            var repository = new Mock<IRepository<T>>();
            repository.Setup(x => x.Query()).Returns(() => _db.Set<T>());
            repository.Setup(x => x.Add(It.IsAny<T>())).Callback<T>(x => _db.Add(x));
            repository.Setup(x => x.Remove(It.IsAny<T>())).Callback<T>(x => _db.Remove(x));
            repository.Setup(x => x.SaveChangesAsync()).Returns(async () => { await _db.SaveChangesAsync(); });
            return repository.Object;
        }

        public void Dispose() => _db.Dispose();

        private sealed class ComplianceDb : DbContext
        {
            public ComplianceDb() : base(new DbContextOptionsBuilder<ComplianceDb>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<LicensingCompliance>().HasMany(x => x.Licenses).WithOne().HasForeignKey(x => x.LicensingComplianceId);
                builder.Entity<License>().HasMany(x => x.AdditionalDocuments).WithOne().HasForeignKey(x => x.LicenseId);
                builder.Entity<License>().HasOne(x => x.LicenseDocument).WithOne().HasForeignKey<LicenseDocument>(x => x.LicenseId);
                // Match SimplDbContext: relationship deletion is not automatic.
                foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
                    relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
