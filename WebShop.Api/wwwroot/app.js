const API_BASE = "";
const USER_STORAGE_KEY = "webshopUser";


const state = {
  user: null,
  allProducts: [],
  products: [],
  categories: [],
  featuredIndex: 0,
  cart: [],
  cartExpiresAt: null,
  selectedProduct: null,
  activeCategoryChipId: null,
  activeCollection: null
};

const els = {
  statusText: document.getElementById("statusText"),
  authGate: document.getElementById("authGate"),
  shopShell: document.getElementById("shopShell"),
  productGrid: document.getElementById("productGrid"),
  featuredContent: document.getElementById("featuredContent"),
  categorySelect: document.getElementById("categorySelect"),
  searchInput: document.getElementById("searchInput"),
  minPriceInput: document.getElementById("minPriceInput"),
  maxPriceInput: document.getElementById("maxPriceInput"),
  cartDrawer: document.getElementById("cartDrawer"),
  cartItems: document.getElementById("cartItems"),
  cartTotal: document.getElementById("cartTotal"),
  cartStatusText: document.getElementById("cartStatusText"),
  cartExpiryText: document.getElementById("cartExpiryText"),
  clearCartBtn: document.getElementById("clearCartBtn"),
  shippingInput: document.getElementById("shippingInput"),
  discountInput: document.getElementById("discountInput"),
  emailInput: document.getElementById("emailInput"),
  passwordInput: document.getElementById("passwordInput"),
  productDialog: document.getElementById("productDialog"),
  dialogTitle: document.getElementById("dialogTitle"),
  dialogMeta: document.getElementById("dialogMeta"),
  dialogDescription: document.getElementById("dialogDescription"),
  reviewList: document.getElementById("reviewList"),
  homeView: document.getElementById("homeView"),
  productView: document.getElementById("productView"),
  routeProductThumb: document.getElementById("routeProductThumb"),
  routeProductBrand: document.getElementById("routeProductBrand"),
  routeProductTitle: document.getElementById("routeProductTitle"),
  routeProductMeta: document.getElementById("routeProductMeta"),
  routeProductDescription: document.getElementById("routeProductDescription"),
  routeReviewList: document.getElementById("routeReviewList"),
  routeRecoGrid: document.getElementById("routeRecoGrid"),
  categoryChips: document.getElementById("categoryChips"),
  gateEmailInput: document.getElementById("gateEmailInput"),
  gatePasswordInput: document.getElementById("gatePasswordInput"),
  gateStatusText: document.getElementById("gateStatusText")
};

bindEvents();
initialize();

async function initialize() {
  try {
    const savedUser = localStorage.getItem(USER_STORAGE_KEY);
    if (savedUser) {
      state.user = JSON.parse(savedUser);
      els.emailInput.value = state.user.email;
      els.gateEmailInput.value = state.user.email;
      showShop();
    } else {
      showGate();
    }

    await Promise.all([loadCategories(), loadProducts()]);

    if (state.user) {
      await loadCartFromApi();
    } else {
      renderCart();
    }

    renderFeatured();
    applyRoute();
    setStatus(`Loaded ${state.products.length} products.`);
  } catch (error) {
    localStorage.removeItem(USER_STORAGE_KEY);
    state.user = null;
    showGate();
    setStatus(`Startup failed: ${error.message}`);
  }
}

