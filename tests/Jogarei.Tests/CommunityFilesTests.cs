using Jogarei.Session;
using Xunit;

namespace Jogarei.Tests;

/// <summary>
/// PP873: what GitHub shows a visitor about funding, reporting and contributing is this project's.
///
/// Until 2026-11-20 the repository published chiaki-ng's: a funding link to its author, issue
/// templates asking for a Steam Deck and a chiaki-ng version, a contributor guide about the Qt
/// client and a Flatpak dev environment, and Copilot notes describing the same client.
/// </summary>
public class CommunityFilesTests
{
    /// <summary>PP873: the upstream author's funding link and the Qt client's Copilot notes are gone.</summary>
    [Fact]
    public void TheUpstreamsFundingAndCopilotNotesAreGone()
    {
        if (SanitizerSource.RepositoryRoot() is not { } root)
            return;

        Assert.False(File.Exists(Path.Combine(root, ".github", "funding.yml")));
        Assert.False(File.Exists(Path.Combine(root, ".github", "copilot-instructions.md")));
    }

    /// <summary>PP873: the issue templates and the guide describe Jogarei, not the upstream or its platforms.</summary>
    [Theory]
    [InlineData(@".github\ISSUE_TEMPLATE\bug_report.md")]
    [InlineData(@".github\ISSUE_TEMPLATE\feature_request.md")]
    [InlineData("CONTRIBUTOR_GUIDE.md")]
    public void TheCommunityFilesAreThisProjects(string relative)
    {
        if (SanitizerSource.LocateRelative(relative) is not { } path)
            return;

        string text = File.ReadAllText(path);

        Assert.Contains("Jogarei", text);
        Assert.DoesNotContain("Steam Deck", text);
        Assert.DoesNotContain("Flatpak", text);
        Assert.DoesNotContain("chiaki-ng Version", text);
        Assert.DoesNotContain("qmlmainwindow", text);
    }
}
