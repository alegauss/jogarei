import { SiteButton, SiteRich as Rich, SiteSectionHead } from "@viglet/viglet-design-system/site";
import { download, releasesUrl } from "../../lib/site-content";

// The section the argument ends in: the reader who accepted it can have the thing. The two
// buttons carry data-twin="omit" for the reason the hero's do, because a call to action
// converts a person and costs an agent the same words on every page. The prose around them
// does not, because what the installer places and what it leaves alone are facts a reader is
// right to want before installing anything.
export function Download() {
  return (
    <section id="download">
      <div className="vg-site-wrap">
        <div className="reveal">
          <SiteSectionHead eyebrow={download.eyebrow} title={download.heading}>
            <Rich runs={download.intro} />
          </SiteSectionHead>
        </div>
        <div className="vg-site-hero-actions" data-twin="omit">
          <SiteButton href={releasesUrl}>{download.cta}</SiteButton>
          <SiteButton variant="ghost" href={releasesUrl}>
            {download.secondary}
          </SiteButton>
        </div>
        <div className="vg-site-hero-meta">
          {download.facts.map((fact) => (
            <span key={fact}>{fact}</span>
          ))}
        </div>
        <p
          style={{
            maxWidth: "720px",
            margin: "26px auto 0",
            textAlign: "center",
            color: "var(--muted-2)",
            fontSize: ".9rem",
          }}
        >
          <Rich runs={download.note} />
        </p>
      </div>
    </section>
  );
}
