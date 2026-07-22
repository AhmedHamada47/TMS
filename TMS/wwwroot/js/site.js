(function () {
  "use strict";

  var body = document.body;
  var root = document.documentElement;

  /* ---------- Dark mode ---------- */
  var THEME_KEY = "taskflow-theme";
  var darkToggle = document.getElementById("darkModeToggle");

  function applyTheme(theme) {
    if (theme === "dark") {
      root.setAttribute("data-theme", "dark");
    } else {
      root.removeAttribute("data-theme");
    }
    if (darkToggle) {
      var icon = darkToggle.querySelector("i");
      if (icon) {
        icon.className = theme === "dark" ? "fas fa-sun" : "fas fa-moon";
      }
    }
  }

  var savedTheme = localStorage.getItem(THEME_KEY);
  if (!savedTheme) {
    savedTheme = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light";
  }
  applyTheme(savedTheme);

  if (darkToggle) {
    darkToggle.addEventListener("click", function () {
      var current = root.getAttribute("data-theme") === "dark" ? "dark" : "light";
      var next = current === "dark" ? "light" : "dark";
      applyTheme(next);
      localStorage.setItem(THEME_KEY, next);
    });
  }

  /* ---------- Sidebar toggle ---------- */
  var SIDEBAR_KEY = "taskflow-sidebar-collapsed";
  var sidebarToggle = document.getElementById("sidebarToggle");
  var isMobile = function () { return window.innerWidth <= 991.98; };

  // On desktop, restore the collapsed preference. On mobile the sidebar
  // always starts closed (the class is instead used as an "open" flag).
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

  // Close the mobile drawer when clicking outside of it.
  document.addEventListener("click", function (e) {
    if (!isMobile()) return;
    var sidebar = document.querySelector(".sidebar");
    if (!sidebar || !body.classList.contains("sidebar-collapsed")) return;
    if (sidebar.contains(e.target) || e.target.closest("#sidebarToggle")) return;
    body.classList.remove("sidebar-collapsed");
  });

  // Keep behaviour sane when resizing across the mobile breakpoint.
  var lastIsMobile = isMobile();
  window.addEventListener("resize", function () {
    var nowMobile = isMobile();
    if (nowMobile !== lastIsMobile) {
      body.classList.remove("sidebar-collapsed");
      if (!nowMobile && localStorage.getItem(SIDEBAR_KEY) === "1") {
        body.classList.add("sidebar-collapsed");
      }
      lastIsMobile = nowMobile;
    }
  });
})();
