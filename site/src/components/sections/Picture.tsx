import { SiteRich as Rich, SiteSectionHead } from "@viglet/viglet-design-system/site";
import { picture } from "../../lib/site-content";
import { compositionDiagram } from "../../lib/diagrams";
import { RawSvg } from "../ui/RawSvg";

export function Picture() {
  return (
    <section id="picture">
      <div className="vg-site-wrap">
        <div className="reveal">
          <SiteSectionHead eyebrow={picture.eyebrow} title={<Rich runs={picture.headingRuns} />}>
            <Rich runs={picture.intro} />
          </SiteSectionHead>
        </div>
        <RawSvg className="shot-frame reveal" markup={compositionDiagram} />
        <div className="grid" style={{ marginTop: "34px" }}>
          {picture.cards.map((card) => (
            <div className="card reveal" key={card.title}>
              <div className="ico">{card.icon}</div>
              <h3>{card.title}</h3>
              <p>
                <Rich runs={card.body} />
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
