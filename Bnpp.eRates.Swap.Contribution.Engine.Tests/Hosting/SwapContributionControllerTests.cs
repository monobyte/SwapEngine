using Bnpp.eRates.Contribution.Model.Common;
using Bnpp.eRates.Swap.Contribution.Engine.Hosting;
using Bnpp.eRates.Swap.Contribution.Engine.Tests._TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bnpp.eRates.Swap.Contribution.Engine.Tests.Hosting;

public class SwapContributionControllerTests
{
    private readonly Mock<ILogger<SwapContributionController<TestInstrument>>> _mockLogger;
    private readonly Mock<IInstrumentProvider<TestInstrument>> _mockProvider;
    private readonly SwapContributionController<TestInstrument> _controller;

    public SwapContributionControllerTests()
    {
        _mockLogger = new Mock<ILogger<SwapContributionController<TestInstrument>>>();
        _mockProvider = new Mock<IInstrumentProvider<TestInstrument>>();
        _controller = new SwapContributionController<TestInstrument>(_mockLogger.Object, _mockProvider.Object);
    }

    [Fact]
    public void GetInstruments_ReturnsOkResult()
    {
        _mockProvider.Setup(p => p.GetAllInstruments())
            .Returns(new List<TestInstrument>());

        var result = _controller.GetInstruments();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void GetInstruments_ReturnsAllInstrumentsFromProvider()
    {
        var instruments = new List<TestInstrument>
        {
            new() { Id = "1", Description = "EUR 1Y", Currency = "EUR", Key = "1" },
            new() { Id = "2", Description = "USD 5Y", Currency = "USD", Key = "2" },
            new() { Id = "3", Description = "GBP 10Y", Currency = "GBP", Key = "3" },
        };
        _mockProvider.Setup(p => p.GetAllInstruments()).Returns(instruments);

        var result = _controller.GetInstruments();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<TestInstrument>>(okResult.Value);
        Assert.Equal(3, returned.Count());
    }

    [Fact]
    public void GetInstruments_ReturnsEmptyList_WhenNoInstruments()
    {
        _mockProvider.Setup(p => p.GetAllInstruments())
            .Returns(Enumerable.Empty<TestInstrument>());

        var result = _controller.GetInstruments();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<TestInstrument>>(okResult.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public void GetInstruments_CallsProviderExactlyOnce()
    {
        _mockProvider.Setup(p => p.GetAllInstruments())
            .Returns(new List<TestInstrument>());

        _controller.GetInstruments();

        _mockProvider.Verify(p => p.GetAllInstruments(), Times.Once);
    }

    [Fact]
    public void GetInstruments_PreservesInstrumentData()
    {
        var instrument = new TestInstrument
        {
            Id = "42",
            Description = "BRL 2Y IMM",
            Currency = "BRL",
            MarketId = "LATAM",
            DateMaturity = 20260315,
            Key = "42"
        };
        _mockProvider.Setup(p => p.GetAllInstruments())
            .Returns(new[] { instrument });

        var result = _controller.GetInstruments();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<TestInstrument>>(okResult.Value).ToList();
        Assert.Single(returned);
        Assert.Equal("42", returned[0].Id);
        Assert.Equal("BRL 2Y IMM", returned[0].Description);
        Assert.Equal("BRL", returned[0].Currency);
        Assert.Equal("LATAM", returned[0].MarketId);
    }
}
