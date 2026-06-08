namespace Spx.Web.E2ETests;

[TestFixture]
public class NexusGameplayPanelTests : PageTest
{
    private PlaygroundFixture _fixture = null!;

    [SetUp]
    public async Task Setup()
    {
        _fixture = new PlaygroundFixture();
        await _fixture.StartAsync();

        await Page.GotoAsync($"{_fixture.BaseUrl}/stories/nexus/components/gameplay-panel");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _fixture.DisposeAsync();
    }

    [Test]
    public async Task Submit_orders_button_is_present()
    {
        await Expect(Page.Locator("[data-testid='nexus-submit-orders']")).ToBeVisibleAsync();
    }
}