function bindEvents() {
  document.getElementById("searchBtn").addEventListener("click", onSearch);
  document.getElementById("resetBtn").addEventListener("click", resetFilters);

  document.getElementById("featuredPrevBtn").addEventListener("click", () => rotateFeatured(-1));
  document.getElementById("featuredNextBtn").addEventListener("click", () => rotateFeatured(1));
  document.getElementById("featuredOpenBtn").addEventListener("click", () => {
    const featured = getFeaturedProduct();
    if (featured) navigateToProduct(featured.id);
  });

  document.getElementById("openCartBtn").addEventListener("click", toggleCart);
  document.getElementById("closeCartBtn").addEventListener("click", toggleCart);
  document.getElementById("navOpenCart").addEventListener("click", (event) => {
    event.preventDefault();
    toggleCart();
  });

  document.getElementById("shopNowBtn").addEventListener("click", () => {
    document.getElementById("catalog").scrollIntoView({ behavior: "smooth", block: "start" });
  });
  document.getElementById("checkoutBtn").addEventListener("click", onCheckout);
  els.clearCartBtn.addEventListener("click", clearCart);

  document.getElementById("loginBtn").addEventListener("click", () => onLogin(false));
  document.getElementById("registerBtn").addEventListener("click", () => onRegister(false));
  document.getElementById("logoutBtn").addEventListener("click", onLogout);
  document.getElementById("gateLoginBtn").addEventListener("click", () => onLogin(true));
  document.getElementById("gateRegisterBtn").addEventListener("click", () => onRegister(true));

  document.getElementById("dialogCloseBtn").addEventListener("click", () => els.productDialog.close());
  document.getElementById("dialogAddBtn").addEventListener("click", async () => {
    if (!state.selectedProduct) return;
    await addToCart(state.selectedProduct);
  });

  document.getElementById("productBackBtn").addEventListener("click", () => navigateHome());
  document.getElementById("routeAddToCartBtn").addEventListener("click", async () => {
    if (!state.selectedProduct) return;
    await addToCart(state.selectedProduct);
  });
  document.getElementById("routeOpenCartBtn").addEventListener("click", toggleCart);

  document.getElementById("clearChipFilterBtn").addEventListener("click", () => {
    state.activeCategoryChipId = null;
    applyChipFilter();
  });

  for (const link of document.querySelectorAll("a[data-route='home']")) {
    link.addEventListener("click", (event) => {
      event.preventDefault();
      navigateHome();
    });
  }

  for (const card of document.querySelectorAll("button[data-collection]")) {
    card.addEventListener("click", () => {
      const collection = card.dataset.collection;
      state.activeCollection = state.activeCollection === collection ? null : collection;
      applyCollectionFilter();
    });
  }

  window.addEventListener("popstate", applyRoute);

  setInterval(() => {
    if (isHomeRoute()) {
      rotateFeatured(1);
    }
  }, 5000);

  setInterval(renderCartExpiry, 60000);
}

async function loadCategories() {
  const response = await fetch(`${API_BASE}/categories`);
  if (!response.ok) throw new Error("Cannot load categories");
  state.categories = await response.json();

  const options = ['<option value="">All categories</option>'];
  for (const category of state.categories) {
    options.push(`<option value="${category.id}">${escapeHtml(category.name)}</option>`);
  }
  els.categorySelect.innerHTML = options.join("");

  renderCategoryChips();
}

function renderCategoryChips() {
  els.categoryChips.innerHTML = state.categories
    .map((category) => {
      const active = state.activeCategoryChipId === category.id ? "active" : "";
      return `<button class="chip ${active}" data-chip="${category.id}">${escapeHtml(category.name)}</button>`;
    })
    .join("");

  for (const chip of document.querySelectorAll("button[data-chip]")) {
    chip.addEventListener("click", () => {
      const id = Number(chip.dataset.chip);
      state.activeCategoryChipId = state.activeCategoryChipId === id ? null : id;
      applyChipFilter();
    });
  }
}

async function loadProducts() {
  const response = await fetch(`${API_BASE}/products`);
  if (!response.ok) throw new Error("Cannot load products");
  state.allProducts = await response.json();
  state.products = [...state.allProducts];
  renderProducts();
}

async function onSearch() {
  const payload = {
    searchTerm: normalizeOrNull(els.searchInput.value),
    categoryId: normalizeCategoryId(els.categorySelect.value),
    minPrice: normalizeNumberOrNull(els.minPriceInput.value),
    maxPrice: normalizeNumberOrNull(els.maxPriceInput.value)
  };

  const response = await fetch(`${API_BASE}/products/search`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    setStatus("Search failed.");
    return;
  }

  state.products = await response.json();
  renderProducts();
  renderFeatured();
  setStatus(`Found ${state.products.length} products.`);
}

function resetFilters() {
  els.searchInput.value = "";
  els.minPriceInput.value = "";
  els.maxPriceInput.value = "";
  els.categorySelect.value = "";
  state.activeCategoryChipId = null;
  state.activeCollection = null;
  state.products = [...state.allProducts];
  renderCategoryChips();
  renderProducts();
  renderFeatured();
  setStatus(`Loaded ${state.products.length} products.`);
}

