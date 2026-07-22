/* =========================================================
   CONFIG
========================================================= */
const CONFIG = {
  API_BASE_URL: "http://localhost:5000/api",
  FETCH_INTERVAL: 10000,
  LOCATION_UPDATE_INTERVAL: 30000,
  DEFAULT_RADIUS_KM: 15,
  DEFAULT_LOCATION: { lat: 24.7136, lng: 46.6753 },
};

/* =========================================================
   STATE
========================================================= */
const state = {
  map: null,
  markers: { driver: null, pickup: null, dropoff: null },
  routeLine: null,
  isOnline: false,
  currentTripId: null,
  currentTrip: null,
  currentLocation: null,
  intervals: { fetch: null, location: null, timer: null },
  tripStartTime: null,
  user: null,
  token: null,
};

/* =========================================================
   INITIALIZATION
========================================================= */
document.addEventListener("DOMContentLoaded", () => {
  checkAuth();
  initMap();
  checkActiveTrip();
  loadDriverStats();
});

/* =========================================================
   AUTH
========================================================= */
function checkAuth() {
  state.token =
    localStorage.getItem("token") || sessionStorage.getItem("token");
  const userStr =
    localStorage.getItem("user") || sessionStorage.getItem("user");

  if (!state.token || !userStr) {
    redirectToLogin();
    return;
  }

  try {
    state.user = JSON.parse(userStr);

    if (state.user.role !== "Driver") {
      redirectToLogin();
      return;
    }

    const driverName = state.user.fullName || "السائق";
    document.getElementById("driverName").textContent = `مرحباً، ${driverName}`;

    if (state.user.rating) {
      document.getElementById(
        "driverRating"
      ).textContent = `⭐ ${state.user.rating.toFixed(1)}`;
    }
  } catch (e) {
    console.error("Error parsing user:", e);
    redirectToLogin();
  }
}

function redirectToLogin() {
  window.location.href = "login.html";
}

function logout() {
  if (confirm("هل أنت متأكد من تسجيل الخروج؟")) {
    localStorage.clear();
    sessionStorage.clear();
    redirectToLogin();
  }
}

/* =========================================================
   API HELPER
========================================================= */
async function apiRequest(endpoint, method = "GET", body = null) {
  const options = {
    method,
    headers: {
      Authorization: `Bearer ${state.token}`,
      "Content-Type": "application/json",
    },
  };

  if (body) {
    options.body = JSON.stringify(body);
  }

  const response = await fetch(`${CONFIG.API_BASE_URL}${endpoint}`, options);

  if (response.status === 401) {
    showToast("انتهت صلاحية الجلسة", "error");
    setTimeout(redirectToLogin, 2000);
    throw new Error("Unauthorized");
  }

  const text = await response.text();
  const data = text ? JSON.parse(text) : {};

  if (!response.ok) {
    throw new Error(data.error || "Server Error");
  }

  return data;
}

/* =========================================================
   MAP
========================================================= */
function initMap() {
  state.map = L.map("map", {
    zoomControl: false,
    attributionControl: false,
  }).setView([CONFIG.DEFAULT_LOCATION.lat, CONFIG.DEFAULT_LOCATION.lng], 13);

  L.tileLayer("https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png", {
    maxZoom: 19,
  }).addTo(state.map);

  L.control.zoom({ position: "bottomleft" }).addTo(state.map);

  getCurrentLocation();
}

/* =========================================================
   LOCATION
========================================================= */
function getCurrentLocation() {
  if (!navigator.geolocation) {
    showToast("المتصفح لا يدعم تحديد الموقع", "error");
    state.currentLocation = CONFIG.DEFAULT_LOCATION;
    updateDriverMarker();
    return;
  }

  navigator.geolocation.getCurrentPosition(
    (pos) => {
      state.currentLocation = {
        lat: pos.coords.latitude,
        lng: pos.coords.longitude,
      };
      state.map.setView(
        [state.currentLocation.lat, state.currentLocation.lng],
        15
      );
      updateDriverMarker();
    },
    (error) => {
      console.error("Geolocation error:", error);
      showToast("تعذر تحديد موقعك", "warning");
      state.currentLocation = CONFIG.DEFAULT_LOCATION;
      updateDriverMarker();
    },
    { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 }
  );
}

function refreshLocation() {
  showToast("جاري تحديث الموقع...", "info");
  getCurrentLocation();
}

