using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Properties;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imoveis.Tests;

public sealed class PropertyPartyLinkFlowTests
{
    [Fact]
    public async Task CreateWithPartyIdsMirrorsFieldsAndPersistsLinks()
    {
        await using var dbContext = CreateDbContext();
        var owner = CreateParty("PROPRIETARIO", "Maria Proprietaria", "11911110000", "maria@teste.com");
        var administrator = CreateParty("ADMINISTRADOR", "Carlos Admin", "11922220000", "carlos@teste.com");
        var lawyer = CreateParty("ADVOGADO", "Ana Advogada", "11933330000", "ana@teste.com");
        dbContext.Parties.AddRange(owner, administrator, lawyer);
        await dbContext.SaveChangesAsync();

        var service = new PropertyService(dbContext);
        var created = await service.CreateAsync(BuildCreateRequest(
            owner.Id,
            administrator.Id,
            lawyer.Id,
            proprietary: null,
            administratorName: null,
            administratorPhone: null,
            administratorEmail: null,
            lawyerName: null), CancellationToken.None);

        Assert.Equal(owner.Id, created.ProprietaryPartyId);
        Assert.Equal(administrator.Id, created.AdministratorPartyId);
        Assert.Equal(lawyer.Id, created.LawyerPartyId);
        Assert.Equal(owner.Name, created.Proprietary);
        Assert.Equal(administrator.Name, created.Administrator);
        Assert.Equal(administrator.Phone, created.Administration.AdministratorPhone);
        Assert.Equal(administrator.Email, created.Administration.AdministratorEmail);
        Assert.Equal(lawyer.Name, created.Administration.Lawyer);

        var links = await dbContext.PropertyPartyLinks
            .Where(x => x.PropertyId == created.Id)
            .OrderBy(x => x.Role)
            .ToListAsync();

        Assert.Collection(
            links,
            item => Assert.Equal(PropertyPartyRole.OWNER, item.Role),
            item => Assert.Equal(PropertyPartyRole.ADMINISTRATOR, item.Role),
            item => Assert.Equal(PropertyPartyRole.LAWYER, item.Role));
    }

