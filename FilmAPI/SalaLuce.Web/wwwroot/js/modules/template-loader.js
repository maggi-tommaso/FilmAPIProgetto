export async function loadLayout({ header, footer, navKey }) {
  const [headerHtml, footerHtml] = await Promise.all([
    fetch(`/components/${header}`).then((response) => response.text()),
    fetch(`/components/${footer}`).then((response) => response.text())
  ]);

  const headerRoot = document.getElementById("layout-header");
  const footerRoot = document.getElementById("layout-footer");

  if (headerRoot) {
    headerRoot.innerHTML = headerHtml;
  }

  if (footerRoot) {
    footerRoot.innerHTML = footerHtml;
  }

  document.dispatchEvent(new CustomEvent("salaluce:layout-loaded", { detail: { navKey } }));
}
