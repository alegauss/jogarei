# Improvements

## Block A — Core

## Block B — Native interop

### §PP93 Three answers to one question

This client carries three different touchpad extents. streamsession.cpp picks per
console - 1920x942 for a PS4, 1919x1079 for a PS5 - which are the real dimensions of a
DualShock 4 and a DualSense pad. controllermanager.h defines PS_TOUCHPAD_MAXX 1920 and
PS_TOUCHPAD_MAXY 1079, which is each axis's larger value and therefore neither pad, and
the dpad-touch path and the SDL touchpad path both use it whichever console is
connected.

On a PS4, holding dpad-down walks the finger to y=1079 on a pad that ends at 942, and
dpad-right reaches x=1920 on a PS5 pad that ends at 1919. The error is always outward,
so the gesture keeps working and stops near the edge rather than at it.

Whether that is worth changing is genuinely open, which is why this is an idea and not a
task. The console may well clamp what it is sent, in which case the only cost is that
the last increment of travel does nothing. Nobody has measured it, and measuring it
needs a PS4 as well as a PS5.

What is not open is that one client should not hold three answers to one question.
Whatever the right pair is, the dpad path and the mouse path should read it from the
same place - and the reason to write that down now is that the port has just copied all
three, so it is the moment when the duplication is visible.

### §PP851 The half of the haptics a DualSense can hear

PushHapticsFrame has two arms and PP846 ported one: the fold to a single rumble
strength, which the client uses when haptics_output is 0. Below that early return is the
other arm, and it is the one a DualSense is for.

WHAT IT DOES. The frame is remixed to four channels, resampled, and written to an audio
device that IS the pad. The actuators are wide-band, so what arrives is a waveform
rather than a strength - exactly what the fold throws away.

SO THE PORT'S PS5 PATH IS THE PS4 PATH. PP846 sends a DualSense the 47-byte effects
report, which drives the same two rumble motors a DualShock 4 has.

WHAT THIS NEEDS. An output device rather than a controller handle, chosen by name; a
3000-to-48000 resample; and a two-to-four remix whose layout is [0, 0, L, R] - the
actuators are the last two - with a saturating override the client skips near 1.

THE PREMISE IS MEASURED AND IS NOT WRITTEN HERE. A pad was attached, the endpoint
exists, and the path depends on the cable - facts a ship of this line would delete with
the argument for doing it. They are in docs/HARDWARE-CONTRACT.md, under what this client
requires of a pad.

THE FIRST QUESTION IS SDL OR XAUDIO2. The client uses SDL audio; this port initialises
SDL for the game controller subsystem alone, and PP820's engine is its own. A non-goal
forbids hand-rolling what Windows ships, which cuts against the smaller change.

### §PP854 A pad that stops without anything failing

Clicking into another window and back stops the pad driving the console. Reproduced
twice in a row, once against a Windows notification and once against VS Code, and the
session goes on streaming - the picture keeps arriving and the audio keeps playing.

