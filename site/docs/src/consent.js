// Google Analytics for the documentation area, only after the visitor allows it (PP860). The
// flow is the site's src/components/Consent.tsx, and the key and the measurement id are the
// hub's (its AG1): the area is served from the same origin, so a visitor who answered on the
// landing pages, on the hub or here has answered for all three. Inlined into every page's head
// by astro.config.mjs, so it is plain script with no imports.
(function () {
  var KEY = "alegauss-consent";
  var ID = "G-6HG45Q9BHP";

  function read() {
    try {
      var v = localStorage.getItem(KEY);
      return v === "granted" || v === "denied" ? v : null;
    } catch (e) {
      return null;
    }
  }

  function load() {
    window.dataLayer = window.dataLayer || [];
    window.gtag = function () {
      window.dataLayer.push(arguments);
    };
    window.gtag("js", new Date());
    window.gtag("config", ID);
    var s = document.createElement("script");
    s.async = true;
    s.src = "https://www.googletagmanager.com/gtag/js?id=" + ID;
    document.head.appendChild(s);
  }

  function choose(choice, banner) {
    try {
      localStorage.setItem(KEY, choice);
    } catch (e) {
      /* private mode: the choice lasts this page */
    }
    if (choice === "granted") load();
    banner.remove();
  }

  function ask() {
    var banner = document.createElement("section");
    banner.className = "pp-consent";
    banner.setAttribute("aria-label", "Analytics consent");
    var text = document.createElement("p");
    text.textContent =
      "This site counts visits with Google Analytics only if you allow it, and analytics is not loaded until you choose.";
    var actions = document.createElement("div");
    actions.className = "pp-consent-actions";
    var decline = document.createElement("button");
    decline.type = "button";
    decline.textContent = "Decline";
    decline.onclick = function () {
      choose("denied", banner);
    };
    var allow = document.createElement("button");
    allow.type = "button";
    allow.className = "pp-consent-allow";
    allow.textContent = "Allow analytics";
    allow.onclick = function () {
      choose("granted", banner);
    };
    actions.append(decline, allow);
    banner.append(text, actions);
    document.body.appendChild(banner);
  }

  var choice = read();
  if (choice === "granted") load();
  else if (choice === null) {
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", ask);
    else ask();
  }
})();
