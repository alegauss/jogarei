import {
  SiteButton,
  SiteFooter,
  SiteNav,
  SiteSponsor,
  SiteThemeToggle,
} from "@viglet/viglet-design-system/site";
import {
  download,
  footer,
  navLinks,
  parentUrl,
  releasesUrl,
  repoUrl,
  sponsor,
  THEME_KEY,
} from "../lib/site-content";
import { ConsentSettings } from "./Consent";
import { Signal } from "./ui/Signal";

// The chrome every route shares, drawn by the design system's site layer (PP860). What is left
// here is this product's half of it: which links, which words, and the signal the footer opens
// out of.

// The mark carries its own silhouette, so it is sized by height and left to find its width: a
// square box and a corner radius belong to a tile, which this mark is not.
const brand = {
  href: "/jogarei/",
  name: "Jogarei",
  logo: "/jogarei/logo.svg",
  logoShape: "mark",
} as const;

const themeLabel = (next: string) => `Switch to ${next} theme`;

export function Nav() {
  return (
    <SiteNav
      brand={brand}
      parent={{ href: parentUrl, name: "alegauss", pre: "part of", title: "alegauss: small developer tools" }}
      links={navLinks}
      actions={
        <>
          <SiteButton href={releasesUrl}>{download.ctaShort}</SiteButton>
          <SiteButton variant="ghost" href={repoUrl}>
            ★ View on GitHub
          </SiteButton>
          <SiteThemeToggle storageKey={THEME_KEY} label={themeLabel} />
        </>
      }
    />
  );
}

// No ad slot here. The house ad sits in the middle of each page's content, at a seam the
// reading already had, rather than at the end where the footer's own sponsor block and the
// disclaimer already are: two commercial blocks stacked at the page end read as a wall, and
// the one furthest down the page is the one nobody reaches.
export function Footer() {
  return (
    <SiteFooter
      brand={brand}
      links={footer.links}
      sponsor={
        <SiteSponsor
          declaration={sponsor}
          moreLabel="More at"
          note="Both Apache 2.0 and self-hostable."
        />
      }
      disclaimer={
        <>
          {footer.disclaimer} <ConsentSettings />
        </>
      }
    >
      <Signal className="signal--footer" />
    </SiteFooter>
  );
}
