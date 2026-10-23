// The portfolio renders itself from the API it is served by.
// DOM nodes are built with textContent (never innerHTML), so data can't inject markup.

const $ = (selector) => document.querySelector(selector);

function h(tag, attrs = {}, ...children) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs)) {
    if (value === null || value === undefined || value === false) continue;
    if (key === "class") el.className = value;
    else el.setAttribute(key, value === true ? "" : value);
  }
  for (const child of children.flat()) {
    if (child === null || child === undefined || child === false) continue;
    el.append(child.nodeType ? child : document.createTextNode(String(child)));
  }
  return el;
}

// Paths are relative (no leading slash) so the site also works under a sub-path,
// such as https://<user>.github.io/kennys-portfolio/ on GitHub Pages.

// When the ASP.NET Core API isn't reachable (GitHub Pages only hosts static files),
// the same data is read from portfolio.json and filtered here with the API's rules.
let staticData = null;

// Where the API lives. "" = same origin; on GitHub Pages config.js points to the hosted API.
const API_BASE = (() => {
  const base = window.PORTFOLIO_API_BASE || "";
  return base && !base.endsWith("/") ? `${base}/` : base;
})();
const API_TIMEOUT_MS = 8000; // free hosting sleeps when idle; don't make visitors stare at a blank page
const apiUrl = (path) => API_BASE + path;

const includesTerm = (values, term) => values.some((v) => v.toLowerCase().includes(term.toLowerCase()));

function fromStatic(path) {
  const url = new URL(path, "http://static.local/");
  const technology = url.searchParams.get("technology");
  const category = url.searchParams.get("category");

  switch (url.pathname.replace(/^\/+/, "")) {
    case "api/profile": return staticData.profile;
    case "api/experience":
      return technology ? staticData.experience.filter((e) => includesTerm(e.technologies, technology)) : staticData.experience;
    case "api/projects":
      return technology ? staticData.projects.filter((p) => includesTerm(p.technologies, technology)) : staticData.projects;
    case "api/skills":
      return category
        ? staticData.skills.filter((g) => g.category.toLowerCase().includes(category.toLowerCase()))
        : staticData.skills;
    case "api/education": return staticData.education;
    case "api/certifications": return staticData.certifications;
    case "api/courses": return staticData.courses;
    case "api/languages": return staticData.languages;
    default: throw new Error(`${path} is not available in static mode`);
  }
}

async function detectMode() {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), API_TIMEOUT_MS);

  try {
    const response = await fetch(apiUrl("api/profile"), {
      headers: { Accept: "application/json" },
      signal: controller.signal,
    });
    if (response.ok) {
      await response.json(); // a host that answers 200 with HTML will fail here
      return; // live API
    }
  } catch {
    // unreachable, too slow (cold start) or blocked: fall through to static mode
  } finally {
    clearTimeout(timer);
  }

  // Wake the hosted API in the background so the next visit finds it awake.
  if (API_BASE) fetch(apiUrl("health"), { mode: "no-cors" }).catch(() => {});

  const response = await fetch("portfolio.json");
  if (!response.ok) throw new Error(`portfolio.json responded ${response.status}`);
  staticData = await response.json();
  document.body.dataset.mode = "static";
}

async function getJson(path) {
  if (staticData) return fromStatic(path);

  const response = await fetch(apiUrl(path), { headers: { Accept: "application/json" } });
  if (!response.ok) throw new Error(`${path} responded ${response.status}`);
  return response.json();
}

// The endpoint links in the "API" section are written relative; point them at the real API host.
function wireEndpointLinks() {
  if (!API_BASE) return;
  for (const link of document.querySelectorAll(".endpoints a")) {
    link.href = apiUrl(link.getAttribute("href"));
    link.target = "_blank";
    link.rel = "noopener noreferrer";
  }
}

const chips = (items) => h("ul", { class: "chips" }, items.map((item) => h("li", {}, item)));
const bullets = (items) => h("ul", { class: "bullets" }, items.map((item) => h("li", {}, item)));

/* ---------- Renderers ---------- */