function applyChipFilter() {
  renderCategoryChips();
  state.activeCollection = null;
  if (!state.activeCategoryChipId) {
    state.products = [...state.allProducts];
  } else {
    state.products = state.allProducts.filter((product) => product.categoryId === state.activeCategoryChipId);
  }

  renderProducts();
  renderFeatured();
  setStatus(`Showing ${state.products.length} products.`);
}

function applyCollectionFilter() {
  if (!state.activeCollection) {
    state.products = [...state.allProducts];
  } else if (state.activeCollection === "new") {
    const year = new Date().getFullYear() - 1;
    state.products = state.allProducts.filter((product) => product.releaseYear && product.releaseYear >= year);
  } else if (state.activeCollection === "budget") {
    state.products = state.allProducts.filter((product) => product.price <= 40);
  } else if (state.activeCollection === "low-stock") {
    state.products = state.allProducts.filter((product) => product.stock > 0 && product.stock <= 25);
  } else if (state.activeCollection === "premium") {
    state.products = state.allProducts.filter((product) => product.price >= 180);
  }

  renderProducts();
  renderFeatured();
  setStatus(`Collection: ${state.activeCollection || "all"} (${state.products.length} products).`);
}

function renderProducts() {
  if (state.products.length === 0) {
    els.productGrid.innerHTML = "<p>No products found.</p>";
    return;
  }

  els.productGrid.innerHTML = state.products
    .map((product) => {
      const thumbColor = colorForCategory(product.categoryId);
      const badges = buildBadges(product)
        .map((badge) => `<span class="badge ${badge.className}">${badge.text}</span>`)
        .join("");
      const stockClass = product.stock > 0 ? "stock" : "stock oos";

      return `
        <article class="product-card">
          <div class="product-thumb" style="background:${thumbColor}">${buildInitials(product.name)}</div>
          <div class="product-body">
            <div class="badges">${badges}</div>
            <h4 class="product-title">${escapeHtml(product.name)}</h4>
            <div class="product-meta">
              <span class="price">EUR ${product.price.toFixed(2)}</span>
              <span class="${stockClass}">${product.stock > 0 ? `${product.stock} in stock` : "Out of stock"}</span>
            </div>
            <div class="card-actions">
              <button class="btn btn-soft" data-view="${product.id}">View</button>
              <button class="btn btn-primary" data-add="${product.id}">Add</button>
            </div>
          </div>
        </article>`;
    })
    .join("");

  for (const button of document.querySelectorAll("button[data-view]")) {
    button.addEventListener("click", () => {
      const product = findProductById(Number(button.dataset.view));
      if (product) {
        openProductDialog(product);
      }
    });
  }

  for (const button of document.querySelectorAll("button[data-add]")) {
    button.addEventListener("click", async () => {
      const product = findProductById(Number(button.dataset.add));
      if (product) {
        await addToCart(product);
      }
    });
  }
}

function renderFeatured() {
  const featured = getFeaturedProduct();
  if (!featured) {
    els.featuredContent.innerHTML = "No featured product available.";
    return;
  }

  els.featuredContent.innerHTML = `
    <h4>${escapeHtml(featured.name)}</h4>
    <p>${escapeHtml(featured.brand || "NovaMarket")} | EUR ${featured.price.toFixed(2)} | Stock ${featured.stock}</p>
    <p>${escapeHtml(featured.description || "No description")}</p>
  `;
}

function rotateFeatured(direction) {
  if (state.products.length === 0) return;
  state.featuredIndex = (state.featuredIndex + direction + state.products.length) % state.products.length;
  renderFeatured();
}

function getFeaturedProduct() {
  if (state.products.length === 0) return null;
  return state.products[state.featuredIndex];
}

function navigateHome() {
  history.pushState({}, "", "/");
  applyRoute();
}

function navigateToProduct(productId) {
  history.pushState({}, "", `/product/${productId}`);
  applyRoute();
}

function isHomeRoute() {
  return !location.pathname.startsWith("/product/");
}

