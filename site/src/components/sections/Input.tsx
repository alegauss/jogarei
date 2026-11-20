import { SiteRich as Rich, SiteSectionHead } from "@viglet/viglet-design-system/site";
import { helpLines, input } from "../../lib/site-content";
import { mappingDiagram } from "../../lib/diagrams";
import { RawSvg } from "../ui/RawSvg";

export function Input() {
  return (
    <section id="input">
      <div className="vg-site-wrap">
        <div className="reveal">
          <SiteSectionHead eyebrow={input.eyebrow} title={input.heading}>
            <Rich runs={input.intro} />
          </SiteSectionHead>
        </div>
        <div className="split reveal">
          <div className="split-txt">
            <ul className="feat-list">
              {input.list.map((runs, i) => (
                <li key={i}>
                  <span className="chk">✓</span>
                  <span>
                    <Rich runs={runs} />
                  </span>
                </li>
              ))}
            </ul>
          </div>
          <RawSvg className="shot-frame" markup={mappingDiagram} />
        </div>
        <div className="term reveal" style={{ marginTop: "44px" }}>
          <div className="bar">
            <i />
            <i />
            <i />
            <span>{input.terminalTitle}</span>
          </div>
          {/* The flag list is generated from the application's own source on every build, so
              a flag renamed there cannot leave a line here that the program no longer
              answers. */}
          <pre>
            {input.helpLead}
            {"\n\n"}
            {helpLines().join("\n")}
            {"\n\n"}
            <span className="c">{input.helpNote}</span>
          </pre>
        </div>
      </div>
    </section>
  );
}