function updateDriverMarker() {
  if (!state.currentLocation) return;

  if (state.markers.driver) {
    state.map.removeLayer(state.markers.driver);
  }

  const driverIcon = L.divIcon({
    className: "driver-marker",
    html: '<div style="font-size:30px;">🚗</div>',
    iconSize: [40, 40],
    iconAnchor: [20, 20],
  });

  state.markers.driver = L.marker(
    [state.currentLocation.lat, state.currentLocation.lng],
    { icon: driverIcon }
  ).addTo(state.map);
}

async function updateLocationOnServer() {
  if (!state.currentLocation || !state.isOnline) return;

  const driverId = state.user.driverId || state.user.userId;

  try {
    await apiRequest(`/Drivers/${driverId}/location`, "POST", {
      latitude: state.currentLocation.lat,
      longitude: state.currentLocation.lng,
    });
  } catch (e) {
    console.error("Failed to update location:", e);
  }
}

/* =========================================================
   ONLINE/OFFLINE TOGGLE
========================================================= */
async function toggleOnline() {
  const driverId = state.user.driverId || state.user.userId;

  try {
    if (!state.isOnline) {
      // Go Online
      await apiRequest(`/Drivers/${driverId}/online`, "POST");
      state.isOnline = true;
      showToast("أنت الآن متصل ✓", "success");
      startOnlineMode();
    } else {
      // Go Offline
      await apiRequest(`/Drivers/${driverId}/offline`, "POST");
      state.isOnline = false;
      showToast("أنت غير متصل", "warning");
      stopOnlineMode();
    }

    updateToggleUI();
  } catch (e) {
    console.error("Toggle failed:", e);
    showToast(e.message || "حدث خطأ", "error");
  }
}

function startOnlineMode() {
  fetchRequests();
  state.intervals.fetch = setInterval(fetchRequests, CONFIG.FETCH_INTERVAL);
  state.intervals.location = setInterval(() => {
    getCurrentLocation();
    updateLocationOnServer();
  }, CONFIG.LOCATION_UPDATE_INTERVAL);
}

function stopOnlineMode() {
  clearInterval(state.intervals.fetch);
  clearInterval(state.intervals.location);
  clearRequestsList();
}

function updateToggleUI() {
  const toggle = document.getElementById("onlineToggle");
  const text = document.getElementById("toggleText");

  toggle.classList.toggle("active", state.isOnline);
  text.textContent = state.isOnline ? "متصل" : "غير متصل";
  updateDriverMarker();
}

function clearRequestsList() {
  document.getElementById("requestsList").innerHTML = `
    <div class="empty-state">
      <div class="empty-state-icon">🚗</div>
      <h3>لا توجد طلبات متاحة</h3>
      <p>قم بتفعيل الاتصال لرؤية الطلبات القريبة منك</p>
    </div>
  `;
  document.getElementById("requestsCount").textContent = "0";
}

/* =========================================================
   REQUESTS
========================================================= */
async function fetchRequests() {
  if (!state.currentLocation) {
    console.warn("Location not available");
    return;
  }

  const loading = document.getElementById("requestsLoading");
  if (loading) loading.classList.add("show");

  const driverId = state.user.driverId || state.user.userId;

  try {
    // ✅ المسار الصحيح للـ API
    const data = await apiRequest(
      `/Requests/nearby?driverId=${driverId}&radiusKm=${CONFIG.DEFAULT_RADIUS_KM}`
    );

    if (data.success && data.data) {
      const requests = Array.isArray(data.data) ? data.data : [];
      renderRequests(requests);
      document.getElementById("requestsCount").textContent = requests.length;
    } else {
      clearRequestsList();
    }
  } catch (e) {
    console.error("Failed to fetch requests:", e);
  } finally {
    if (loading) loading.classList.remove("show");
  }
}

function renderRequests(requests) {
  const list = document.getElementById("requestsList");

  if (!requests || requests.length === 0) {
    list.innerHTML = `
      <div class="empty-state">
        <div class="empty-state-icon">📭</div>
        <h3>لا توجد طلبات حالياً</h3>
        <p>ستظهر الطلبات الجديدة هنا تلقائياً</p>
      </div>
    `;
    return;
  }

  list.innerHTML = requests
    .map(
      (r, index) => `
    <div class="request-card" style="animation-delay: ${index * 0.1}s">
      <div class="request-header">
        <span class="request-distance">📍 ${formatDistance(
          r.distanceToPickup
        )}</span>
        <span class="request-time">منذ ${r.minutesSinceCreated || 0} د</span>
      </div>

      ${
        r.estimatedCost
          ? `<div class="request-price">${r.estimatedCost} ر.س</div>`
          : ""
      }

      <div class="location-item">
        <div class="location-dot pickup"></div>
        <span>${formatCoords(r.pickupLatitude, r.pickupLongitude)}</span>
      </div>

      <div class="location-item">
        <div class="location-dot dropoff"></div>
        <span>${formatCoords(r.dropLatitude, r.dropLongitude)}</span>
      </div>

      <div class="request-footer">
        <button class="btn-accept" onclick="acceptRequest(${r.requestId})">
          ✓ قبول الطلب
        </button>
        <button class="btn-reject" onclick="rejectRequest(${r.requestId})">
          ✕
        </button>
      </div>
    </div>
  `
    )
    .join("");
}

