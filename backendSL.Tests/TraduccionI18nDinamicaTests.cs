using System.Net;
using System.Net.Http.Json;
using ProjectSaborLatino.Controllers;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class TraduccionI18nDinamicaTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public TraduccionI18nDinamicaTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ObtenerCatalogoDinamico_RetornaEstructuraI18nValida()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/i18n/catalogo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalogo = await response.Content.ReadFromJsonAsync<Dictionary<string, Dictionary<string, string>>>();
        Assert.NotNull(catalogo);
        Assert.True(catalogo.ContainsKey("en"));
        Assert.True(catalogo.ContainsKey("fr"));
        Assert.True(catalogo.ContainsKey("pt"));
    }

    [Fact]
    public async Task TraducirLote_RetornaDiccionarioTraducido()
    {
        using var client = _factory.CreateClient();

        var dto = new PeticionTraducirDto
        {
            IdiomaDestino = "en",
            Textos = ["Karaoke bailable", "Sonido profesional"]
        };

        var response = await client.PostAsJsonAsync("/api/i18n/traducir", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var resultado = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(resultado);
        Assert.True(resultado.ContainsKey("Karaoke bailable"));
        Assert.NotEmpty(resultado["Karaoke bailable"]);
    }
}