async function applyRoute() {
  const path = location.pathname;
  if (!path.startsWith("/product/")) {
    els.homeView.classList.remove("hidden");
    els.productView.classList.add("hidden");
    return;
  }

  const productId = Number(path.split("/").pop());
  const product = findProductById(productId);
  if (!product) {
    navigateHome();
    return;
  }

  await renderProductRoute(product);
}

async function renderProductRoute(product) {
  state.selectedProduct = product;
  els.homeView.classList.add("hidden");
  els.productView.classList.remove("hidden");

  els.routeProductThumb.style.background = colorForCategory(product.categoryId);
  els.routeProductThumb.textContent = buildInitials(product.name);
  els.routeProductBrand.textContent = product.brand || "NovaMarket";
  els.routeProductTitle.textContent = product.name;
  els.routeProductMeta.textContent = `EUR ${product.price.toFixed(2)} | Stock ${product.stock}`;
  els.routeProductDescription.textContent = product.description || "No description";

  await loadRouteReviews(product.id);
  renderRouteRecommendations(product);
}

async function loadRouteReviews(productId) {
  els.routeReviewList.innerHTML = "Loading reviews...";
  try {
    const response = await fetch(`${API_BASE}/products/${productId}/reviews`);
    if (!response.ok) throw new Error("Cannot load reviews");

    const reviews = await response.json();
    if (!reviews.length) {
      els.routeReviewList.innerHTML = "<p>No reviews yet.</p>";
      return;
    }

    els.routeReviewList.innerHTML = reviews.slice(0, 8).map((review) => `
      <div class="review-item">
        <strong>${escapeHtml(review.userEmail)}</strong> · ${review.stars}/5
        <p>${escapeHtml(review.explanation)}</p>
      </div>`).join("");
  } catch {
    els.routeReviewList.innerHTML = "<p>Could not load reviews.</p>";
  }
}

function renderRouteRecommendations(product) {
  const recommendations = state.allProducts
    .filter((candidate) => candidate.id !== product.id)
    .filter((candidate) => candidate.categoryId === product.categoryId || Math.abs(candidate.price - product.price) <= 30)
    .slice(0, 8);

  if (!recommendations.length) {
    els.routeRecoGrid.innerHTML = "<p>No recommendations available.</p>";
    return;
  }

  els.routeRecoGrid.innerHTML = recommendations
    .map((item) => {
      return `
        <article class="product-card">
          <div class="product-thumb" style="background:${colorForCategory(item.categoryId)}">${buildInitials(item.name)}</div>
          <div class="product-body">
            <h4 class="product-title">${escapeHtml(item.name)}</h4>
            <div class="product-meta">
              <span class="price">EUR ${item.price.toFixed(2)}</span>
              <span class="${item.stock > 0 ? "stock" : "stock oos"}">${item.stock > 0 ? `${item.stock} in stock` : "Out of stock"}</span>
            </div>
            <div class="card-actions">
              <button class="btn btn-soft" data-route-view="${item.id}">Open</button>
            </div>
          </div>
        </article>`;
    })
    .join("");

  for (const button of document.querySelectorAll("button[data-route-view]")) {
    button.addEventListener("click", () => {
      navigateToProduct(Number(button.dataset.routeView));
    });
  }
}

async function openProductDialog(product) {
  state.selectedProduct = product;
  els.dialogTitle.textContent = product.name;
  els.dialogMeta.textContent = `EUR ${product.price.toFixed(2)} | ${product.brand || "NovaMarket"} | Stock ${product.stock}`;
  els.dialogDescription.textContent = product.description || "No description";
  els.reviewList.innerHTML = "Loading reviews...";
  els.productDialog.showModal();

  try {
    const response = await fetch(`${API_BASE}/products/${product.id}/reviews`);
    if (!response.ok) throw new Error("Cannot load reviews");
    const reviews = await response.json();
    if (!reviews.length) {
      els.reviewList.innerHTML = "<p>No reviews yet.</p>";
      return;
    }

    els.reviewList.innerHTML = reviews.slice(0, 8).map((review) => `
      <div class="review-item">
        <strong>${escapeHtml(review.userEmail)}</strong> · ${review.stars}/5
        <p>${escapeHtml(review.explanation)}</p>
      </div>`).join("");
  } catch {
    els.reviewList.innerHTML = "<p>Could not load reviews.</p>";
  }
}