WHAT IT IS NOT, MEASURED RATHER THAN ARGUED. Instruments were added for the three
obvious candidates and all three came back clean in the run that reproduced it: `last
event 0,0s ago` says SDL's pump was still delivering at the instant the run ended; `0
departure(s) seen, 0 released` says the device was never re-enumerated; `holds a handle:
True` says nothing went stale. PadFeed pushed 14237 states and the sink was armed
throughout.

AND THE HINT IS SET. SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS is one of the four Gamepads
writes before SDL_Init, which is the exact defence against this, and the selftest
asserts it reads back.

SO THE LAYER THAT BREAKS IS BELOW THE PAD AND ABOVE THE WIRE - the same region PP853 is
about, and the two may be one thing. What separates them is that PP853's zero holds for
the whole run while this starts at a moment a person can name.

WHAT WOULD SETTLE IT is a count with a clock on it: state packets per second across the
focus change. The wire's total says 5608 for two minutes and cannot say whether they
stopped halfway.

## Block C — Video and input path

## Block D — Screens

### §PP859 The settings dialog nothing opens

PP167 composed the nine tabs into SettingsView, and nothing in app/ constructs one: no
window shows it, no menu or shortcut opens it, and no DataContext is set on it. Each
tab's view model has a constructor that reads an IPreferences, and no caller passes one
- the Controllers tab's, for instance, reads four of its rows and not the rumble
intensity or the haptic override beside them. Nothing writes a value back either.

SO EVERY SETTING THIS PORT READS IS ONE THE QT CLIENT WROTE. PP858 made a session read
the two haptics rows from the store; the only way to change them is a registry edit or
the retired client. The same is true of every row on all nine tabs.

READ, AND NOT DELIBERATE. No entry for PP16, PP140, PP167 or PP632 defers it; the front
door (ConsoleListView) has no way in. Three joins: the front door opens the dialog, each
tab is built from the store, and a save writes rows back in the store's declared words
and widths through QSettingsWriter. Two are the owner's calls: where it opens, and
whether the port writes the store the Qt client wrote - PP626 handled that store as real
registrations.

The census a later answer needs: nine tabs, each with an IPreferences constructor, none
of them called from app/ outside tests.

## Block E — Windows-only build

### §PP301 The first green run

PP22 put the native build on a runner and configured it the only way a runner can be
configured - MSVC with the vcpkg toolchain - and that toolchain has never compiled a
line of this tree. Everything anyone here has ever built came through MSYS2 MinGW64, so
the workflow's first push is also the first test of it.

What is likely to be found there, named rather than discovered one red push at a time:
lib/ and third-party/ carry GCC's picky warning set and `-Werror-implicit-function-
declaration`, which cl.exe does not accept; the vendored curl configures itself off the
compiler it detects; nanopb's generator wants a Python 3 that vcpkg does not install;
and libplacebo is found through pkgconf, which vcpkg lays down as a tool rather than on
PATH.

The assertions PP22 shipped cover the file - every path it names, the framework it
installs, the toolchain it configures through - and cover nothing about whether the
build succeeds. That is the honest boundary: only a runner answers that, and pretending
otherwise would put a second build system in a test.

So this is the first green run, and what it costs is whatever the four above turn out to
be. It is filed apart from PP22 because the workflow is worth having while it is red -
the alternative was leaving CI unwritten until someone had a runner to iterate against,
which is how a port keeps building on exactly one machine.

### §PP302 Distributing through the Microsoft Store

Rewritten after the portfolio review of 30/10/2026 (visibility,
`estrategia/portfolio.md`). What an unsigned application costs its user is unchanged:
SmartScreen shows "Windows protected your PC" on the first run, browsers warn on the
download, and the warning is the first thing a new user sees. What changed is the
answer. The decision for all of the author's Windows apps is the Microsoft Store, which
signs the MSIX package itself, in place of buying a certificate (OV, EV or Azure's
service). claude-tray tests the path first (visibility VS58); this line follows what
that test learns.

This port's own points to check against the Store's rules: no Sony mark in the name or
the listing (PP861, PP863), since a store review refuses third-party trademarks;
AGPL-3.0, whose source offer the listing must carry; the PSN sign-in through WebView2;
discovery and streaming over the local network, which need the private-network
capabilities declared; the NVIDIA path and the decode fallbacks, which must survive
packaging; and the side-by-side install with chiaki-ng. If one of these rules out MSIX,
the outcome is written down and a certificate is reconsidered.

Done when the application is listed in the Store and installs without a SmartScreen
warning, or the reason it cannot be is recorded here.

### §PP871 Spike readings become fixtures

PP866's audit found eight spikes and gate\alloc-budget still in the tree, each held by a
reader: EchoCancellation and OverlayDraw read the release JSON of spike\audio-effects
and overlay-draw; SpikeRunQuality reads decode-path, opus-decode, video-hdr and
video-upscale; ComSignaturesTests asserts decoder-choice and mic-capture declare COM
signatures; other spikes compile present-path's Stats.cs. gate\alloc-budget is held by
nothing but its README, whose proof method and the -fno-builtin-malloc flag the C
counter needs are recorded nowhere else.

Copy each reading into a fixture beside the test that reads it, retire the COM assertion
or point it at app\, record alloc-budget's README in the test that already asserts its
numbers, and then delete spike\ and gate\ with the .slnx comment that names them. A
spike is a decision already taken; its evidence belongs beside its reader, not in the
experiment.

### §PP872 The reference build leaves after its last row

PP63 left a reference build in the tree to produce PP46's before:
scripts\reference-build-msvc.ps1, scripts\windows-vc\gf-complete.patch (which the script
names, though it reads the copy inside its own worktree), and the build\reference
worktree it creates. PP864 still needs it, for the cold row of the Qt build.

Once PP864's rows are recorded, the before is measured and nothing will rebuild it:
delete the script and the patch, drop ReferenceBuildTests' script checks while keeping
the assertion on the measured row, prune the worktree, and leave the rows and the README
table as the record.

## Block F — Managed core

### §PP107 The two that were said to be uncalled

chiaki_reorder_queue_drop and chiaki_reorder_queue_peek are both broken. PP562: the
suite calls both and pins the drop, contrary to this.

drop announces the element to the drop callback and then does not remove it. It never
clears entry->set, so the element stays peekable and pullable - and its own
count-reduction loop, `while(!entry->set)`, cannot run for the same reason. peek writes
through its seq_num pointer unconditionally, and takion.c passes NULL for it. Read, not
run: running it is the crash.

Both are on one path: when crypt becomes available, takion re-checks the MACs of
everything already queued and drops what fails. There, peek cannot survive a set entry,
and a rejected packet is delivered anyway. The path needs a non-empty data queue when
crypt initialises, which is presumably why it has survived.

Decided: accepted. Not patched, because every drift check in this port asserts that the
managed side matches lib/, and a local patch would leave them asserting agreement with a
libchiaki nobody else runs. Reporting upstream stays open and is not this project's to
send.

What that cost was a reason held in prose, and prose does not go red. So
ReorderQueueSource holds five facts about the two and their caller: drop clears no set
flag, its count loop is guarded by the return above it, peek writes both out-pointers
where pull guards its own, and takion still passes NULL and still drops on a bad MAC.
Repair any upstream and the port's copy becomes the divergence, on the next run.

### §PP853 The half of the pad the console never hears

Chiaki's feedback path is two packet types and the port sends one of them. The STATE
packet carries the sticks, the triggers and the orientation; the HISTORY packet carries
button presses and releases, which is why PP717 wrote a recorder and PP723 composed it
into a sender.

MEASURED, ON A CONSOLE. A two-minute session against PS5-385 with a DualSense reported
`feedback wire: 5608 sent, last Success, 0 history packet(s) offered`. The count of
state packets rises the whole run and never errors; the history count never leaves zero,
through gameplay with buttons being pressed.

SO THE SYMPTOM IS NOT "THE PAD DOES NOT WORK". Sticks move a character and buttons do
nothing, which is what a person reports as the controller half-working and what sent
this session looking at SDL, at the pump and at the device handle - all of which are
fine. PadFeed pushes 14237 states, the pump delivers to the last instant, and the wire
accepts everything it is given.

WHERE IT COULD BE. HistoriesOffered counts in TakionFeedbackSink.Put, so zero means no
history packet was ever handed to it: either FeedbackHistoryRecorder.Record produces no
events for a real button change, or the sender's flush never offers what it recorded.
Both were read and both are right: an SDL press driven through translation, sink and a
running sender queues and offers history offline. The live run's new counters name the
stage.

The instrument that found it is in the run now, so a fix has a number to move.

### §PP867 Drift checks on fixtures, then gui goes

PP866's audit found gui\ (101 files, the Qt client PP632 retired) still in the tree for
one reason: about 32 drift checks read its source as an oracle (Preferences,
InputTranslation, SessionLogSanitizer and others). Nothing builds it.

The way out is the one PP33 took for json-c: run each check once against gui\, record
what it answered as a fixture beside the test, point the check at the fixture, and then
delete gui\. With it go cmake\FindSDL2.cmake and FindSpeexDSP.cmake, the
CHIAKI_ENABLE_GUI branch of the root CMakeLists.txt, and the option itself; and
third-party\cpp-steam-tools, if the line that gates it has not already removed it.

A check whose oracle has to stay live because upstream still changes gui\ is the
exception, and says so in its own summary rather than keeping the folder by default.

### §PP868 The C and everything behind it leave together

PP866's audit classed lib\ as removable only after the port: it is still compiled into
chiaki-shim.dll, which Jogarei.exe loads, and the drift checks read it as the oracle the
managed code is held against.

Behind it stands everything that exists only to build it: the CMake graph and its cmake\
modules, the MSYS2 MinGW64 toolchain compile.cmd drives, test\ and its munit submodule
(the C suite with its floor of 71 cases), third-party\nanopb, and shim\ itself. They go
together, in one commit, when the shim's last export is ported and no check needs the C
as an oracle, which is the same moment compile.cmd stops needing MSYS2.

Set aside until then: nothing here can start while the shim exports anything.

## Block G — Test discipline

## Block H — Performance and telemetry

### §PP55 The instrument outside the process

PP40 shipped the half that a regression test can use: input queueing, the console's
reported round trip and the decode-to-present handoff, summed into a floor. What it
cannot do is say whether that floor tracks the real click-to-photon delay, because every
term it is missing - the console's input handling, the game's render, the encoder, the
display's own pipeline - lives outside this process.

Reflex Latency Analyzer measures click to photon on a monitor that supports it, without
a camera rig. It does not apply to this client as a low latency mode: Reflex controls a
render queue this application does not have. As a measuring device it answers exactly
the question, and it is the reason this line is filed rather than folded into PP40.

The hardware is what blocks it: the development machine has an NVIDIA card but no
Reflex-capable monitor, so no number can be taken today. Taken later against a converted
tree, it measures the port instead of the client - the same window that closes on PP39
closes on this.

### §PP822 The instrument PP53's criterion describes

PP53 asks whether a frame arrives unpaced against a fixed refresh, and its own criterion
refuses the easy answer: a frame-time trace on a varying panel, not a flag DXGI
accepted. PP163 is this tree's record of what an API's return value is worth as a
prediction about a pixel.

IDXGISwapChain::GetFrameStatistics is the instrument that criterion describes. It
reports PresentCount, PresentRefreshCount, SyncRefreshCount and SyncQPCTime - the
composition's own view of when a present actually reached a vblank, on the same clock
QueryPerformanceCounter reads. That is a measurement of what happened rather than of
what was asked for, which is the distinction PP53 is built on.

WHY THIS IS AN IDEA AND NOT A TASK. Nothing in the port presents through a swapchain of
its own yet. PP49 and PP53 both name that as their blocker in the same words - the video
plane owning its own swapchain - and frame statistics come off a swapchain. So this
waits on what they wait on, and is written down now so the instrument is not invented a
third time when the plane exists.

It also needs no variable-refresh display, which PP53 does. On a fixed panel it still
says whether a present met its vblank or missed one, which is the half of PP53's
question that hardware is not gating.

### §PP864 Two rows taken after a reboot

PP46 recorded the before and the after on 2026-11-20, both with --cache-state warm: the
installer, the tree and the idle working set, which no cache state moves. The start-up
time is in both rows too, and it is a warm start wearing the cold label, because both
executables had run on that boot. PP61 is why the harness says so instead of hiding it.

What is left is one row per build taken cold. After a reboot, and before either
executable has run:

    scripts\reference-build-msvc.ps1 -CacheState cold-boot

for the before, which ends by measuring and writes its row under build\reference, then
measure-startup --cache-state cold-boot on build\chiaki-ng-package\ChiakiNg.exe for the
after. The order matters: one reboot covers both rows only if the first launch of each
executable is the measured one. A second reboot between them is equally valid.

Both rows replace the warm ones in tools\measure-startup, the table in its README gains
the cold figures, and StartupBeforeAfterTests then asserts the two declare cold-boot.
That is the assertion this line ships with.

A session cannot reboot the machine it runs on, and dropping the standby list instead
(--cache-state dropped) needs elevation the agent sessions here do not have. So this
requires a-cold-boot.

## Block I — NVIDIA path

### §PP47 The right NVIDIA feature, waiting on a switch

The shipped half: DLSS cannot apply here, RTX Video Super Resolution is the candidate,
and the floor is measured. The plain upscale from 1080p NV12 to 4K costs 262.9us mean
and 274.1us p99 on the RTX 4060 - 1.6% of a frame at 60fps. Whatever VSR costs, it costs
that plus something.

What is left is VSR's own number. The spike in spike/video-upscale sets the stream
extension and nothing changes: 0 of 8.3 million pixels differ, while nvsvsr.dll and
nvvitvsr.dll sit in the driver store, so the feature is installed and unreached.

Three candidates were filed and two are now dropped. The GUID is mpv's, corroborated
across three independent retrievals. Offscreen output is not disqualifying: mpv's own
filter writes to an ordinary texture and works.

What survives is the driver's own switch, and mpv documents it: the option "only enables
the appropriate processing extensions; whether it actually works depends on your
hardware and the settings in your GPU driver's control panel". The remaining step is a
human one: NVIDIA Control Panel, Video, RTX Video Enhancement, then re-run.

That is a finding rather than a defect, and it belongs to PP51 as much as here: a vendor
path needing a control panel visit has a different contract from one that does not, and
a user who never opens that panel gets the unaccelerated path silently.

The quality half stays unanswerable here regardless. It needs a real decoded frame,
which needs a console, so the synthetic pattern settles cost and never benefit.

### §PP49 HDR on a stream that does not carry it

The window already deals with HDR when the stream is HDR. The case this covers is the
other one: an SDR stream on a display capable of more, which is most sessions on most
titles.

The cost half is done and it came back cheap. RTX Video HDR engages on this card and
costs 29.0us a frame, 0.17% of a 60fps interval - see spike/video-hdr, and the ledger
for the number. So cost is not what decides this feature, which is worth saying because
it is the question this line was filed to answer.

A sentence that stood here is now false, and its correction is why the spike was written
carefully. This said RTX Video HDR "runs on the same NGX surface as the upscaler". It
does not: super resolution is the NVIDIA PPE interface at method 2 and true HDR is an
interface of its own at method 3, so a spike inheriting PP47's constant would have set
an extension the driver knows, been accepted, and reported PP47's finding as news.

What is left is the half a number cannot reach. An inferred HDR image is an opinion
about colour the source did not express, and on some content it looks worse - so the
picture has to be judged on a decoded console frame rather than on the synthetic chart
the cost was taken from, and whatever ships is a setting that turns off with a fidelity
mode bypassing it. Both are criteria on the line now.

### §PP53 The one that removes waiting instead of adding work

Nothing in the window mentions VRR, G-SYNC or adaptive sync. Frames from a console
arrive when the network delivers them - irregularly by nature - and a fixed refresh
present rounds every one of them up to the next vblank. At 60Hz that is up to 16ms of
pure waiting, added to a frame that already travelled a network.

Variable refresh is the direct answer, and it is the only item in this block that makes
the picture arrive earlier rather than look better or arrive smoother.

The first thing measured was whether PP319's choice had already cost it. It has not, at
either depth. DXGI takes DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING on a composition swapchain
and presents with it, and PP646 asked again through a committed tree - the swapchain as
a visual's content on a real window - where it survives too. Both refuse that present
where the flag was not asked for, so the flags are read. A sentence here is corrected by
that: it said exclusive fullscreen is the usual precondition, and the tearing pair is
what replaced needing one.

What is left is the half an API cannot answer. A composed frame goes through DWM, so a
flag DXGI accepted is not a panel that followed - which is the mistake PP163 made one
subsystem along, and there is no display here that varies its refresh to check it on.
Below the display's minimum, low framerate compensation changes the behaviour again, and
that too is read rather than assumed.

## Block J — Public documentation

### §PP873 The repository's own community files

PP866's audit found chiaki-ng's own files still published by GitHub from this
repository: .github\funding.yml (buy_me_a_coffee: streetpea, upstream's author), the
issue templates written for chiaki-ng, .github\copilot-instructions.md describing the Qt
client, and CONTRIBUTOR_GUIDE.md in chiaki-ng's words, which the site's Contributing
link points at.

Each is what a visitor to alegauss/jogarei reads about how to fund, report and
contribute, and each describes another project. funding.yml goes, since streetpea is not
this project's author; the templates and the guide are rewritten for Jogarei (Windows
only, no release yet, issues here); the Copilot instructions are rewritten for the WPF
host or deleted. The credit to chiaki-ng stays in the README.

## Block K — The site on the design system
