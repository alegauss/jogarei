import { SiteRich as Rich, SiteSectionHead } from "@viglet/viglet-design-system/site";
import { nonGoals } from "../../lib/site-content";

// The design system has no counterpart for this list, so it stays here, under the shared
// section head and on the shared tokens.
export function NonGoals() {
  return (
    <section>
      <div className="vg-site-wrap">
        <div className="reveal">
          <SiteSectionHead eyebrow={nonGoals.eyebrow} title={nonGoals.heading}>
            <Rich runs={nonGoals.intro} />
          </SiteSectionHead>
        </div>
        <div className="nots reveal">
          {nonGoals.items.map((item) => (
            <div className="not" key={item.title}>
              <h4>
                <em>✗</em> {item.title}
              </h4>
              <p>{item.body}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
