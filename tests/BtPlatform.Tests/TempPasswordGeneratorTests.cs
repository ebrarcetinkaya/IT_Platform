using BtPlatform.Core.Security;
using Xunit;

namespace BtPlatform.Tests;

public class TempPasswordGeneratorTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(20)]
    public void Generate_HasRequestedLength(int length)
    {
        Assert.Equal(length, TempPasswordGenerator.Generate(length).Length);
    }

    [Fact]
    public void Generate_AlwaysContainsAllCharacterClasses()
    {
        for (var i = 0; i < 200; i++)
        {
            var p = TempPasswordGenerator.Generate();
            Assert.Contains(p, ch => char.IsUpper(ch));
            Assert.Contains(p, ch => char.IsLower(ch));
            Assert.Contains(p, ch => char.IsDigit(ch));
            Assert.Contains(p, ch => "!@#$%*?".Contains(ch));
        }
    }

    [Fact]
    public void Generate_NeverContainsAmbiguousCharacters()
    {
        for (var i = 0; i < 500; i++)
        {
            var p = TempPasswordGenerator.Generate();
            Assert.DoesNotContain(p, ch => "0O1lI".Contains(ch));
        }
    }

    [Fact]
    public void Generate_ProducesDifferentPasswords()
    {
        var set = new HashSet<string>();
        for (var i = 0; i < 100; i++) set.Add(TempPasswordGenerator.Generate());
        Assert.Equal(100, set.Count);
    }

    [Fact]
    public void Generate_TooShort_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TempPasswordGenerator.Generate(7));
    }
}