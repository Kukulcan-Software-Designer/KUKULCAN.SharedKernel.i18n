using System.Security.Claims;
using KUKULCAN.SharedKernel.i18n.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace KUKULCAN.SharedKernel.i18n.Infrastructure.UnitTests.Services;

[TestFixture]
public sealed class HttpCurrentUserTests
{
    [Test]
    public void WithoutHttpContext_ReturnsSystemDefaults()
    {
        var accessor = new HttpContextAccessor();
        var currentUser = new HttpCurrentUser(accessor);

        Assert.Multiple(() =>
        {
            Assert.That(currentUser.IsAuthenticated, Is.False);
            Assert.That(currentUser.UserId, Is.EqualTo(Guid.Empty));
            Assert.That(currentUser.UserName, Is.EqualTo("system"));
            Assert.That(currentUser.Email, Is.Null);
            Assert.That(currentUser.TenantId, Is.EqualTo(Guid.Empty));
            Assert.That(currentUser.Roles, Is.Empty);
            Assert.That(currentUser.IsInRole("admin"), Is.False);
        });
    }

    [Test]
    public void Claims_AreMappedToCurrentUser()
    {
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "juan"),
            new Claim(ClaimTypes.Email, "juan@example.test"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "editor")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var currentUser = new HttpCurrentUser(accessor);

        Assert.Multiple(() =>
        {
            Assert.That(currentUser.IsAuthenticated, Is.True);
            Assert.That(currentUser.UserId, Is.EqualTo(userId));
            Assert.That(currentUser.UserName, Is.EqualTo("juan"));
            Assert.That(currentUser.Email, Is.EqualTo("juan@example.test"));
            Assert.That(currentUser.Roles, Is.EquivalentTo(new[] { "admin", "editor" }));
            Assert.That(currentUser.IsInRole("admin"), Is.True);
            Assert.That(currentUser.IsInAllRoles("admin", "editor"), Is.True);
            Assert.That(currentUser.IsInAllRoles("admin", "missing"), Is.False);
        });
    }

    [Test]
    public void SubAndPreferredUsernameClaims_AreUsedAsFallbacks()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("preferred_username", "preferred"),
            new Claim("email", "preferred@example.test")
        ], "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new HttpCurrentUser(new HttpContextAccessor { HttpContext = context });

        Assert.Multiple(() =>
        {
            Assert.That(currentUser.UserId, Is.EqualTo(userId));
            Assert.That(currentUser.UserName, Is.EqualTo("preferred"));
            Assert.That(currentUser.Email, Is.EqualTo("preferred@example.test"));
        });
    }
}
