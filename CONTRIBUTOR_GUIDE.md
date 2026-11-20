# Contributing to Jogarei

Jogarei is a remote play client for Windows, written in C# on .NET 10 and WPF. It started as a fork
of chiaki-ng and is being ported off its C core one piece at a time: what has been ported is
managed code, and what has not still runs from the C library through a small native shim.

## Where things are

| Path | What it holds |
|---|---|
| `app\Protocol\` | The remote play protocol in managed code: the transport, reordering, error correction, the frame path, encryption |
| `app\Session\` | Sessions, discovery, registration, the PSN login, telemetry, and the checks that hold the port against its sources |
| `app\Native\` | The boundary to native code: the shim, the renderer, audio, Windows APIs |
| `app\Settings\`, `app\Views\` | The settings store and the WPF screens |
| `tests\Jogarei.Tests\` | The xUnit suite |
| `lib\` | chiaki-ng's C library, vendored and never patched here; it leaves as the port completes |
| `shim\` | The native DLLs the host loads: `chiaki-shim.dll` over `lib\`, and `chiaki-render.dll` |
| `site\` | The site and the documentation at [alegauss.com/jogarei](https://alegauss.com/jogarei/) |
| `docs\` | The roadmap, the changelog, design notes and the hardware contract |

## Building and testing

You need Windows 10 or 11 on x64, [MSYS2](https://www.msys2.org/) with MinGW64 for the native side,
and the .NET 10 SDK. From PowerShell or cmd, at the repository root:

```
compile.cmd    build the native libraries, the host and the tests
test.cmd       run the C suite, the host's self-test and the .NET tests
package.cmd    stage the payload, prove it runs outside the checkout, build the installer
```

Run one at a time, and not from Git Bash: its PATH puts a different compiler first and breaks the
CMake cache. [CLAUDE.md](CLAUDE.md) lists this and the other traps the build has.

The site has its own gate: `npm run build` and then `npm test` in `site\`.

## How work is tracked

The backlog is [docs/ROADMAP.md](docs/ROADMAP.md), with its design notes in
[docs/IMPROVEMENTS.md](docs/IMPROVEMENTS.md) and what has shipped in
[docs/CHANGELOG.md](docs/CHANGELOG.md). Those three are written by
[roadkeep](https://github.com/alegauss/roadkeep), not by hand. Each task has a `PP` number.

- **One task, one commit.** The code, its tests and the roadmap change land together.
- **Every task ships with a test that names its id** and fails without the change. A ratchet in
  the suite counts shipped tasks that no test names, and the count may not rise.
- **The non-goals bind.** They are listed in the roadmap: no other platforms, no redesign while
  porting, no local patch to the vendored C, and no hand-rolled subsystem where Windows already
  ships one.

## Reporting a problem

Open an issue here, with the session log from `%APPDATA%\Chiaki\Chiaki\log`. Problems with Jogarei
belong in this repository, not with the chiaki-ng project.
