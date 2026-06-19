// Commerce Catalog console. Talks to the API on the same origin by default; override via the API base box.
(() => {
  "use strict";

  const $ = (id) => document.getElementById(id);
  const storedBase = localStorage.getItem("apiBase") || "";
  const baseInput = $("apiBase");
  baseInput.value = storedBase;

  const apiBase = () => (baseInput.value || window.location.origin).replace(/\/+$/, "");

  const fmt = (amount, currency) =>
    new Intl.NumberFormat("en-IE", { style: "currency", currency: currency || "EUR" }).format(Number(amount));

  function toast(message, isError) {
    const el = $("toast");
    el.textContent = message;
    el.className = "toast show" + (isError ? " error" : "");
    setTimeout(() => (el.className = "toast"), 2600);
  }

  function renderRows(products) {
    const tbody = $("rows");
    tbody.innerHTML = "";
    for (const p of products) {
      const tr = document.createElement("tr");
      tr.dataset.testid = "product-row";
      tr.dataset.id = p.id;
      tr.dataset.sku = p.sku;
      tr.innerHTML = `
        <td class="sku">${p.sku}</td>
        <td class="name">${escapeHtml(p.name)}</td>
        <td class="price" data-role="price">${fmt(p.price, p.currency)}</td>
        <td>
          <span class="reprice">
            <input type="number" step="0.01" min="0" class="row-price-input" aria-label="New price for ${p.sku}" />
            <button class="btn btn-ghost row-price-btn">Set</button>
          </span>
        </td>`;
      tr.querySelector(".row-price-btn").addEventListener("click", () => reprice(p.id, tr));
      tbody.appendChild(tr);
    }
    $("count").textContent = `${products.length} product${products.length === 1 ? "" : "s"}`;
  }

  function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, (c) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
  }

  async function loadProducts() {
    try {
      const res = await fetch(`${apiBase()}/api/products?take=50`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      renderRows(await res.json());
    } catch (err) {
      toast(`Could not load catalog: ${err.message}`, true);
    }
  }

  async function addProduct() {
    const body = {
      sku: $("sku").value.trim(),
      name: $("name").value.trim(),
      description: null,
      price: Number($("price").value),
      currency: "EUR",
      supplierId: $("supplierId").value.trim()
    };
    if (!body.sku || !body.name || Number.isNaN(body.price)) {
      toast("SKU, name, and price are required.", true);
      return;
    }
    try {
      const res = await fetch(`${apiBase()}/api/products`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
      });
      if (res.status === 201) {
        $("sku").value = "";
        $("name").value = "";
        $("price").value = "";
        toast(`Added ${body.sku}.`);
        await loadProducts();
      } else {
        const problem = await res.json().catch(() => ({}));
        toast(problem.detail || `Create failed (HTTP ${res.status}).`, true);
      }
    } catch (err) {
      toast(`Create failed: ${err.message}`, true);
    }
  }

  async function reprice(id, row) {
    const input = row.querySelector(".row-price-input");
    const price = Number(input.value);
    if (Number.isNaN(price) || price < 0) {
      toast("Enter a valid price.", true);
      return;
    }
    try {
      const res = await fetch(`${apiBase()}/api/products/${id}/price`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ price })
      });
      if (res.status === 204) {
        input.value = "";
      } else {
        toast(`Reprice failed (HTTP ${res.status}).`, true);
      }
    } catch (err) {
      toast(`Reprice failed: ${err.message}`, true);
    }
  }

  function onPriceChanged(change) {
    // Update the matching row and flash it.
    const row = document.querySelector(`tr[data-id="${change.productId}"]`);
    if (row) {
      const cell = row.querySelector('[data-role="price"]');
      if (cell) cell.textContent = fmt(change.newAmount, change.currency);
      row.classList.remove("flash");
      void row.offsetWidth;
      row.classList.add("flash");
    }
    prependEvent(change);
  }

  function prependEvent(change) {
    const feed = $("feed");
    const empty = feed.querySelector(".empty");
    if (empty) empty.remove();

    const rose = Number(change.newAmount) >= Number(change.oldAmount);
    const el = document.createElement("div");
    el.className = "event";
    el.innerHTML = `
      <span class="sku">${change.sku}</span>
      <span class="delta ${rose ? "up" : "down"}">${fmt(change.oldAmount, change.currency)} -> ${fmt(change.newAmount, change.currency)}</span>
      <span class="ts">${new Date(change.occurredAt || Date.now()).toLocaleTimeString()}</span>`;
    feed.prepend(el);
    while (feed.children.length > 25) feed.lastChild.remove();
  }

  function setStatus(live) {
    const status = $("status");
    status.className = "status" + (live ? " live" : "");
    $("statusText").textContent = live ? "live" : "offline";
  }

  async function connectRealtime() {
    if (!window.signalR) return;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiBase()}/hubs/prices`)
      .withAutomaticReconnect()
      .build();

    connection.on("priceChanged", onPriceChanged);
    connection.onreconnected(() => setStatus(true));
    connection.onclose(() => setStatus(false));

    try {
      await connection.start();
      setStatus(true);
    } catch {
      setStatus(false);
      setTimeout(connectRealtime, 4000);
    }
  }

  $("add").addEventListener("click", addProduct);
  baseInput.addEventListener("change", () => {
    localStorage.setItem("apiBase", baseInput.value.trim());
    location.reload();
  });

  loadProducts();
  connectRealtime();
})();
