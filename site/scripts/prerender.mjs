// The prerender and the social card, both through the design system's build helpers (PP860).
//
// prerenderSite renders every route into the client build's index.html, patching the <head>
// by replace-or-throw so a drifted template fails the build rather than shipping a page under
// another route's title, and writes the Markdown twin beside each page, manifest.json and
// sitemap.xml (lastmod from git, never the build clock), all from the route table routes.tsx
// has already asserted against the component map — so a route missing from either side never
// reaches the loop. It reads the SSR bundle built by `vite build --ssr`.
//
// renderOgCard rasterises public/og.svg to dist/og.png at 1200x630. The card stays the
// hand-drawn one: a platform that renders a link preview cannot rasterise an SVG, so an
// og:image pointing at one introduces the project as an empty rectangle.
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { prerenderSite, renderOgCard } from "@viglet/viglet-design-system/vite";

import {
  render,
  renderStatic,
  ROUTE_META,
  canonicalUrl,
  outputDir,
  OG_IMAGE,
} from "../dist-server/entry-server.js";
import { htmlToMarkdown } from "./markdown.mjs";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const distDir = join(siteDir, "dist");

const card = await renderOgCard({
  svg: readFileSync(join(siteDir, "public", "og.svg"), "utf8"),
  out: join(distDir, "og.png"),
  // the card names DejaVu explicitly; the helper loads system fonts so its text renders
  fontFamily: "DejaVu Sans",
});
console.log(`og-image: dist/og.png  ${card.width}x${card.height}  (${(card.bytes / 1024).toFixed(0)} kB)`);

const written = prerenderSite({
  distDir,
  routes: ROUTE_META,
  render,
  canonicalUrl,
  outputDir,
  ogImage: OG_IMAGE,
  // The Markdown twin, converted from the same component tree as clean static HTML (S5).
  twin: (path) => htmlToMarkdown(renderStatic(path)),
  // The manifest lists the routes, their twins and their sizes, so a reader that is not a
  // browser can find the whole site from one file. No build timestamp: the same input builds
  // byte-identical output.
  manifest: { name: "PPortal", base: "/pportal/", llms: "llms.txt" },
  // The sitemap's lastmod is one date over the whole authored tree (src, public, index.html),
  // read from git rather than the build clock: a rebuild that changed nothing would otherwise
  // tell a crawler that every page did. A tarball with no .git omits it, which beats a guess.
  git: { cwd: siteDir },
  // robots.txt is written below instead, because it names a second sitemap.
  robots: false,
  log: console.log,
});

// PP446: the documentation area under /docs is a second build with a second sitemap, because
// the prerender walks ROUTE_META and ROUTE_META will never know those pages. Naming it here
// is what makes both halves of one deploy crawlable from one file — and it is a claim about a
// file this script has not seen: the docs build runs after the prerender, so `docs.test.mjs`
// asserts that what robots names is in dist. A docs build that was skipped, or emptied by a
// later `vite build`, fails there rather than publishing a Sitemap: line pointing at a 404.
//
// Absolute, because that is what makes the sitemap discoverable without a submission — and
// it carries the base prefix, so the rename that moves every route moves this too.
const sitemapUrl = `${canonicalUrl("/")}sitemap.xml`;
const docsSitemapUrl = `${canonicalUrl("/")}docs/sitemap-index.xml`;
writeFileSync(
  join(distDir, "robots.txt"),
  `User-agent: *\nAllow: /\n\nSitemap: ${sitemapUrl}\nSitemap: ${docsSitemapUrl}\n`,
);

console.log(
  `prerender: ${written.length} route(s) + twins, manifest.json, sitemap.xml and robots.txt written to dist/`,
);
