# What this client requires of the hardware

PP51. The direction of Block I is that NVIDIA is where the tuning goes — the decoder that gets
measured, the upscaler that gets integrated, the path that gets a number. This file is the other
half of that sentence, written down so it is a commitment rather than an intention: **first is not
only.** A Windows machine with Intel graphics is an ordinary laptop, and an application that fails
on one has not shipped, it has narrowed.

**The GPU was this file's whole subject and is now its first half.** PP851 measured a pad and the
facts were the same shape — what the client requires of a device, and what its absence looks like —
so they are a section below rather than a second file. The rule that binds both is the one this file
opens with: absence is quiet, and a feature that is not there is simply not offered.

## The GPU floor

Four things must keep working on a machine with no NVIDIA card at all. They are not aspirations;
a change that breaks one of them is a regression whether or not anything else improved.

**A block heading is a schedule, not a requirement.** §PP647 is why that sentence is here: Block I
is titled "NVIDIA path" because it is the push that was put second on instruction, and roadkeep.toml
already noted that two of its lines were never image quality. §PP53 is one of them and it is
vendor-neutral outright — which is now a row below rather than a note, because a reader scanning the
heading would have taken a latency win as gated on hardware it does not need.

| Must keep working | Where it is decided today |
|---|---|
| **Hardware decode via d3d11va** | [`qmlsettings.cpp`](../gui/src/qmlsettings.cpp) lists `vulkan`, `d3d11va` and `cuda`, each filtered by `hwDecoderRuntimeAvailable`. The choice itself is [`chiaki_decoder_choice`](../lib/src/decoderchoice.c): cuda only when the window reports an NVIDIA card *and* ffmpeg lists it; otherwise vulkan, then d3d11va. **This row is the one with a test.** [`test/decoderchoice.c`](../test/decoderchoice.c) asserts it on the machine it is written for - no NVIDIA card, an OpenGL window, d3d11va listed - and dropping the d3d11va arm turns the suite red (§PP77). |
| **A vendor-neutral renderer** | Vulkan is the default and OpenGL is the fallback when Vulkan initialisation fails ([`qmlmainwindow.cpp`](../gui/src/qmlmainwindow.cpp)). Neither is an NVIDIA path, and the fallback is taken on the driver's answer rather than on the vendor. |
| **A present that can tear, with no vendor extension** | Variable refresh is not an NVIDIA feature and PP53 measured it as DXGI's: `DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING` with `DXGI_PRESENT_ALLOW_TEARING`, which is the same pair FreeSync and VESA Adaptive-Sync answer to. **This row has a test.** `VendorNeutralPresentTests` reads every file of the render shim — the swapchain probes, the DirectComposition trees, both tearing probes — and fails if any of them names a GPU vendor at all, with `chiaki_shim.c` as the control that shows the check can find one (§PP647). |
| **An SDR present with no NGX** | Nothing in the tree loads NGX, and the present path asks for neither vendor feature. Both are now priced and neither is integrated: RTX Video Super Resolution did not engage on this card (§PP47), and RTX Video HDR does engage, cheaply (§PP49); see "What the deleted spikes measured" below. A feature that is measured is still not a feature that is shipped, and this row is about the second. |

## What absence looks like

**Nothing visible except that the option is not offered.** A machine without cuda does not see cuda
in the decoder list, because the list is built from what ffmpeg reports and what the runtime can
actually open. There is no error, no warning, and no degraded mode to explain — a feature that is
not there is simply not in the menu.

The same rule binds every vendor feature added later: the absence path is the *quiet* one. A dialog
saying an NVIDIA card would be better is a requirement with a friendly voice.

**And there is a third shape, which §PP648 measured rather than predicted.** A driver feature can be
present, offered, and switched off in the vendor's own control panel — and then the option *is* in
the menu, every call succeeds, and nothing happens. RTX Video Super Resolution does not engage on
this RTX 4060 (§PP47 and §PP648) while RTX Video HDR does (§PP49) — same card, same driver, same
API. So the toggles are per feature. `SpikeExtensionEchoTests` holds the three committed runs.

That binds anything built on one of them: **a vendor feature that reports success is not a vendor
feature that ran.** Whatever ships has to read back the effect rather than the return code, the way
both spikes compare pixels, or the setting will claim to be on for users whose panel says otherwise.