function renderHero(profile) {
  $("#hero-location").textContent = profile.location;
  $("#hero-name").textContent = profile.name;
  $("#hero-headline").textContent = profile.headline;
  $("#hero-summary").textContent = profile.summary;
  $("#footer-name").textContent = profile.name;

  // Static copy only: point visitors to the repository that holds the real API.
  const github = profile.links.find((link) => link.label === "GitHub");
  if (github) $("#repo-link").href = `${github.url.replace(/\/$/, "")}/kennys-portfolio`;

  $("#hero-links").replaceChildren(
    ...profile.links.map((link) =>
      h("a", { class: "btn", href: link.url, target: "_blank", rel: "noopener noreferrer" }, link.label)),
    h("a", { class: "btn btn-ghost", href: `mailto:${profile.email}` }, "Email me"),
  );
}

function projectCard(project) {
  return h("article", { class: "card" },
    h("p", { class: "eyebrow" }, project.context),
    h("h3", {}, project.title),
    h("p", { class: "problem" }, h("strong", {}, "Problem: "), project.problem),
    bullets(project.highlights),
    chips(project.technologies),
  );
}

function renderExperience(entries) {
  $("#experience-list").replaceChildren(
    ...entries.map((entry, index) =>
      h("li", { class: "timeline-item" },
        h("p", { class: "eyebrow" }, entry.period),
        h("h3", {}, entry.role),
        h("p", { class: "where" }, entry.projectName ? `${entry.company} · ${entry.projectName}` : entry.company),
        h("details", { open: index === 0 },
          h("summary", {}, "Highlights & technologies"),
          bullets(entry.highlights),
          chips(entry.technologies),
        ),
      )),
  );
}

function renderSkills(groups) {
  $("#skills-list").replaceChildren(
    ...groups.map((group) => h("div", { class: "card" }, h("h3", {}, group.category), chips(group.items))),
  );
}

function renderLearning({ education, certifications, courses, languages }) {
  $("#education-list").replaceChildren(
    ...education.map((e) => h("li", {}, e.program, h("small", {}, `${e.institution} · ${e.period}`))),
  );
  $("#certifications-list").replaceChildren(
    ...certifications.map((c) => h("li", {}, c.name, h("small", {}, `${c.issuer} · ${c.year}`))),
  );
  $("#courses-list").replaceChildren(...courses.map((course) => h("li", {}, course)));
  $("#languages-line").textContent =
    "Languages: " + languages.map((l) => `${l.language} (${l.level})`).join(" · ");
}

/* ---------- Project filter (calls GET /api/projects?technology=…) ---------- */

const FILTERS = ["All", "Azure", "AWS", "MongoDB", "SQL"];

async function loadProjects(technology) {
  const url = technology ? `api/projects?technology=${encodeURIComponent(technology)}` : "api/projects";
  const projects = await getJson(url);

  $("#api-call").textContent = staticData
    ? `Static copy · same filter as GET /${url}`
    : `GET ${API_BASE || "/"}${url}`;
  $("#projects-list").replaceChildren(
    ...(projects.length
      ? projects.map(projectCard)
      : [h("p", { class: "section-note" }, "No projects match this filter.")]),
  );
}

function renderFilters() {
  const container = $("#project-filters");
  const buttons = FILTERS.map((label, index) =>
    h("button", { type: "button", "aria-pressed": index === 0 ? "true" : "false" }, label));

  for (const button of buttons) {
    button.addEventListener("click", async () => {
      buttons.forEach((b) => b.setAttribute("aria-pressed", String(b === button)));
      try {
        await loadProjects(button.textContent === "All" ? null : button.textContent);
      } catch (error) {
        showError(error);
      }
    });
  }

  container.replaceChildren(...buttons);
}

/* ---------- Boot ---------- */

function showError(error) {
  console.error(error);
  const box = $("#app-error");
  box.textContent = "Couldn't load the portfolio data from the API. Please try again in a moment.";
  box.hidden = false;
}

async function init() {
  wireEndpointLinks();

  try {
    await detectMode();

    const [profile, experience, skills, education, certifications, courses, languages] = await Promise.all([
      getJson("api/profile"),
      getJson("api/experience"),
      getJson("api/skills"),
      getJson("api/education"),
      getJson("api/certifications"),
      getJson("api/courses"),
      getJson("api/languages"),
    ]);

    renderHero(profile);
    renderExperience(experience);
    renderSkills(skills);
    renderLearning({ education, certifications, courses, languages });
    renderFilters();
    await loadProjects(null);
  } catch (error) {
    showError(error);
  }
}

init();
