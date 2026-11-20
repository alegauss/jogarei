// Only the reader moves the window. The rule: a panel that keeps its own content in view
// scrolls its own element, never scrollIntoView, which scrolls every scrollable ancestor
// including the document. This is the source lint that stops the autoplaying frame path from
// dragging a reader back to the hero once a second.
//
// Also here: the site fetches no third-party font at page load, which is a stated non-goal,
// so no source file may link fonts.googleapis.com.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, extname } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");

function walk(dir, out = []) {
  for (const name of readdirSync(dir)) {
    if (name === "node_modules" || name === "dist" || name === "dist-server") continue;
    const full = join(dir, name);
    if (statSync(full).isDirectory()) walk(full, out);
    else out.push(full);
  }
  return out;
}

const sourceFiles = walk(join(siteDir, "src")).filter((f) =>
  [".ts", ".tsx", ".js", ".jsx"].includes(extname(f)),
);

const relative = (f) => f.replace(siteDir, "").replace(/^[\\/]/, "").replace(/\\/g, "/");

test("no source calls scrollIntoView", () => {
  // the call, not the word: a comment explaining why we avoid it is fine
  const offenders = sourceFiles.filter((f) => readFileSync(f, "utf8").includes("scrollIntoView("));
  assert.deepEqual(
    offenders.map(relative),
    [],
    "a panel must scroll its own element (scrollTop), never scrollIntoView",
  );
});

// The header's chrome is sticky, sits at z-index 50 and paints a blurred bar. Styled by the
// bare element, every other <nav> on the page gets all three: the sibling strip on the
// feature pages became a second sticky bar and parked itself on top of the header as soon as
// the page moved. Nothing failed - it rendered, and it was wrong - so the selector is held
// here rather than remembered. The header is the design system's SiteNav now, styled by
// .vg-site-nav, and this keeps the page's own stylesheet from undoing that.
test("the header's chrome is styled by its class, not by the nav element", () => {
  const css = readFileSync(join(siteDir, "src", "index.css"), "utf8");
  const bare = [...css.matchAll(/^nav[\s{,]/gm)].map((m) => m[0].trim());
  assert.deepEqual(
    bare,
    [],
    "index.css styles the bare nav element, so every <nav> on the page inherits the " +
      "header's sticky bar: scope the rule to a class",
  );
});

test("analytics loads only from the consent banner", () => {
  // PP860. The banner's promise is that nothing reaches Google before a choice. A tag pasted
  // back into the head would load on every page and the banner would go on asking regardless,
  // so the template is held to carrying none and the consent component to being the only
  // loader. The docs area's half is held in docs.test.mjs, against what it built.
  const html = readFileSync(join(siteDir, "index.html"), "utf8");
  assert.ok(!html.includes("googletagmanager.com"), "index.html loads analytics before consent");
  const loaders = sourceFiles.filter((f) => readFileSync(f, "utf8").includes("googletagmanager.com"));
  assert.deepEqual(loaders.map(relative), ["src/components/Consent.tsx"]);
});

test("the consent banner promises nothing a script loaded without consent breaks", () => {
  // Every third-party script the source appends, Google's aside, loads without asking, so the
  // banner may not say that nothing loads, and it has to name the one that does: the house ad.
  const banner = readFileSync(join(siteDir, "src", "components", "Consent.tsx"), "utf8");
  const hosts = new Set(
    sourceFiles
      .flatMap((f) => [...readFileSync(f, "utf8").matchAll(/"https:\/\/([^/"]+)\/[^"]*\.js"/g)])
      .map((m) => m[1])
      .filter((h) => !h.endsWith("googletagmanager.com")),
  );
  if (hosts.size === 0) return;
  assert.ok(!/nothing is loaded/i.test(banner), `the banner says nothing loads, and ${[...hosts]} does`);
  assert.ok(/\bad\b/i.test(banner), "the banner does not name the ad that loads either way");
});

test("no source fetches a third-party font at page load", () => {
  const all = [...sourceFiles, join(siteDir, "index.html")];
  const offenders = all.filter((f) => readFileSync(f, "utf8").includes("fonts.googleapis.com"));
  assert.deepEqual(offenders.map(relative), []);
});

// The band loops by translating one whole repeat of its traces, so a period that does not
// divide that repeat puts a visible seam through the signal once per cycle, which is every
// few seconds, forever. The arithmetic is the whole of the illusion, so it is asserted rather
// than left to whoever next retunes the speeds.
test("every signal period closes on the repeat the drift translates by", () => {
  const src = readFileSync(join(siteDir, "src", "components", "ui", "Signal.tsx"), "utf8");
  const span = Number(/const SPAN = (\d+)/.exec(src)?.[1]);
  assert.ok(span > 0, "Signal.tsx no longer declares SPAN");
  // the drift animation moves by 50% of a band drawn at 200%, so one repeat is half the span
  const repeat = span / 2;
  const periods = [...src.matchAll(/period: (\d+)/g)].map((m) => Number(m[1]));
  assert.ok(periods.length >= 3, "expected a period per layer");
  assert.deepEqual(
    periods.filter((p) => repeat % p !== 0),
    [],
    `every period must divide ${repeat}`,
  );
});

// The copy states no count and no version of its own: both come off the application's source
// through src/lib/product.ts. A number typed into a sentence is true the day it is typed.
test("the copy does not hard-code the application version", () => {
  const content = readFileSync(join(siteDir, "src", "lib", "site-content.ts"), "utf8");
  const features = readFileSync(join(siteDir, "src", "lib", "features.ts"), "utf8");
  for (const [name, src] of [
    ["site-content.ts", content],
    ["features.ts", features],
  ]) {
    const literals = [...src.matchAll(/"\d+\.\d+\.\d+"/g)].map((m) => m[0]);
    assert.deepEqual(literals, [], `${name} states a version literal instead of calling version()`);
  }
});