## What the evidence actually covers

Worth stating plainly, because the spikes' tables read stronger than they are:

- All three hardware decode paths are measured, not just cuda (§PP48, ledger), and the paced
  numbers put cuda **last** of the three (§PP71). The non-NVIDIA path is not the unmeasured one.
- Every one of those runs was taken on the same machine — an RTX 4060, named in each committed
  result since §PP66. **`d3d11va` has been measured on an NVIDIA card running d3d11va, never on
  Intel or AMD silicon.** There is no such machine here.
- The two NGX evaluations say nothing about the floor either way, and are listed above only so the
  row is not read as "nobody looked". Neither ran on non-NVIDIA silicon and neither could: an
  absent driver extension is the case they would be measuring, and its cost is zero by definition.

So the floor above is a contract held by code review, not by measurement. Saying otherwise would be
the folklore this block exists to refuse.

## What the driver offers for frame generation

Measured by PP50's spike on the RTX 4060 (driver 32.0.16.1074), recorded here when PP866 deleted
the spike, so nobody has to rebuild it to learn the answer:

- The D3D11 video processor offers **no frame-rate conversion**:
  `D3D11_VIDEO_PROCESSOR_PROCESSOR_CAPS_FRAME_RATE_CONVERSION` is absent, and the caps read `0x17`
  under all three usages.
- The 2-out-per-1-in custom rate it does offer **duplicates rather than interpolates**: the
  generated frame is byte-identical to the previous one (0 of 2,073,600 pixels differ, with
  `RepeatFrame` true or false), at 53.4 µs a frame.
- Interpolation would need one future frame, a 33.3 ms hold at 30 fps, and DLSS Frame Generation
  cannot apply to a decoded stream at all. There is no interpolation trade to take on this card.
- Vortice mis-types `ppPastSurfaces`; the spike passed a hand-built array, and `--prove-arrays`
  crashing with `0xC0000005` is what showed that array reached the driver.

## What the deleted spikes measured

PP871 deleted `spike/` once every decision in it was taken. The committed readings a test reads
are fixtures under `tests/Jogarei.Tests/fixtures/spikes`, byte for byte; what only the READMEs
held is here. Every number is from the RTX 4060 above, driver 32.0.16.1074, Windows 11.

- **Present path** (§PP43, §PP58). A `D3DImage` shared surface holds 59.9 fps with a 42–47 µs
  present median and a ~5 ms p99. An `HwndHost` child window presented from
  `CompositionTarget.Rendering` holds ~31.8 fps: `PresentEx` waits a vblank even with
  `PresentInterval.Immediate`, and that wait serialises with WPF's own vblank-synchronised tick.
  From a background thread the same window holds 60.0 fps; a `D3DImage` cannot be driven off the UI
  thread at all. `AddDirtyRect` is 37 of the shared surface's 47 µs. A `RenderTargetBitmap`
  capture proves the drawing, never the presenting.
- **FFmpeg decode** (§PP48, §PP65, §PP71), 1080p60 H.264 at ~29 Mbps. Unpaced, the three hardware
  decoders sit within 13% of each other, cuda and vulkan within 0.1% (one engine). What differs is
  the per-frame `av_hwframe_transfer_data` readback: cuda 793 µs, d3d11va 2 253 µs, vulkan none
  (1 880 µs without its exemption); software decode is 5 462 µs. d3d11va's 103 µs median against
  a 26 990 µs p99 is the harness feeding twelve times real time: paced at 60 fps it is 285 µs p50
  and 548 µs p99, and `extra_hw_frames` of 0, 4 or 16 changes nothing. Paced totals per frame are
  vulkan ~400, d3d11va ~2 550, cuda ~2 900 µs; cuda's paced median is four to eight times the
  others', with the clocks falling in the idle 90% of each interval (implicated, not proven). HEVC
  was not measured.
