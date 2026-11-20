
<img src="site/public/logo.svg" alt="" width="72" height="72">

# Jogarei

A remote play client for Windows that streams your PS5 or PS4 to a window that behaves like the
rest of Windows. It is a native .NET 10 and WPF application, free software under the AGPL-3.0.
"Jogarei" is Portuguese for "I will play".

**Site and documentation:** [alegauss.com/jogarei](https://alegauss.com/jogarei/) ·
[how it works](https://alegauss.com/jogarei/docs/)

## What it does differently

- **A Windows interface.** WPF on the system's Fluent theme, light and dark following the OS, and
  every screen reachable with a controller.
- **Ten-bit video.** libplacebo renders into a shared Direct3D 11 texture and DirectComposition
  presents it, paced by the display's own refresh rate, with the stream overlay above the video.
- **Hardware decode on the card you have.** H.264 and HEVC through d3d11va, Vulkan or CUDA, chosen
  from what the machine reports. Tuning goes to NVIDIA first; what must keep working without an
  NVIDIA card is written down in [docs/HARDWARE-CONTRACT.md](docs/HARDWARE-CONTRACT.md).
- **Nothing bundled that Windows already ships.** No browser in the installer: the account login
  uses WebView2.
- **Side by side with chiaki-ng.** It installs under its own identity and reads the consoles,
  pairings, controller mappings and token chiaki-ng already stored, so your setup carries over.

## Status

There is no release yet. The stream, the transport and the frame path run in managed code, each
held against the C it replaces. Two input defects are open in live sessions: a button press does
not reach the console (PP853), and the controller stops driving the console after the window loses
focus (PP854). The backlog is in [docs/ROADMAP.md](docs/ROADMAP.md).

Report problems in this repository's issues, not to the chiaki-ng project.

## Building

Windows 10 or 11, x64. The native side builds in [MSYS2](https://www.msys2.org/) MinGW64 and the
host needs the .NET 10 SDK.

```
compile.cmd    build the native libraries, the host and the tests
test.cmd       run the C suite, the host's self-test and the .NET tests
package.cmd    stage the payload, prove it runs outside the checkout, build the installer (Inno Setup 6)
```

Run them from PowerShell or cmd, not from Git Bash: see [CLAUDE.md](CLAUDE.md) for why.

## Credits

Jogarei stands on [Chiaki](https://git.sr.ht/~thestr4ng3r/chiaki) and
[chiaki-ng](https://github.com/streetpea/chiaki-ng), whose work on the remote play protocol made it
possible. It started as a fork of chiaki-ng, and its vendored C library is still theirs.

## Licence

[AGPL-3.0](LICENSES/AGPL-3.0-only-OpenSSL.txt), with the OpenSSL exception.

Unofficial community project. Not affiliated with, endorsed by, or sponsored by Sony Interactive
Entertainment. "PlayStation", "PS4" and "PS5" are trademarks of Sony Interactive Entertainment Inc.