async function loadCartFromApi() {
  if (!state.user) {
    state.cart = [];
    state.cartExpiresAt = null;
    renderCart();
    return;
  }

  const response = await fetch(`${API_BASE}/cart/${state.user.userId}`);
  if (!response.ok) {
    state.cart = [];
    state.cartExpiresAt = null;
    renderCart();
    return;
  }

  const cart = await response.json();
  state.cartExpiresAt = cart.expiresInSeconds
    ? Date.now() + cart.expiresInSeconds * 1000
    : null;
  state.cart = cart.items.map((item) => ({
    id: item.productId,
    name: item.productName,
    price: item.unitPrice,
    quantity: item.quantity
  }));

  renderCart();
}

async function addToCart(product) {
  if (!state.user) {
    setStatus("Sign in first to add items to your cart.");
    return false;
  }

  const response = await fetch(`${API_BASE}/cart/items`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId: state.user.userId, productId: product.id, quantity: 1 })
  });

  if (!response.ok) {
    const error = await response.json().catch(() => null);
    setCartStatus(error?.message || "Could not add item to cart.");
    await loadCartFromApi();
    return false;
  }

  await loadCartFromApi();
  setCartStatus(`Added ${product.name} to cart.`);
  return true;
}

async function removeOneFromCart(productId) {
  if (!state.user) {
    setCartStatus("Sign in first to edit your cart.");
    return;
  }

  const response = await fetch(`${API_BASE}/cart/items/${productId}?userId=${state.user.userId}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    const error = await response.json().catch(() => null);
    setCartStatus(error?.message || "Could not remove item.");
    await loadCartFromApi();
    return;
  }

  await loadCartFromApi();
  setCartStatus("Removed 1 item.");
}

async function clearCart() {
  if (!state.user) {
    state.cart = [];
    state.cartExpiresAt = null;
    renderCart();
    return;
  }

  const response = await fetch(`${API_BASE}/cart/${state.user.userId}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    const error = await response.json().catch(() => null);
    setCartStatus(error?.message || "Could not clear cart.");
    return;
  }

  state.cart = [];
  state.cartExpiresAt = null;
  renderCart();
  setCartStatus("Cart cleared.");
}

function renderCart() {
  renderCartExpiry();

  if (state.cart.length === 0) {
    els.cartItems.innerHTML = "<p>Your cart is empty.</p>";
    els.cartTotal.textContent = "EUR 0.00";
    return;
  }

  let total = 0;
  els.cartItems.innerHTML = state.cart.map((item) => {
    const lineTotal = item.price * item.quantity;
    total += lineTotal;
    return `
      <div class="cart-item">
        <div class="cart-item-top">
          <strong>${escapeHtml(item.name)}</strong>
          <button class="btn btn-ghost" data-remove-cart="${item.id}" title="Remove one">-1</button>
        </div>
        <div>${item.quantity} x EUR ${item.price.toFixed(2)} = EUR ${lineTotal.toFixed(2)}</div>
      </div>`;
  }).join("");

  for (const button of document.querySelectorAll("button[data-remove-cart]")) {
    button.addEventListener("click", async () => {
      await removeOneFromCart(Number(button.dataset.removeCart));
    });
  }

  els.cartTotal.textContent = `EUR ${total.toFixed(2)}`;
}

async function onCheckout() {
  if (!state.user) {
    setStatus("Sign in first to checkout against API.");
    return;
  }

  const shippingAddress = normalizeOrNull(els.shippingInput.value);
  if (!shippingAddress) {
    setStatus("Shipping address is required.");
    return;
  }

  const payload = {
    userId: state.user.userId,
    shippingAddress,
    discountCode: normalizeOrNull(els.discountInput.value),
    shippingAddressId: null
  };

  const response = await fetch(`${API_BASE}/orders/checkout`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    setStatus("Checkout failed. Ensure your API cart has items and address is valid.");
    return;
  }

  state.cart = [];
  state.cartExpiresAt = null;
  renderCart();
  els.shippingInput.value = "";
  els.discountInput.value = "";
  setCartStatus("Checkout complete. Cart cleared.");
}

