using FluentAssertions;
using SnipLink.Domain;

namespace SnipLink.UnitTests;

public class Base62Tests
{
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(32)]
    public void Generate_ProducesRequestedLength(int length)
    {
        Base62.Generate(length).Should().HaveLength(length);
    }

    [Fact]
    public void Generate_UsesOnlyBase62Characters()
    {
        var code = Base62.Generate(200);
        code.Should().MatchRegex("^[0-9a-zA-Z]+$");
    }

    [Fact]
    public void Generate_DefaultLengthIsSix()
    {
        Base62.Generate().Should().HaveLength(Base62.DefaultLength);
    }

    [Fact]
    public void Generate_IsHighlyUnlikelyToCollide()
    {
        var codes = Enumerable.Range(0, 1000).Select(_ => Base62.Generate()).ToList();
        codes.Distinct().Should().HaveCount(codes.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Generate_RejectsNonPositiveLength(int length)
    {
        var act = () => Base62.Generate(length);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
