import { features } from "../../lib/features";

// The card grid of the /features page the nav points at. It reads the feature records, as the
// landing's index does, so the two cannot come to disagree about what exists.
//
// The landing draws its grid with the design system's SiteFeatureIndex, which brings its own
// section head; this page's head is its hero, so it takes the same card classes on their own.
export function FeatureCards() {
  return (
    <div className="vg-site-feature-index reveal">
      {features.map((f) => (
        <a className="vg-site-feature-card" href={`/jogarei/features/${f.slug}/`} key={f.slug}>
          <h3>{f.heading}</h3>
          <p>{f.description}</p>
          <span className="vg-site-feature-card-go">Read the page →</span>
        </a>
      ))}
    </div>
  );
}
