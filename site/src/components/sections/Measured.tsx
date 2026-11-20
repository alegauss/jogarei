import { SiteRich as Rich, SiteSectionHead } from "@viglet/viglet-design-system/site";
import { measured } from "../../lib/site-content";

export function Measured() {
  return (
    <section id="measured">
      <div className="vg-site-wrap">
        <div className="reveal">
          <SiteSectionHead eyebrow={measured.eyebrow} title={measured.heading}>
            <Rich runs={measured.intro} />
          </SiteSectionHead>
        </div>
        <ul className="feat-list two reveal">
          {/* What the record carries and what compares two of them render as one list and are
              two arrays, so the split stays visible in the source that states it. */}
          {[...measured.rows, ...measured.notes].map(([lead, rest]) => (
            <li key={lead}>
              <span className="chk">✓</span>
              <span>
                <b>{lead}</b>
                {rest}
              </span>
            </li>
          ))}
        </ul>
        <p
          style={{
            textAlign: "center",
            color: "var(--muted-2)",
            fontSize: ".9rem",
            marginTop: "30px",
          }}
        >
          <Rich runs={measured.note} />
        </p>
      </div>
    </section>
  );
}
