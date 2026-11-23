import { SiteButton, SiteFeatureIndex, SiteHero, SiteRich } from "@viglet/viglet-design-system/site";
import { Nav, Footer } from "../components/Chrome";
import { FrameTrip } from "../components/FrameTrip";
import { Why } from "../components/sections/Why";
import { Picture } from "../components/sections/Picture";
import { Input } from "../components/sections/Input";
import { WindowSection } from "../components/sections/WindowSection";
import { Measured } from "../components/sections/Measured";
import { Floor } from "../components/sections/Floor";
import { NonGoals } from "../components/sections/NonGoals";
import { Download } from "../components/sections/Download";
import { Ad } from "../components/ui/Ad";
import { Signal } from "../components/ui/Signal";
import { features } from "../lib/features";
import { download, frameTrip, hero, repoUrl } from "../lib/site-content";

// The landing page. The section order is the argument rather than a feature list: why it was
// rebuilt, then the picture (the part that decided the architecture), the pad, the screens,
// the evidence, the depth pages, the floor a machine without an NVIDIA card keeps, what it
// refuses to do, and the download.
//
// The page ends on the download, and nothing follows it: a reader who has read this far has
// decided, and the last thing they should meet is the button rather than another section.
//
// The ad's seam is between the evidence and the depth pages. Every other boundary on this
// page is mid-argument - why into picture, picture into pad, floor into non-goals - and an ad
// dropped into one of those interrupts a sentence that is still being made. This one is where
// the argument has finished and the reader is choosing where to go next, so it is a pause
// that was already there rather than one the ad introduces.
export function Landing() {
  return (
    <>
      <Nav />
      <SiteHero
        icon={{ src: "/transmiti/logo.svg", alt: "Transmiti logo", logoShape: "mark" }}
        badge={hero.badge}
        title={hero.titleLead}
        titleAccent={hero.titleAccent}
        sub={<SiteRich runs={hero.sub} />}
        // The button scrolls to the download section rather than leaving for GitHub, because
        // the question between a reader and an install is what it touches on their machine,
        // and that section answers it. The package drops the actions from the Markdown twin:
        // they convert a reader and cost an agent the same forty words on every page.
        actions={
          <>
            <SiteButton href="#download">{download.cta}</SiteButton>
            <SiteButton variant="ghost" href={repoUrl}>
              ★ View on GitHub
            </SiteButton>
          </>
        }
        // The frame's journey is content, not a call to action, so the twin keeps it.
        feature={
          <>
            <div className="session-eyebrow">{frameTrip.eyebrow}</div>
            <FrameTrip />
          </>
        }
        meta={hero.meta}
        pills={hero.pills.map((runs, i) => (
          <SiteRich runs={runs} key={i} />
        ))}
      >
        <Signal />
      </SiteHero>
      <Why />
      <Picture />
      <Input />
      <WindowSection />
      <Measured />
      <Ad slot="landing-mid" />
      {/* The landing's own index of the depth pages, one card per record, so the list and the
          pages cannot disagree about what exists. */}
      <SiteFeatureIndex
        id="features"
        eyebrow="In depth"
        title="One page per part of it"
        description="Each section above has a page to link at, whether from a README, an issue or a search result."
        goLabel="Read the page →"
        items={features.map((f) => ({
          href: `/transmiti/features/${f.slug}/`,
          heading: f.heading,
          description: f.description,
        }))}
      />
      <Floor />
      <NonGoals />
      <Download />
      <Footer />
    </>
  );
}
