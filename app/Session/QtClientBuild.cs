namespace Jogarei.Session;

/// <summary>
/// PP598: the Qt client's build is three pieces, and they retire together or not at all.
///
/// THE DECISION, taken 2026-08-31 and recorded here because the commit that acts on it is not this
/// one. PP597 established that PP33's deletion and a buildable Qt client are mutually exclusive -
/// gui/ calls eleven holepunch exports directly, across streamsession.cpp and qmlbackend.cpp, so
/// the file PP33 removes is one gui/ links against. The choice was between letting the client go
/// and holding PP33 back; it is the first. gui/ stays as source and stops being a build target.
///
/// IT HAS HAPPENED. PP632 took the argument away in the same commit that stopped session.c asking,
/// which is what "rides in PP33's own commit" meant. What this class held was the shape of the
/// decision until then; what it holds now is the shape of the retirement, so a `gui` argument
/// arriving back is caught rather than welcomed - the same turn PP591 gave the harness it deleted.
///
/// The affordance was real until that commit: configure resolved, WebEngine was optional in
/// gui/CMakeLists.txt, and `compile.cmd gui` linked gui\chiaki.exe in thirteen steps. It stopped
/// working the instant the field went, because gui/ calls eleven holepunch exports directly - which
/// is why the two could never have been separate commits.
///
/// PP623 SAYS WHICH COMMIT THAT IS. "PP33's own commit" was reasoned about a change understood as
/// nine calls plus this wiring; PP621 measured the oracle in the same transaction. The retirement
/// still rides with the deletion and nothing above is withdrawn - what changed is that the deletion
/// is three commits, and this is the middle one. <see cref="HolepunchDeletionOrder"/> is where that
/// order lives, and it names the pieces below through this type rather than copying them.
///
/// THREE PIECES, AND THE HALF-DONE RETIREMENT WAS THE EXPENSIVE ONE. `compile.cmd`'s `gui` argument
/// set CHIAKI_ENABLE_GUI=ON; GuiFreshness compared the built client against gui/ and failed on
/// Stale; <see cref="ClientRelativePath"/> is the binary both meant. Removing the argument and
/// leaving the check would have left every checkout that ever built a client permanently red with
/// nothing able to clear it - so PP632 took the argument AND gave the check a Retired state, which
/// is what PP597 asked for by name.
///
/// PP867 FINISHED IT. gui/ is deleted, and with it the CHIAKI_ENABLE_GUI option and GuiFreshness -
/// a freshness check over sources that no longer exist has nothing to compare. The files the drift
/// checks read were recorded under tests\oracles\gui first. What stays here is the record of the
/// retirement and the one guard no compiler gives: compile.cmd must not offer the argument again,
/// because it would now set an option CMakeLists.txt does not declare.
/// </summary>
public static class QtClientBuild
{
    /// <summary>The argument that turns the client on, as compile.cmd spells it.</summary>
    public const string CompileArgument = "gui";

    /// <summary>What that argument sets, which is the line that makes the affordance real.</summary>
    public const string EnableFlag = "CHIAKI_ENABLE_GUI=ON";

    /// <summary>The gate that offers it.</summary>
    public const string CompileRelativePath = "compile.cmd";

    /// <summary>
    /// The binary the argument produced. PP867: held here now that GuiFreshness, which used to own
    /// it, is deleted - <see cref="Settings.StoreOwnership"/> still derives the process name it looks
    /// for from it, because a checkout that built the client before PP632 may still run that binary.
    ///
    /// A property and not a const on purpose: build\gui\chiaki.exe exists only where somebody built
    /// it once, and a const would put it in DriftCorpus, which reports a declared path that is
    /// missing.
    /// </summary>
    public static string ClientRelativePath { get; } = @"build\gui\chiaki.exe";

    /// <summary>compile.cmd, or null outside a checkout.</summary>
    public static string? LocateCompile() => SanitizerSource.LocateRelative(CompileRelativePath);

    /// <summary>
    /// PP599: the file that drives the C session's PSN path, which is the client this retires.
    ///
    /// The two facts that were separate until decision A joined them. PP596 established that the Qt
    /// client is the ONLY thing that puts a holepunch handle into a ChiakiSession, so it is the only
    /// thing that can enter session.c's PSN path at all. This record says that same client stops
    /// being built. Together they say nobody will ask session.c for PSN once the retirement lands.
    ///
    /// WHY IT MATTERS TO PP33'S PLAN. §PP533 settled the direction as a CONVERSION - session.c stops
    /// taking a holepunch handle and starts taking the five results it currently derives, four of
    /// them durable and the registration info scoped to its own block (PP551). That design assumed
    /// session.c must keep doing PSN for somebody. After A it does not: the managed flow owns the
    /// PSN sequence (PP340), the shim never passes a handle (PP592), and the Qt client is going. So
    /// the nine asks are REMOVED, and the five-result plumbing is work that no longer has a caller.
    ///
    /// This is not a contradiction of PP533 - it is its premise expiring, which is a thing a settled
    /// design is allowed to do and a thing nobody notices unless it is written down.
    /// </summary>
    public static string PsnDriverRelativePath => Protocol.HolepunchSessionOwnership.QtClientRelativePath;

    /// <summary>
    /// Whether compile.cmd still offers the argument that builds the client.
    ///
    /// Read as the assignment rather than as the word. `gui` appears in that file's comments and in
    /// its usage line, and PP587's finding one file over was that a banner naming a command is not
    /// the command - so what is looked for is the line that sets the flag, with rem lines skipped
    /// because compile.cmd's own comments quote it while explaining PP529.
    /// </summary>
    public static bool CompileStillBuildsTheClient(string compileCmd)
    {
        ArgumentNullException.ThrowIfNull(compileCmd);

        foreach (string line in compileCmd.ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("rem ", StringComparison.OrdinalIgnoreCase))
                continue;

            if (trimmed.Contains(EnableFlag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