function rejectRequest(requestId) {
  const cards = document.querySelectorAll(".request-card");
  cards.forEach((card) => {
    if (card.innerHTML.includes(`acceptRequest(${requestId})`)) {
      card.style.transform = "translateX(100%)";
      card.style.opacity = "0";
      setTimeout(() => card.remove(), 300);
    }
  });
}

/* =========================================================
   TRIP MANAGEMENT
========================================================= */
async function checkActiveTrip() {
  const driverId = state.user?.driverId || state.user?.userId;
  if (!driverId) return;

  try {
    const data = await apiRequest(`/Requests/driver/${driverId}/active`);

    if (data.success && data.data) {
      state.currentTripId = data.data.id;
      state.currentTrip = data.data;
      state.isOnline = true;
      updateToggleUI();
      switchTab("trip");
      displayTripDetails(data.data);
      showTripOnMap(data.data);
      startTripTimer();
    }
  } catch (e) {
    console.log("No active trip");
  }
}

async function acceptRequest(requestId) {
  const driverId = state.user.driverId || state.user.userId;

  try {
    const data = await apiRequest(
      `/Requests/${requestId}/accept?driverId=${driverId}`,
      "POST"
    );

    if (data.success) {
      state.currentTripId = requestId;
      showToast("تم قبول الطلب بنجاح! 🎉", "success");
      switchTab("trip");
      loadTripDetails();
    } else {
      showToast(data.error || "فشل في قبول الطلب", "error");
    }
  } catch (e) {
    console.error("Failed to accept:", e);
    showToast("حدث خطأ أثناء قبول الطلب", "error");
  }
}

async function loadTripDetails() {
  if (!state.currentTripId) return;

  try {
    const data = await apiRequest(`/Requests/${state.currentTripId}/full`);

    if (data.success && data.data) {
      state.currentTrip = data.data;
      displayTripDetails(data.data);
      showTripOnMap(data.data);
      startTripTimer();
    }
  } catch (e) {
    console.error("Failed to load trip:", e);
    showToast("فشل في تحميل تفاصيل الرحلة", "error");
  }
}

function displayTripDetails(trip) {
  document.getElementById("emptyTrip").style.display = "none";
  document.getElementById("tripDetails").classList.add("show");

  // Customer info
  const customerName = trip.customerName || "العميل";
  const customerPhone = trip.customerPhone || "";

  document.getElementById("customerName").textContent = customerName;
  document.getElementById("customerAvatar").textContent =
    customerName.charAt(0);
  document.getElementById("customerPhone").textContent =
    customerPhone || "غير متوفر";

  // Locations
  document.getElementById("pickupAddr").textContent = formatCoords(
    trip.pickupLatitude,
    trip.pickupLongitude
  );
  document.getElementById("dropoffAddr").textContent = formatCoords(
    trip.dropLatitude,
    trip.dropLongitude
  );

  // Price
  const price = trip.estimatedCost || trip.actualCost || 0;
  document.getElementById("tripPrice").textContent = `${price} ر.س`;

  updateTripButtons(trip.status);
}

function updateTripButtons(status) {
  const statusTexts = {
    Accepted: { text: "في الطريق للعميل", color: "#2196F3" },
    Arrived: { text: "وصلت - في انتظار العميل", color: "#FF9800" },
    InProgress: { text: "الرحلة جارية", color: "#4CAF50" },
    Completed: { text: "تمت الرحلة", color: "#4CAF50" },
  };

  const info = statusTexts[status] || { text: status, color: "#2196F3" };

  document.getElementById("tripStatus").textContent = info.text;
  document.getElementById("tripStatus").style.color = info.color;

  // Hide all buttons first
  document.getElementById("arrivedBtn").style.display = "none";
  document.getElementById("startBtn").style.display = "none";
  document.getElementById("completeBtn").style.display = "none";

  // Show appropriate button
  if (status === "Accepted") {
    document.getElementById("arrivedBtn").style.display = "flex";
  } else if (status === "Arrived") {
    document.getElementById("startBtn").style.display = "flex";
  } else if (status === "InProgress") {
    document.getElementById("completeBtn").style.display = "flex";
  }
}

