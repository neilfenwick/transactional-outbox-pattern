using System.Net.Http.Json;
using Contacts.Application.Commands;
using Contacts.IntegrationTests.Fixtures;

namespace Contacts.IntegrationTests;

public class CreateContactTests(ContactsApiFactory factory) : IClassFixture<ContactsApiFactory>
{
    private readonly ContactsApiFactory _factory = factory;

    [Fact]
    public async Task CreateContact_Should_Use_TransactionalBatch()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new CreateContactCommand
        {
            FirstName = "Test",
            LastName = "Tester",
            CompanyName = "Testing",
            Email = "a@b.com",
            Description = "Test Contact"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/contacts", command);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
