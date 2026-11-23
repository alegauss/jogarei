import { SiteRich as Rich } from "@viglet/viglet-design-system/site";
import { Nav, Footer } from "../components/Chrome";
import { Ad } from "../components/ui/Ad";
import { hardware } from "../lib/site-content";

// The hardware page: the product's own contract, written for a reader. The table is a table
// because it is a table in the product's note too, and because "what must keep working" and
// "how it is decided" are two columns a reader compares across rows.
export function Hardware() {
  return (
    <>
      <Nav />
      <header className="page-hero" id="top">
        <div className="vg-site-wrap">
          <a className="feature-back" href="/transmiti/">
            {hardware.backLabel}
          </a>
          <div className="vg-site-eyebrow">{hardware.eyebrow}</div>
          <h1>{hardware.heading}</h1>
          <p className="sub">
            <Rich runs={hardware.lead} />
          </p>
        </div>
      </header>

      <section style={{ paddingTop: "28px" }}>
        <div className="vg-site-wrap">
          <div className="feature-body tight">
            <div className="feature-section reveal">
              <h2>{hardware.floorHeading}</h2>
              <p>
                <Rich runs={hardware.floorIntro} />
              </p>
              <div className="table-wrap">
                <table className="matrix">
                  <thead>
                    <tr>
                      <th>Must keep working</th>
                      <th>How it is decided</th>
                    </tr>
                  </thead>
                  <tbody>
                    {hardware.floor.map((row) => (
                      <tr key={row.what}>
                        <td>
                          <b>{row.what}</b>
                        </td>
                        <td>
                          <Rich runs={row.how} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Between the floor and what absence looks like: the table is the tall half of
                this page, so this is where a reader crosses the middle of it, and it is a
                boundary between two sections rather than a break inside one. */}
            <Ad slot="hardware" band={false} />

            <div className="feature-section reveal">
              <h2>{hardware.absenceHeading}</h2>
              {hardware.absence.map((runs, i) => (
                <p key={i}>
                  <Rich runs={runs} />
                </p>
              ))}
            </div>

            <div className="feature-section reveal">
              <h2>{hardware.evidenceHeading}</h2>
              <p>
                <Rich runs={hardware.evidence} />
              </p>
            </div>
          </div>
        </div>
      </section>

      <Footer />
    </>
  );
}