function showTripOnMap(trip) {
  // Clear existing
  if (state.markers.pickup) state.map.removeLayer(state.markers.pickup);
  if (state.markers.dropoff) state.map.removeLayer(state.markers.dropoff);
  if (state.routeLine) state.map.removeLayer(state.routeLine);

  const pickupLat = trip.pickupLatitude || trip.pickupLat;
  const pickupLng = trip.pickupLongitude || trip.pickupLon;
  const dropLat = trip.dropLatitude || trip.dropLat;
  const dropLng = trip.dropLongitude || trip.dropLon;

  // Pickup marker
  state.markers.pickup = L.marker([pickupLat, pickupLng], {
    icon: L.divIcon({
      html: '<div style="font-size:24px;">📍</div>',
      iconSize: [30, 30],
      iconAnchor: [15, 15],
    }),
  }).addTo(state.map);

  // Dropoff marker
  state.markers.dropoff = L.marker([dropLat, dropLng], {
    icon: L.divIcon({
      html: '<div style="font-size:24px;">🏁</div>',
      iconSize: [30, 30],
      iconAnchor: [15, 15],
    }),
  }).addTo(state.map);

  // Route line
  const routeCoords = [
    [
      state.currentLocation?.lat || pickupLat,
      state.currentLocation?.lng || pickupLng,
    ],
    [pickupLat, pickupLng],
    [dropLat, dropLng],
  ];

  state.routeLine = L.polyline(routeCoords, {
    color: "#4CAF50",
    weight: 4,
    dashArray: "10, 10",
  }).addTo(state.map);

  state.map.fitBounds(L.latLngBounds(routeCoords), { padding: [50, 50] });
}

function startTripTimer() {
  state.tripStartTime = new Date();

  if (state.intervals.timer) clearInterval(state.intervals.timer);

  state.intervals.timer = setInterval(() => {
    const elapsed = Math.floor((new Date() - state.tripStartTime) / 1000);
    const minutes = Math.floor(elapsed / 60)
      .toString()
      .padStart(2, "0");
    const seconds = (elapsed % 60).toString().padStart(2, "0");
    document.getElementById("tripTimer").textContent = `${minutes}:${seconds}`;
  }, 1000);
}

/* =========================================================
   TRIP ACTIONS
========================================================= */
async function markArrived() {
  const btn = document.getElementById("arrivedBtn");
  btn.disabled = true;

  try {
    const data = await apiRequest(
      `/Requests/${state.currentTripId}/arrived`,
      "POST"
    );

    if (data.success) {
      showToast("تم تسجيل وصولك ✓", "success");
      state.currentTrip.status = "Arrived";
      updateTripButtons("Arrived");
    } else {
      showToast(data.error || "حدث خطأ", "error");
    }
  } catch (e) {
    console.error("Failed:", e);
    showToast("حدث خطأ", "error");
  } finally {
    btn.disabled = false;
  }
}

async function startTrip() {
  const btn = document.getElementById("startBtn");
  btn.disabled = true;

  try {
    const data = await apiRequest(
      `/Requests/${state.currentTripId}/start`,
      "POST"
    );

    if (data.success) {
      showToast("بدأت الرحلة! 🚗", "success");
      state.currentTrip.status = "InProgress";
      updateTripButtons("InProgress");
      state.tripStartTime = new Date();
    } else {
      showToast(data.error || "حدث خطأ", "error");
    }
  } catch (e) {
    console.error("Failed:", e);
    showToast("حدث خطأ", "error");
  } finally {
    btn.disabled = false;
  }
}

async function completeTrip() {
  const btn = document.getElementById("completeBtn");
  btn.disabled = true;

  try {
    const data = await apiRequest(
      `/Requests/${state.currentTripId}/complete`,
      "POST"
    );

    if (data.success) {
      showToast("تم إكمال الرحلة بنجاح! 🎉", "success");
      resetTrip();
      loadDriverStats();
    } else {
      showToast(data.error || "حدث خطأ", "error");
    }
  } catch (e) {
    console.error("Failed:", e);
    showToast("حدث خطأ", "error");
  } finally {
    btn.disabled = false;
  }
}

