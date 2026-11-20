import { SiteRich as Rich } from "@viglet/viglet-design-system/site";
import { floor } from "../../lib/site-content";

export function Floor() {
  return (
    <section style={{ paddingTop: "20px" }}>
      <div className="vg-site-wrap reveal">
        <div className="banner">
          <div className="lock">{floor.icon}</div>
          <h2>{floor.heading}</h2>
          {floor.body.map((runs, i) => (
            <p key={i}>
              <Rich runs={runs} />
            </p>
          ))}
          <p>
            <a className="feature-link" href={floor.linkHref}>
              {floor.linkLabel}
            </a>
          </p>
        </div>
      </div>
    </section>
  );
}