    [Fact]
    public async Task UpdateCanSwapAndRemoveLinkedParties()
    {
        await using var dbContext = CreateDbContext();
        var ownerA = CreateParty("PROPRIETARIO", "Maria Proprietaria", "11911110000", "maria@teste.com");
        var ownerB = CreateParty("PROPRIETARIO", "Joao Proprietario", "11944440000", "joao@teste.com");
        var administrator = CreateParty("ADMINISTRADOR", "Carlos Admin", "11922220000", "carlos@teste.com");
        var lawyer = CreateParty("ADVOGADO", "Ana Advogada", "11933330000", "ana@teste.com");
        dbContext.Parties.AddRange(ownerA, ownerB, administrator, lawyer);
        await dbContext.SaveChangesAsync();

        var service = new PropertyService(dbContext);
        var created = await service.CreateAsync(BuildCreateRequest(ownerA.Id, administrator.Id, lawyer.Id), CancellationToken.None);
        dbContext.ChangeTracker.Clear();

        var updated = await service.UpdateAsync(
            created.Id,
            new PropertyUpdateRequest(
                new PropertyIdentitySectionRequest("Apartamento Centro", "Rua A, 10 (Centro)", "Sao Paulo", "SP", "01001000", "Apartamento", PropertyStatusContract.Disponivel, null),
                new PropertyDocumentationSectionRequest(null, null, null),
                new PropertyCharacteristicsSectionRequest(2, false, false, true, null),
                new PropertyAdministrationSectionRequest(
                    null,
                    ownerB.Id,
                    null,
                    null,
                    null,
                    null,
                    "10%",
                    null,
                    null,
                    "OAB 123",
                    "Atualizado")),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(ownerB.Id, updated.ProprietaryPartyId);
        Assert.Null(updated.AdministratorPartyId);
        Assert.Null(updated.LawyerPartyId);
        Assert.Equal(ownerB.Name, updated.Proprietary);
        Assert.Null(updated.Administrator);
        Assert.Null(updated.Administration.AdministratorPhone);
        Assert.Null(updated.Administration.AdministratorEmail);
        Assert.Null(updated.Administration.Lawyer);

        var links = await dbContext.PropertyPartyLinks
            .Where(x => x.PropertyId == created.Id)
            .OrderBy(x => x.Role)
            .ToListAsync();

        Assert.Single(links);
        Assert.Equal(PropertyPartyRole.OWNER, links[0].Role);
        Assert.Equal(ownerB.Id, links[0].PartyId);
    }

    [Fact]
    public async Task CreateWithoutPartyIdsKeepsLegacyManualFields()
    {
        await using var dbContext = CreateDbContext();
        var service = new PropertyService(dbContext);

        var created = await service.CreateAsync(
            BuildCreateRequest(
                null,
                null,
                null,
                proprietary: "Maria Manual",
                administratorName: "Carlos Manual",
                administratorPhone: "11922220000",
                administratorEmail: "carlos@manual.com",
                lawyerName: "Ana Manual"),
            CancellationToken.None);

        Assert.Null(created.ProprietaryPartyId);
        Assert.Null(created.AdministratorPartyId);
        Assert.Null(created.LawyerPartyId);
        Assert.Equal("Maria Manual", created.Proprietary);
        Assert.Equal("Carlos Manual", created.Administrator);
        Assert.Equal("11922220000", created.Administration.AdministratorPhone);
        Assert.Equal("carlos@manual.com", created.Administration.AdministratorEmail);
        Assert.Equal("Ana Manual", created.Administration.Lawyer);
        Assert.False(await dbContext.PropertyPartyLinks.AnyAsync(x => x.PropertyId == created.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateRejectsInactiveOrMissingParty(bool useExistingButInactive)
    {
        await using var dbContext = CreateDbContext();
        Guid? ownerId = null;

        if (useExistingButInactive)
        {
            var inactiveParty = CreateParty("PROPRIETARIO", "Maria Inativa", "11911110000", "maria@teste.com");
            inactiveParty.IsActive = false;
            dbContext.Parties.Add(inactiveParty);
            await dbContext.SaveChangesAsync();
            ownerId = inactiveParty.Id;
        }
        else
        {
            ownerId = Guid.NewGuid();
        }

        var service = new PropertyService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(BuildCreateRequest(ownerId, null, null, proprietary: null), CancellationToken.None));

        Assert.Equal("validation_error", exception.Code);
    }

    private static PropertyCreateRequest BuildCreateRequest(
        Guid? ownerId,
        Guid? administratorId,
        Guid? lawyerId,
        string? proprietary = "Maria Proprietaria",
        string? administratorName = "Carlos Admin",
        string? administratorPhone = "11922220000",
        string? administratorEmail = "carlos@teste.com",
        string? lawyerName = "Ana Advogada")
        => new(
            new PropertyIdentitySectionRequest(
                "Apartamento Centro",
                "Rua A, 10 (Centro)",
                "Sao Paulo",
                "SP",
                "01001000",
                "Apartamento",
                PropertyStatusContract.Disponivel,
                null),
            new PropertyDocumentationSectionRequest(null, null, null),
            new PropertyCharacteristicsSectionRequest(2, false, false, true, null),
            new PropertyAdministrationSectionRequest(
                proprietary,
                ownerId,
                administratorName,
                administratorId,
                administratorPhone,
                administratorEmail,
                "10%",
                lawyerName,
                lawyerId,
                "OAB 123",
                null),
            null,
            null);

    private static Party CreateParty(string kind, string name, string phone, string email)
        => new()
        {
            Kind = PartyKindContract.Parse(kind, "kind"),
            Name = name,
            Phone = phone,
            Email = email,
            IsActive = true
        };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"property-parties-tests-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }
}
