using abbasmhd.PiiMasking;

namespace abbasmhd.PiiMasking.Tests;

public class PiiStringMaskingTests
{
    private static readonly string[] OnBehalfOfSeparator = [" on behalf of "];

    [Fact]
    public void MaskSegment_Null_ReturnsNull()
    {
        Assert.Null(PiiStringMasking.MaskSegment(null));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("a", "a****")]
    [InlineData("ab", "ab****")]
    [InlineData("samson", "Sa****")]
    public void MaskSegment_MasksAfterTwoCharacters(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskSegment(input));
    }

    [Theory]
    [InlineData("abc", "Ab****")]
    [InlineData("ABC", "Ab****")]
    [InlineData("Jo", "Jo****")]
    [InlineData("A", "A****")]
    [InlineData("XY", "XY****")]
    public void MaskSegment_ExactLengths_ProducesExpectedShape(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskSegment(input));
    }

    [Theory]
    [InlineData("  samson  ", "Sa****")]
    [InlineData("\tsamson\n", "Sa****")]
    public void MaskSegment_TrimsLeadingAndTrailingWhitespace(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskSegment(input));
    }

    [Fact]
    public void MaskSegment_WhitespaceOnly_YieldsFixedMaskOnly()
    {
        Assert.Equal("****", PiiStringMasking.MaskSegment("   "));
        Assert.Equal("****", PiiStringMasking.MaskSegment("\t \t"));
    }

    [Fact]
    public void MaskSegment_MixedCaseLongerString_NormalizesFirstTwoCharacters()
    {
        Assert.Equal("Sa****", PiiStringMasking.MaskSegment("SAMSON"));
        Assert.Equal("Jo****", PiiStringMasking.MaskSegment("John"));
    }

    [Fact]
    public void MaskSegment_Unicode_UsesInvariantCasingOnFirstTwoScalars()
    {
        Assert.Equal("Jo****", PiiStringMasking.MaskSegment("José"));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("xy")]
    [InlineData("xyz")]
    [InlineData("verylongname")]
    public void MaskSegment_AlwaysEndsWithExactlyFourAsterisks(string input)
    {
        var result = PiiStringMasking.MaskSegment(input)!;
        Assert.EndsWith("****", result);
        Assert.Equal(4, result.Count(c => c == '*'));
    }

    [Fact]
    public void MaskEmail_MasksLocalOnly_DomainUnchanged()
    {
        var result = PiiStringMasking.MaskEmail("samson.user@mail.example.com");
        Assert.Equal("Sa****@mail.example.com", result);
    }

    [Fact]
    public void MaskEmail_WithoutAt_FallsBackToSegment()
    {
        Assert.Equal("Sa****", PiiStringMasking.MaskEmail("samson"));
    }

    [Fact]
    public void MaskEmail_ShortLocal_DomainUnchanged()
    {
        Assert.Equal("ab****@c.d", PiiStringMasking.MaskEmail("ab@c.d"));
    }

    [Fact]
    public void MaskEmail_Null_ReturnsNull()
    {
        Assert.Null(PiiStringMasking.MaskEmail(null));
    }

    [Fact]
    public void MaskEmail_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, PiiStringMasking.MaskEmail(string.Empty));
    }

    [Fact]
    public void MaskEmail_TrimsOuterWhitespace()
    {
        Assert.Equal("Sa****@contoso.com", PiiStringMasking.MaskEmail("  samson@contoso.com  "));
    }

    [Fact]
    public void MaskEmail_LocalEmpty_PreservesDomain()
    {
        Assert.Equal("@contoso.com", PiiStringMasking.MaskEmail("@contoso.com"));
    }

    [Fact]
    public void MaskEmail_DomainEmpty_StillAppendsAt()
    {
        Assert.Equal("Us****@", PiiStringMasking.MaskEmail("user@"));
    }

    [Fact]
    public void MaskEmail_FirstAtSplitsRestIsDomainLiteral()
    {
        Assert.Equal("a****@b@c.org", PiiStringMasking.MaskEmail("a@b@c.org"));
    }

    [Fact]
    public void MaskEmail_SubdomainAndPortStyleDomain_UnchangedAfterAt()
    {
        Assert.Equal(
            "Jo****@app.mail.contoso.com:443",
            PiiStringMasking.MaskEmail("joe@app.mail.contoso.com:443"));
    }

    [Fact]
    public void MaskEmail_LocalUsesSegmentRules_IncludingShortLocal()
    {
        Assert.Equal("a****@x", PiiStringMasking.MaskEmail("a@x"));
        Assert.Equal("Ab****@y", PiiStringMasking.MaskEmail("Ab@y"));
    }

    [Theory]
    [InlineData("Sa****", "Sa****")]
    [InlineData("  Jo****  ", "Jo****")]
    [InlineData("x****y", "x****y")]
    public void MaskSegment_WhenAlreadyContainsFourAsterisks_ReturnsTrimmedUnchanged(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskSegment(input));
    }

    [Fact]
    public void MaskEmail_WhenAlreadyContainsFourAsterisks_ReturnsTrimmedUnchanged()
    {
        const string already = "Jo****@contoso.com";
        Assert.Equal(already, PiiStringMasking.MaskEmail($"  {already}  "));
    }

    [Fact]
    public void MaskEmail_WhenDomainContainsFourAsterisks_ReturnsWholeAddressUnchanged()
    {
        const string value = "user@pa****.example.com";
        Assert.Equal(value, PiiStringMasking.MaskEmail(value));
    }

    [Fact]
    public void MaskEachWord_Null_ReturnsNull()
    {
        Assert.Null(PiiStringMasking.MaskEachWord(null));
    }

    [Theory]
    [InlineData("Abe David Smith", "Ab**** Da**** Sm****")]
    [InlineData("  John  Doe  ", "Jo**** Do****")]
    [InlineData("single", "Si****")]
    [InlineData("a b", "a**** b****")]
    public void MaskEachWord_MasksEveryWhitespaceSeparatedToken(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskEachWord(input));
    }

    [Fact]
    public void MaskEachWordRespectingLiterals_Null_ReturnsNull()
    {
        Assert.Null(PiiStringMasking.MaskEachWordRespectingLiterals(null, OnBehalfOfSeparator));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MaskEachWordRespectingLiterals_EmptyOrWhitespaceOnly_ReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, PiiStringMasking.MaskEachWordRespectingLiterals(input, OnBehalfOfSeparator));
    }

    [Theory]
    [InlineData("John Doe on behalf of Jane Smith", "Jo**** Do**** on behalf of Ja**** Sm****")]
    [InlineData("John Doe on behalf of Jane", "Jo**** Do**** on behalf of Ja****")]
    [InlineData("A on behalf of B", "A**** on behalf of B****")]
    [InlineData("SingleToken on behalf of Other", "Si**** on behalf of Ot****")]
    public void MaskEachWordRespectingLiterals_LeavesConfiguredLiteralPlain(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskEachWordRespectingLiterals(input, OnBehalfOfSeparator));
    }

    [Theory]
    [InlineData("John Doe ON BEHALF OF Jane Smith", "Jo**** Do**** ON BEHALF OF Ja**** Sm****")]
    [InlineData("john doe On BeHaLf Of jane", "Jo**** Do**** On BeHaLf Of Ja****")]
    public void MaskEachWordRespectingLiterals_CaseInsensitiveMatch_PreservesSourceCasingOfLiteral(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskEachWordRespectingLiterals(input, OnBehalfOfSeparator));
    }

    [Theory]
    [InlineData("John Doe On Behalf Of Jane Smith", "Jo**** Do**** On**** Be**** Of**** Ja**** Sm****")]
    [InlineData("John Doe", "Jo**** Do****")]
    public void MaskEachWordRespectingLiterals_NoConfiguredSeparators_FallsBackToMaskEachWord(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskEachWordRespectingLiterals(input, []));
        Assert.Equal(expected, PiiStringMasking.MaskEachWordRespectingLiterals(input, null));
    }

    [Theory]
    [InlineData("John Doe", "Jo**** Do****")]
    [InlineData("Acme Corp", "Ac**** Co****")]
    [InlineData("NoLiteralHere", "No****")]
    public void MaskEachWordRespectingLiterals_NoMatchInValue_FallsBackToMaskEachWord(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskEachWordRespectingLiterals(input, OnBehalfOfSeparator));
    }

    [Fact]
    public void MaskEachWordRespectingLiterals_OnlyEmptySeparatorEntries_FallbackToMaskEachWord()
    {
        Assert.Equal(
            "Jo**** Do**** On**** Be**** Of**** Ja**** Sm****",
            PiiStringMasking.MaskEachWordRespectingLiterals("John Doe On Behalf Of Jane Smith", new[] { "", "" }));
    }

    [Theory]
    [InlineData("John Doe on behalf of Jane Smith", "##", "Jo## Do## on behalf of Ja## Sm##")]
    [InlineData("A on behalf of B", "xx", "Axx on behalf of Bxx")]
    public void MaskEachWordRespectingLiterals_CustomMaskSuffix(string input, string suffix, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskEachWordRespectingLiterals(input, OnBehalfOfSeparator, suffix));
    }

    [Fact]
    public void MaskEachWordRespectingLiterals_MultipleSeparators_SplitsInOrder()
    {
        var seps = new[] { " on behalf of ", " representing " };
        var result = PiiStringMasking.MaskEachWordRespectingLiterals(
            "Alice Bob on behalf of Carol Dan representing Eve Frank",
            seps);
        Assert.Equal(
            "Al**** Bo**** on behalf of Ca**** Da**** representing Ev**** Fr****",
            result);
    }

    [Fact]
    public void MaskEachWordRespectingLiterals_EarliestSeparatorWins_ThenContinues()
    {
        var seps = new[] { " bar ", " foo " };
        var result = PiiStringMasking.MaskEachWordRespectingLiterals("a foo x bar y", seps);
        Assert.Equal("a**** foo x**** bar y****", result);
    }

    [Fact]
    public void MaskEachWordRespectingLiterals_AtSameIndex_PrefersLongerSeparator()
    {
        var seps = new[] { " on ", " on behalf of " };
        var result = PiiStringMasking.MaskEachWordRespectingLiterals("A B on behalf of C D", seps);
        Assert.Equal("A**** B**** on behalf of C**** D****", result);
    }

    [Theory]
    [InlineData("Jo**** Do**** on behalf of Ja****", "Jo**** Do**** on behalf of Ja****")]
    [InlineData("  Jo**** Do****  ", "Jo**** Do****")]
    public void MaskEachWordRespectingLiterals_WhenAlreadyContainsMaskSuffix_ReturnsTrimmedUnchanged(string input, string expected)
    {
        Assert.Equal(expected, PiiStringMasking.MaskEachWordRespectingLiterals(input, OnBehalfOfSeparator));
    }

    [Fact]
    public void MaskEachWordRespectingLiterals_TwoConsecutiveRounds_OfSameLiteral()
    {
        var result = PiiStringMasking.MaskEachWordRespectingLiterals(
            "Alpha on behalf of Beta on behalf of Gamma",
            OnBehalfOfSeparator);
        Assert.Equal("Al**** on behalf of Be**** on behalf of Ga****", result);
    }

    [Fact]
    public void MaskEachWordRespectingLiterals_LeaveRemainderUnmasked_KeepsTextAfterLastLiteralPlain()
    {
        var seps = new[] { " on " };
        var masked = PiiStringMasking.MaskEachWordRespectingLiterals(
            "Preamble text on 01-Jan-2024 3:00 PM.",
            seps,
            maskSuffix: null,
            leaveRemainderUnmasked: true);
        Assert.Equal("Pr**** Te**** on 01-Jan-2024 3:00 PM.", masked);
    }
}
