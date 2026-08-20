using KUKULCAN.SharedKernel.i18n.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KUKULCAN.SharedKernel.i18n.API.UnitTests.Controllers;

[TestFixture]
public sealed class CurrenciesControllerTests
{
    [Test]
    public void Controller_HasExpectedRouteAndProducesMetadata()
    {
        var type = typeof(CurrenciesController);
        var route = type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Single();
        var produces = type.GetCustomAttributes(typeof(ProducesAttribute), inherit: true).Single();

        Assert.That(((RouteAttribute)route).Template, Is.EqualTo("api/v1/currencies/{languageCode}"));
        Assert.That(((ProducesAttribute)produces).ContentTypes, Does.Contain("application/json"));
    }

    [Test]
    public void Upsert_HasExpectedHttpRouteAuthorizationAndResponseMetadata()
    {
        var method = typeof(CurrenciesController).GetMethod(nameof(CurrenciesController.Upsert))!;
        var httpPut = method.GetCustomAttributes(typeof(HttpPutAttribute), inherit: true).Single();
        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Single();
        var responses = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
            .Cast<ProducesResponseTypeAttribute>()
            .ToArray();

        Assert.That(((HttpPutAttribute)httpPut).Template, Is.EqualTo("{currencyCode}"));
        Assert.That(((AuthorizeAttribute)authorize).Policy, Is.EqualTo("i18n.write"));
        Assert.That(responses.Any(x => x.StatusCode == StatusCodes.Status200OK), Is.True);
        Assert.That(responses.Any(x => x.StatusCode == StatusCodes.Status200OK && x.Type == typeof(KUKULCAN.SharedKernel.i18n.Domain.DTOs.CurrencyFormatDto)), Is.True);
    }

    [Test]
    public void Delete_HasExpectedHttpRouteAuthorizationAndResponseMetadata()
    {
        var method = typeof(CurrenciesController).GetMethod(nameof(CurrenciesController.Delete))!;
        var httpDelete = method.GetCustomAttributes(typeof(HttpDeleteAttribute), inherit: true).Single();
        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Single();
        var response = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true).Single();

        Assert.That(((HttpDeleteAttribute)httpDelete).Template, Is.EqualTo("{currencyCode}"));
        Assert.That(((AuthorizeAttribute)authorize).Policy, Is.EqualTo("i18n.write"));
        Assert.That(((ProducesResponseTypeAttribute)response).StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));
    }

    [Test]
    public void GetByLanguage_HasExpectedHttpRouteAuthorizationAndResponseMetadata()
    {
        var method = typeof(CurrenciesController).GetMethod(nameof(CurrenciesController.GetByLanguage))!;
        var httpGet = method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).Single();
        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Single();
        var response = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true).Single();

        Assert.That(((HttpGetAttribute)httpGet).Template, Is.EqualTo(""));
        Assert.That(((AuthorizeAttribute)authorize).Policy, Is.EqualTo("i18n.read"));
        Assert.That(((ProducesResponseTypeAttribute)response).StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(((ProducesResponseTypeAttribute)response).Type, Is.EqualTo(typeof(IReadOnlyList<KUKULCAN.SharedKernel.i18n.Domain.DTOs.CurrencyFormatDto>)));
    }
}
