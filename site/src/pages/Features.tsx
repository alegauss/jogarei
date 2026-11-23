import { SiteRich as Rich } from "@viglet/viglet-design-system/site";
import { Nav, Footer } from "../components/Chrome";
import { FeatureCards } from "../components/ui/FeatureCards";
import { featuresIndex } from "../lib/site-content";

// The index the nav's "Features" points at. It exists as a route rather than as the landing's
// #features anchor because the nav is on every page: an anchor is a link that works from the
// landing and does nothing from anywhere else, and a section of one page is not a thing the
// other six can address.
export function Features() {
  return (
    <>
      <Nav />
      <header className="page-hero" id="top">
        <div className="vg-site-wrap">
          <a className="feature-back" href="/transmiti/">
            {featuresIndex.backLabel}
          </a>
          <div className="vg-site-eyebrow">{featuresIndex.eyebrow}</div>
          <h1>{featuresIndex.heading}</h1>
          <p className="sub">
            <Rich runs={featuresIndex.lead} />
          </p>
        </div>
      </header>

      <section style={{ paddingTop: "28px" }}>
        <div className="vg-site-wrap">
          <FeatureCards />
        </div>
      </section>

      <Footer />
    </>
  );
}