- **Media Foundation** (§PP650, §PP818, §PP826, §PP828). One D3D11-aware decoder per codec:
  Microsoft's H.264 MFT, and `HEVCVideoExtension`, a Store extension a user can uninstall. "No
  hardware URL" does not mean the CPU decodes. Paced H.264 costs ~750 µs a frame; unpaced it is
  bimodal (180 µs p50, 14 500 µs p99) exactly as d3d11va was. The output is `D3D11_BIND_DECODER`
  only (bind `0x200`, NV12 1920x1088, array of 8), so a renderer copies it, GPU to GPU, in ~25 µs.
  Leaving FFmpeg would drop 87.6 MB of DLLs and the cuda and vulkan choices. Three traps each gave
  a plausible wrong answer: `MFT_ENUM_FLAG_TRANSCODE_ONLY` narrows the search; `MF_SA_D3D11_AWARE`
  belongs to the created transform, not the activate; and `[ComImport]` inheritance does not
  compose a vtable, so each interface is declared flat.
- **RTX Video Super Resolution and RTX Video HDR** (§PP47, §PP648, §PP49). Two different D3D11
  video-processor stream extensions, both from mpv's `vf_d3d11vpp.c`: super resolution is
  `d43ce1b3-1f4b-48ac-baee-c3c25375e6f7` (version 1, method 2), true HDR is
  `fdd62bb4-620b-4fd7-9ab3-1e59d0d544b3` (version 4, method 3). Super resolution changed 0 of
  8,294,400 pixels in two runs; the plain 1080p-to-4K processor scale it would add to costs
  262.9 µs. True HDR moved 2,073,580 of 2,073,600 pixels for 29.0 µs a frame over a 69.6 µs
  SDR-to-PQ conversion. Auto processing must stay on: turning it off disables both extensions while
  every call succeeds. D3D11 timestamp queries run on the 3D queue and cannot time
  `VideoProcessorBlt` (194 of 200 intervals did not advance), so the instrument is wall clock over a
  drained batch of 25 blts; without the drain it reads 0.2 µs. DLSS Super Resolution cannot apply to
  a decoded stream at all.
- **The rest live in the types that took the decision**: microphone capture in `WasapiCapture` and
  `MicrophoneUnits` (§PP652), echo cancellation in `EchoCancellation` (§PP52), the HUD overlay in
  `OverlayDraw` (§PP641), and the Opus decoder in `OpusDependency` and `SpikeRunQualityTests`
  (§PP32, §PP651).

## What the gate runs on

Today: nothing. [`.github/workflows/roadkeep.yml`](../.github/workflows/roadkeep.yml) is the only
workflow in the tree and it lints the roadmap documents. There is no CI build, no CI test run and
no GPU of any vendor in CI — §PP22 and §PP36 are the lines that change that.

When they do, this is the requirement they inherit: **a vendor path that is the only one with a test
is a vendor requirement with extra steps.** Whatever the gate ends up running, the non-NVIDIA
decode path is in it.

## What this client requires of a pad

Measured with a DualSense attached to this machine, so a later reader does not have to attach one
to learn it. PP845 wired its touchpad and sensors and PP846 its rumble; PP851 is the haptics a
DualSense can *hear*, and that one turns out to depend on the cable.

| Over USB | Over Bluetooth |
|---|---|
| The pad is **054C:0CE6** — the vendor and product pair PP846 routes the 47-byte effects report by, confirmed against `USB\VID_054C&PID_0CE6&MI_00` rather than read out of the Qt client. | The same pad, the same ids, and it still enumerates: SDL maps it, the buttons arrive, the touchpad arrives, and the effects report still rumbles it. |
| It presents **audio endpoints of its own**, which is the only route the console's haptic waveform has to the wide-band actuators. | **No audio endpoint at all.** The device is simply absent, and with it the whole of PP851's path. |

**So the haptic-audio feature is conditional on the cable, and that is not a defect to work
around.** A user on Bluetooth gets PP846's fold to a single rumble strength — the same answer a
DualShock 4 gets, and the Qt client's own answer when it finds no haptic device. What must not
happen is a port that treats the missing endpoint as an error: it is the ordinary state of a
wireless pad, and this file's opening rule applies unchanged — absence is quiet.

**The endpoint's name is localised.** It reads `Alto-falantes (2- …)` on a Portuguese Windows, so a
device chosen by name is chosen by SUBSTRING. The substring the Qt client uses is the pad's model
words, which is loose by construction: an Xbox pad on the same machine carries two of the same
three words. Matching on it alone is a decision to accept a wrong device, not an oversight — and
this machine has both pads attached, so it is the machine that would find out.
