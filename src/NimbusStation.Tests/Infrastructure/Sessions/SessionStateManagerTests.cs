using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Sessions;

namespace NimbusStation.Tests.Infrastructure.Sessions;

public sealed class SessionStateManagerTests
{
    [Fact]
    public void CurrentSession_Initially_IsNull()
    {
        var manager = new SessionStateManager();

        Assert.Null(manager.CurrentSession);
    }

    [Fact]
    public void HasActiveSession_Initially_IsFalse()
    {
        var manager = new SessionStateManager();

        Assert.False(manager.HasActiveSession);
    }

    [Fact]
    public void ActivateSession_SetsCurrentSession()
    {
        var manager = new SessionStateManager();
        var session = Session.Create("TEST-123");

        manager.ActivateSession(session);

        Assert.Same(session, manager.CurrentSession);
        Assert.True(manager.HasActiveSession);
    }

    [Fact]
    public void ActivateSession_WithNull_Throws()
    {
        var manager = new SessionStateManager();

        Assert.Throws<ArgumentNullException>(() => manager.ActivateSession(null!));
    }

    [Fact]
    public void DeactivateSession_ClearsCurrentSession()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123"));

        manager.DeactivateSession();

        Assert.Null(manager.CurrentSession);
        Assert.False(manager.HasActiveSession);
    }

    [Fact]
    public void DeactivateSession_WhenNoSession_DoesNotThrow()
    {
        var manager = new SessionStateManager();

        var exception = Record.Exception(() => manager.DeactivateSession());

        Assert.Null(exception);
    }

    [Fact]
    public void SetCosmosAlias_SetsAlias()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123"));

        manager.SetCosmosAlias("prod-db");

        Assert.Equal("prod-db", manager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
    }

    [Fact]
    public void SetCosmosAlias_PreservesBlobAlias()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123").WithContext(new SessionContext(null, "my-blob")));

        manager.SetCosmosAlias("prod-db");

        Assert.Equal("prod-db", manager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("my-blob", manager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public void SetCosmosAlias_WhenNoSession_Throws()
    {
        var manager = new SessionStateManager();

        var exception = Assert.Throws<InvalidOperationException>(() => manager.SetCosmosAlias("prod-db"));
        Assert.Contains("No active session", exception.Message);
    }

    [Fact]
    public void SetCosmosAlias_WithNullOrEmpty_Throws()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123"));

        Assert.Throws<ArgumentNullException>(() => manager.SetCosmosAlias(null!));
        Assert.Throws<ArgumentException>(() => manager.SetCosmosAlias(""));
        Assert.Throws<ArgumentException>(() => manager.SetCosmosAlias("   "));
    }

    [Fact]
    public void SetBlobAlias_SetsAlias()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123"));

        manager.SetBlobAlias("prod-logs");

        Assert.Equal("prod-logs", manager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public void SetBlobAlias_PreservesCosmosAlias()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123").WithContext(new SessionContext("my-cosmos", null)));

        manager.SetBlobAlias("prod-logs");

        Assert.Equal("my-cosmos", manager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("prod-logs", manager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public void SetBlobAlias_WhenNoSession_Throws()
    {
        var manager = new SessionStateManager();

        var exception = Assert.Throws<InvalidOperationException>(() => manager.SetBlobAlias("prod-logs"));
        Assert.Contains("No active session", exception.Message);
    }

    [Fact]
    public void SetBlobAlias_WithNullOrEmpty_Throws()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123"));

        Assert.Throws<ArgumentNullException>(() => manager.SetBlobAlias(null!));
        Assert.Throws<ArgumentException>(() => manager.SetBlobAlias(""));
        Assert.Throws<ArgumentException>(() => manager.SetBlobAlias("   "));
    }

    [Fact]
    public void ClearCosmosAlias_ClearsAlias()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123").WithContext(new SessionContext("my-cosmos", "my-blob")));

        manager.ClearCosmosAlias();

        Assert.Null(manager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("my-blob", manager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public void ClearCosmosAlias_WhenNoSession_Throws()
    {
        var manager = new SessionStateManager();

        Assert.Throws<InvalidOperationException>(() => manager.ClearCosmosAlias());
    }

    [Fact]
    public void ClearBlobAlias_ClearsAlias()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123").WithContext(new SessionContext("my-cosmos", "my-blob")));

        manager.ClearBlobAlias();

        Assert.Equal("my-cosmos", manager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Null(manager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public void ClearBlobAlias_WhenNoSession_Throws()
    {
        var manager = new SessionStateManager();

        Assert.Throws<InvalidOperationException>(() => manager.ClearBlobAlias());
    }

    [Fact]
    public void ClearAllAliases_ClearsBothAliases()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123").WithContext(new SessionContext("my-cosmos", "my-blob")));

        manager.ClearAllAliases();

        Assert.Null(manager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Null(manager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public void ClearAllAliases_WhenNoSession_Throws()
    {
        var manager = new SessionStateManager();

        Assert.Throws<InvalidOperationException>(() => manager.ClearAllAliases());
    }

    [Fact]
    public void ClearAllAliases_SetsEmptyContext()
    {
        var manager = new SessionStateManager();
        manager.ActivateSession(Session.Create("TEST-123").WithContext(new SessionContext("my-cosmos", "my-blob")));

        manager.ClearAllAliases();

        Assert.NotNull(manager.CurrentSession?.ActiveContext);
        Assert.Equal(SessionContext.Empty, manager.CurrentSession?.ActiveContext);
    }
}