async function cancelTrip() {
  if (!confirm("هل أنت متأكد من إلغاء الرحلة؟")) return;

  try {
    const data = await apiRequest(
      `/Requests/${state.currentTripId}/cancel?reason=إلغاء من السائق`,
      "POST"
    );

    if (data.success) {
      showToast("تم إلغاء الرحلة", "warning");
      resetTrip();
    } else {
      showToast(data.error || "حدث خطأ", "error");
    }
  } catch (e) {
    console.error("Failed:", e);
    showToast("حدث خطأ", "error");
  }
}

function resetTrip() {
  state.currentTripId = null;
  state.currentTrip = null;

  if (state.intervals.timer) clearInterval(state.intervals.timer);

  if (state.markers.pickup) state.map.removeLayer(state.markers.pickup);
  if (state.markers.dropoff) state.map.removeLayer(state.markers.dropoff);
  if (state.routeLine) state.map.removeLayer(state.routeLine);

  state.markers.pickup = null;
  state.markers.dropoff = null;
  state.routeLine = null;

  document.getElementById("tripDetails").classList.remove("show");
  document.getElementById("emptyTrip").style.display = "block";
  document.getElementById("tripTimer").textContent = "00:00";

  switchTab("requests");
  if (state.isOnline) fetchRequests();
}

/* =========================================================
   CUSTOMER COMMUNICATION
========================================================= */
function callCustomer() {
  const phone = state.currentTrip?.customerPhone;
  if (phone) {
    window.location.href = `tel:${phone}`;
  } else {
    showToast("رقم الهاتف غير متوفر", "warning");
  }
}

function messageCustomer() {
  const phone = state.currentTrip?.customerPhone;
  if (phone) {
    window.location.href = `sms:${phone}`;
  } else {
    showToast("رقم الهاتف غير متوفر", "warning");
  }
}

function openNavigation() {
  if (!state.currentTrip) return;

  const isGoingToPickup = state.currentTrip.status === "Accepted";
  const lat = isGoingToPickup
    ? state.currentTrip.pickupLatitude || state.currentTrip.pickupLat
    : state.currentTrip.dropLatitude || state.currentTrip.dropLat;
  const lng = isGoingToPickup
    ? state.currentTrip.pickupLongitude || state.currentTrip.pickupLon
    : state.currentTrip.dropLongitude || state.currentTrip.dropLon;

  window.open(
    `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}`,
    "_blank"
  );
}

/* =========================================================
   DRIVER STATS
========================================================= */
async function loadDriverStats() {
  const driverId = state.user?.driverId || state.user?.userId;
  if (!driverId) return;

  try {
    const data = await apiRequest(`/Payments/driver/${driverId}/earnings`);

    if (data.success && data.data) {
      document.getElementById("todayTrips").textContent =
        data.data.totalRides || 0;
      document.getElementById("todayEarnings").textContent = `${
        data.data.totalEarnings || 0
      } ر.س`;
    }
  } catch (e) {
    console.log("Failed to load stats");
  }
}

/* =========================================================
   UI HELPERS
========================================================= */
function switchTab(tabName) {
  document.querySelectorAll(".sidebar-tab").forEach((tab, i) => {
    tab.classList.toggle("active", i === (tabName === "requests" ? 0 : 1));
  });

  document
    .getElementById("requestsTab")
    .classList.toggle("active", tabName === "requests");
  document
    .getElementById("tripTab")
    .classList.toggle("active", tabName === "trip");
}

function showToast(message, type = "success") {
  const toast = document.getElementById("toast");
  const text = document.getElementById("toastText");

  toast.className = `toast ${type}`;
  text.textContent = message;
  toast.classList.add("show");

  setTimeout(() => toast.classList.remove("show"), 3500);
}

function formatDistance(km) {
  if (!km) return "غير محدد";
  if (km < 1) return `${Math.round(km * 1000)} م`;
  return `${km.toFixed(1)} كم`;
}

function formatCoords(lat, lng) {
  if (!lat || !lng) return "غير محدد";
  return `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
}

/* =========================================================
   CONNECTION MONITORING
========================================================= */
window.addEventListener("online", () => {
  document.getElementById("connectionStatus")?.classList.remove("show");
  showToast("تم استعادة الاتصال", "success");
  if (state.isOnline) fetchRequests();
});

window.addEventListener("offline", () => {
  document.getElementById("connectionStatus")?.classList.add("show");
  showToast("انقطع الاتصال", "error");
});
