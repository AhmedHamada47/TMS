(function () {
  "use strict";

  const body = document.body;
  const root = document.documentElement;

  /* ---------- Dark mode ---------- */
  const THEME_KEY = "taskflow-theme";
  const darkToggle = document.getElementById("darkModeToggle");

  function applyTheme(theme) {
    if (theme === "dark") {
      root.setAttribute("data-theme", "dark");
    } else {
      root.removeAttribute("data-theme");
    }
    if (darkToggle) {
      const icon = darkToggle.querySelector("i");
      if (icon) {
        icon.className = theme === "dark" ? "fas fa-sun" : "fas fa-moon";
      }
    }
  }

  let savedTheme = localStorage.getItem(THEME_KEY);
  if (!savedTheme) {
    savedTheme = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light";
  }
  applyTheme(savedTheme);

  if (darkToggle) {
    darkToggle.addEventListener("click", function () {
      const current = root.getAttribute("data-theme") === "dark" ? "dark" : "light";
      const next = current === "dark" ? "light" : "dark";
      applyTheme(next);
      localStorage.setItem(THEME_KEY, next);
    });
  }

  /* ---------- Sidebar toggle ---------- */
  const SIDEBAR_KEY = "taskflow-sidebar-collapsed";
  const sidebarToggle = document.getElementById("sidebarToggle");
  const isMobile = function () { return window.innerWidth <= 991.98; };

  if (!isMobile() && localStorage.getItem(SIDEBAR_KEY) === "1") {
    body.classList.add("sidebar-collapsed");
  }

  if (sidebarToggle) {
    sidebarToggle.addEventListener("click", function () {
      body.classList.toggle("sidebar-collapsed");
      if (!isMobile()) {
        localStorage.setItem(SIDEBAR_KEY, body.classList.contains("sidebar-collapsed") ? "1" : "0");
      }
    });
  }

  document.addEventListener("click", function (e) {
    if (!isMobile()) return;
    const sidebar = document.querySelector(".sidebar");
    if (!sidebar || !body.classList.contains("sidebar-collapsed")) return;
    if (sidebar.contains(e.target) || e.target.closest("#sidebarToggle")) return;
    body.classList.remove("sidebar-collapsed");
  });

  let lastIsMobile = isMobile();
  window.addEventListener("resize", function () {
    const nowMobile = isMobile();
    if (nowMobile !== lastIsMobile) {
      body.classList.remove("sidebar-collapsed");
      if (!nowMobile && localStorage.getItem(SIDEBAR_KEY) === "1") {
        body.classList.add("sidebar-collapsed");
      }
      lastIsMobile = nowMobile;
    }
  });

  /* ---------- Debounce utility ---------- */
  function debounce(fn, delay) {
    let timer = null;
    return function () {
      const context = this;
      const args = arguments;
      if (timer) clearTimeout(timer);
      timer = setTimeout(function () {
        fn.apply(context, args);
      }, delay);
    };
  }

  /* ---------- Sidebar keyboard accessibility ---------- */
  if (sidebarToggle) {
    sidebarToggle.setAttribute("aria-expanded", body.classList.contains("sidebar-collapsed") ? "false" : "true");
    sidebarToggle.addEventListener("click", function () {
      const expanded = body.classList.contains("sidebar-collapsed") ? "false" : "true";
      sidebarToggle.setAttribute("aria-expanded", expanded);
    });
  }

  /* ---------- Debounce filter form submits on change ---------- */
  document.querySelectorAll(".filter-bar select").forEach(function (sel) {
    sel.addEventListener("change", debounce(function () {
      if (this.form) this.form.submit();
    }, 250));
  });
})();
