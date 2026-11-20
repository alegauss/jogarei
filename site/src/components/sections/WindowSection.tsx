import { SiteRich as Rich, SiteSectionHead } from "@viglet/viglet-design-system/site";
import { windowSection } from "../../lib/site-content";
import { consoleStateIcons, windowDiagram } from "../../lib/diagrams";
import { RawSvg } from "../ui/RawSvg";

export function WindowSection() {
  return (
    <section id="window">
      <div className="vg-site-wrap">
        <div className="reveal">
          <SiteSectionHead eyebrow={windowSection.eyebrow} title={windowSection.heading}>
            <Rich runs={windowSection.intro} />
          </SiteSectionHead>
        </div>
        <figure className="shot-frame reveal" style={{ margin: 0 }}>
          <RawSvg markup={windowDiagram} />
          <figcaption>
            <Rich runs={windowSection.caption} />
          </figcaption>
        </figure>

        <div className="reveal" style={{ marginTop: "62px" }}>
          <SiteSectionHead eyebrow={windowSection.statesEyebrow} title={windowSection.statesHeading} />
        </div>
        <div className="states reveal">
          {windowSection.states.map((state) => (
            <div className={`state ${state.kind}`} key={state.kind}>
              <RawSvg markup={consoleStateIcons[state.kind]} />
              <h3>{state.title}</h3>
              <p>
                <Rich runs={state.body} />
              </p>
            </div>
          ))}
        </div>

        <div className="reveal" style={{ marginTop: "62px" }}>
          <SiteSectionHead eyebrow={windowSection.detailsEyebrow} title={windowSection.detailsHeading} />
        </div>
        <ul className="feat-list two reveal">
          {windowSection.detailsList.map((runs, i) => (
            <li key={i}>
              <span className="chk">✓</span>
              <span>
                <Rich runs={runs} />
              </span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