async function onLogin(useGate) {
  const email = (useGate ? els.gateEmailInput.value : els.emailInput.value).trim();
  const password = useGate ? els.gatePasswordInput.value : els.passwordInput.value;

  const payload = {
    email,
    password
  };

  const response = await fetch(`${API_BASE}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    setStatus("Login failed.");
    setGateStatus("Login failed. Check email/password.");
    return;
  }

  state.user = await response.json();
  localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(state.user));

  els.emailInput.value = email;
  els.gateEmailInput.value = email;
  els.passwordInput.value = password;
  els.gatePasswordInput.value = password;
  showShop();
  await loadCartFromApi();
  setStatus(`Signed in as ${state.user.email}.`);
  setGateStatus("Signed in.");
}

async function onRegister(useGate) {
  const email = (useGate ? els.gateEmailInput.value : els.emailInput.value).trim();
  const password = useGate ? els.gatePasswordInput.value : els.passwordInput.value;

  const payload = {
    email,
    password,
    firstName: null,
    lastName: null
  };

  const response = await fetch(`${API_BASE}/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    setStatus("Register failed.");
    setGateStatus("Register failed. Use a valid email/password.");
    return;
  }

  setStatus("Registration successful. You can sign in now.");
  setGateStatus("Registration successful. Sign in to continue.");
}

function onLogout() {
  localStorage.removeItem(USER_STORAGE_KEY);
  state.user = null;
  state.cart = [];
  state.cartExpiresAt = null;

  els.passwordInput.value = "";
  els.gatePasswordInput.value = "";

  renderCart();
  showGate();
  setStatus("Signed out.");
  setGateStatus("Signed out. Sign in to enter the webshop.");
}

function toggleCart() {
  els.cartDrawer.classList.toggle("open");
}

function normalizeNumberOrNull(value) {
  if (!value || value.trim() === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function normalizeCategoryId(value) {
  if (!value) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function normalizeOrNull(value) {
  const normalized = (value || "").trim();
  return normalized === "" ? null : normalized;
}

function findProductById(id) {
  return state.allProducts.find((product) => product.id === id);
}

function setStatus(text) {
  els.statusText.textContent = text;
}

function setGateStatus(text) {
  els.gateStatusText.textContent = text;
}

function setCartStatus(text) {
  els.cartStatusText.textContent = text;
}

function renderCartExpiry() {
  if (!state.cartExpiresAt || state.cart.length === 0) {
    els.cartExpiryText.textContent = "";
    return;
  }

  const remainingMs = Math.max(0, state.cartExpiresAt - Date.now());
  const totalMinutes = Math.ceil(remainingMs / 60000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours > 0) {
    els.cartExpiryText.textContent = `Saved for ${hours}h ${minutes}m`;
  } else {
    els.cartExpiryText.textContent = `Saved for ${minutes}m`;
  }
}

function showShop() {
  els.authGate.classList.add("hidden");
  els.shopShell.classList.remove("hidden");
  applyRoute();
}

function showGate() {
  els.shopShell.classList.add("hidden");
  els.authGate.classList.remove("hidden");
}

function buildInitials(name) {
  const parts = name.split(" ").filter(Boolean);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

function colorForCategory(categoryId) {
  const palette = [
    "linear-gradient(135deg,#2a9d8f,#59c8b5)",
    "linear-gradient(135deg,#e76f51,#f4a261)",
    "linear-gradient(135deg,#457b9d,#5fa8d3)",
    "linear-gradient(135deg,#7b8f48,#aac76a)",
    "linear-gradient(135deg,#bc6c25,#dda15e)",
    "linear-gradient(135deg,#3a86ff,#70a7ff)"
  ];

  return palette[categoryId % palette.length];
}

function buildBadges(product) {
  const list = [];
  if (product.releaseYear && product.releaseYear >= new Date().getFullYear() - 1) {
    list.push({ text: "NEW", className: "new" });
  }
  if (product.price <= 40) {
    list.push({ text: "DEAL", className: "deal" });
  }
  if (product.stock > 0 && product.stock <= 25) {
    list.push({ text: "LOW", className: "low" });
  }
  if (!list.length) {
    list.push({ text: "POPULAR", className: "pop" });
  }
  return list;
}

function escapeHtml(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
