/* =========================================================
           CONFIG
========================================================= */
const API_BASE_URL = "http://localhost:5000/api";

/* =========================================================
           GLOBAL STATE
========================================================= */
let map, pickupMarker, dropoffMarker, driverMarker, routeLine;
let pickupLocation = null;
let dropoffLocation = null;
let currentRequestId = null;
let currentDriverPhone = null;
let statusCheckInterval = null;
let selectingLocation = "pickup";

/* =========================================================
           AUTH HELPERS
========================================================= */
function getToken() {
  return localStorage.getItem("token") || sessionStorage.getItem("token");
}

function getUser() {
  return JSON.parse(
    localStorage.getItem("user") || sessionStorage.getItem("user") || "{}"
  );
}

function logout() {
  localStorage.removeItem("token");
  localStorage.removeItem("user");
  sessionStorage.removeItem("token");
  sessionStorage.removeItem("user");
  window.location.href = "login.html";
}

/* =========================================================
           SAFE FETCH
========================================================= */
async function safeFetch(url, options = {}) {
  const response = await fetch(url, options);

  if (response.status === 401) {
    logout();
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
           INIT
========================================================= */
document.addEventListener("DOMContentLoaded", function () {
  checkAuth();
  initMap();
  checkActiveRequest();

  // ✅ تأكد من أن الزر معطل في البداية
  const requestBtn = document.getElementById("requestBtn");
  if (requestBtn) {
    requestBtn.disabled = true;
  }
});

/* =========================================================
           AUTH CHECK
========================================================= */
function checkAuth() {
  const token = getToken();
  const user = getUser();

  if (!token || token === "undefined") {
    logout();
    return;
  }

  document.getElementById("userName").textContent = user.fullName || "مستخدم";
  document.getElementById("userAvatar").textContent = (
    user.fullName || "م"
  ).charAt(0);
}

/* =========================================================
           MAP
========================================================= */
function initMap() {
  map = L.map("map", { zoomControl: false }).setView([24.7136, 46.6753], 13);

  L.tileLayer("https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png", {
    maxZoom: 19,
  }).addTo(map);

  map.on("click", onMapClick);
  getCurrentLocation();
}

function getCurrentLocation() {
  if (!navigator.geolocation) return;

  navigator.geolocation.getCurrentPosition(
    (pos) => {
      const { latitude, longitude } = pos.coords;
      map.setView([latitude, longitude], 15);
      if (!pickupLocation) setPickupLocation(latitude, longitude);
    },
    () => showToast("لم نتمكن من تحديد موقعك", "error")
  );
}

function onMapClick(e) {
  // ✅ لا تفعل شيء إذا كان هناك طلب نشط
  if (currentRequestId) return;

  const { lat, lng } = e.latlng;

  if (selectingLocation === "pickup") {
    setPickupLocation(lat, lng);
    selectingLocation = "dropoff";
    showToast("الآن حدد نقطة الوصول", "info");
  } else {
    setDropoffLocation(lat, lng);
    selectingLocation = "pickup";
  }
}

function setPickupFromMap() {
  selectingLocation = "pickup";
  showToast("انقر على الخريطة لتحديد موقع الانطلاق");
}

function setDropoffFromMap() {
  selectingLocation = "dropoff";
  showToast("انقر على الخريطة لتحديد موقع الوصول");
}

/* =========================================================
           LOCATION HANDLERS
========================================================= */
function setPickupLocation(lat, lng) {
  pickupLocation = { lat, lng };

  if (pickupMarker) map.removeLayer(pickupMarker);

  pickupMarker = L.marker([lat, lng], {
    icon: L.divIcon({
      className: "custom-marker pickup",
      html: '<div style="background:#4CAF50;width:20px;height:20px;border-radius:50%;border:3px solid white;"></div>',
      iconSize: [26, 26],
      iconAnchor: [13, 13],
    }),
  }).addTo(map);

  document.getElementById("pickupInput").value = `${lat.toFixed(
    5
  )}, ${lng.toFixed(5)}`;

  // ✅ تحديث المعلومات فقط - بدون إنشاء طلب
  updateTripInfo();
}

function setDropoffLocation(lat, lng) {
  dropoffLocation = { lat, lng };

  if (dropoffMarker) map.removeLayer(dropoffMarker);

  dropoffMarker = L.marker([lat, lng], {
    icon: L.divIcon({
      className: "custom-marker dropoff",
      html: '<div style="background:#f44336;width:20px;height:20px;border-radius:50%;border:3px solid white;"></div>',
      iconSize: [26, 26],
      iconAnchor: [13, 13],
    }),
  }).addTo(map);

  document.getElementById("dropoffInput").value = `${lat.toFixed(
    5
  )}, ${lng.toFixed(5)}`;

  // ✅ رسم الخط بين النقطتين
  drawRoute();

  // ✅ تحديث المعلومات فقط - بدون إنشاء طلب
  updateTripInfo();
}

/* =========================================================
           DRAW ROUTE
========================================================= */
function drawRoute() {
  if (!pickupLocation || !dropoffLocation) return;

  if (routeLine) map.removeLayer(routeLine);

  routeLine = L.polyline(
    [
      [pickupLocation.lat, pickupLocation.lng],
      [dropoffLocation.lat, dropoffLocation.lng],
    ],
    {
      color: "#2196F3",
      weight: 4,
      opacity: 0.8,
      dashArray: "10, 10",
    }
  ).addTo(map);

  // تكبير الخريطة لإظهار المسار
  map.fitBounds(routeLine.getBounds(), { padding: [50, 50] });
}

/* =========================================================
           TRIP INFO - ✅ بدون إنشاء تلقائي
========================================================= */
function updateTripInfo() {
  const tripInfo = document.getElementById("tripInfo");
  const requestBtn = document.getElementById("requestBtn");

  // ✅ إذا لم يتم تحديد النقطتين
  if (!pickupLocation || !dropoffLocation) {
    tripInfo.classList.remove("show");
    requestBtn.disabled = true;
    return;
  }

  const distance = calculateDistance(
    pickupLocation.lat,
    pickupLocation.lng,
    dropoffLocation.lat,
    dropoffLocation.lng
  );

  const duration = Math.round(distance * 3);
  const price = Math.round(distance * 2.5 + 5);

  document.getElementById("tripDistance").textContent = `${distance.toFixed(
    1
  )} كم`;
  document.getElementById("tripDuration").textContent = `${duration} د`;
  document.getElementById("tripPrice").textContent = `${price} ر.س`;

  // ✅ إظهار معلومات الرحلة
  tripInfo.classList.add("show");

  // ✅ تفعيل الزر - المستخدم يجب أن يضغط عليه يدوياً
  requestBtn.disabled = false;
  requestBtn.style.display = "flex";

  // ❌ لا نستدعي createRequest() هنا!
}

function calculateDistance(lat1, lon1, lat2, lon2) {
  const R = 6371;
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLon = ((lon2 - lon1) * Math.PI) / 180;
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLon / 2) ** 2;

  return R * (2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a)));
}

/* =========================================================
           REQUESTS
========================================================= */
async function createRequest() {
  if (!pickupLocation || !dropoffLocation) {
    showToast("يرجى تحديد موقع الانطلاق والوصول", "error");
    return;
  }

  // ✅ تعطيل الزر وإظهار حالة التحميل
  const btn = document.getElementById("requestBtn");
  btn.disabled = true;
  btn.innerHTML = "<span>⏳</span><span>جاري الإرسال...</span>";

  try {
    const result = await safeFetch(`${API_BASE_URL}/Requests`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${getToken()}`,
      },
      body: JSON.stringify({
        pickupLatitude: pickupLocation.lat,
        pickupLongitude: pickupLocation.lng,
        dropLatitude: dropoffLocation.lat,
        dropLongitude: dropoffLocation.lng,
      }),
    });

    if (result.success) {
      currentRequestId = result.data;
      showActiveRequest("Pending");
      startStatusCheck();
      showToast("تم إنشاء الطلب بنجاح ✅");
    } else {
      showToast(result.error || "فشل إنشاء الطلب", "error");
      resetRequestButton();
    }
  } catch (e) {
    console.error(e);
    showToast(e.message || "فشل إنشاء الطلب", "error");
    resetRequestButton();
  }
}

async function getRequestDetails() {
  if (!currentRequestId) return;

  try {
    const result = await safeFetch(
      `${API_BASE_URL}/Requests/${currentRequestId}/full`,
      { headers: { Authorization: `Bearer ${getToken()}` } }
    );

    if (result.success) {
      const req = result.data;
      showActiveRequest(req.status, req);

      // ✅ تحديث موقع السائق على الخريطة
      if (req.driverLatitude && req.driverLongitude) {
        updateDriverMarker(req.driverLatitude, req.driverLongitude);
      }

      // ✅ إيقاف التحديث عند انتهاء الرحلة
      if (req.status === "Completed" || req.status === "Canceled") {
        stopStatusCheck();
        setTimeout(resetUI, 3000);
      }
    }
  } catch (e) {
    console.error("خطأ في جلب تفاصيل الطلب:", e);
  }
}

async function checkActiveRequest() {
  const user = getUser();
  const customerId = user.customerId || user.userId;

  if (!customerId) {
    console.log("لا يوجد معرف للعميل");
    return;
  }

  try {
    const result = await safeFetch(
      `${API_BASE_URL}/Requests/customer/${customerId}/active`,
      { headers: { Authorization: `Bearer ${getToken()}` } }
    );

    if (result.success && result.data) {
      const req = result.data;
      currentRequestId = req.id;

      setPickupLocation(req.pickupLatitude, req.pickupLongitude);
      setDropoffLocation(req.dropLatitude, req.dropLongitude);
      showActiveRequest(req.status, req);
      startStatusCheck();
    }
  } catch (e) {
    console.log("لا يوجد طلب نشط");
  }
}

async function cancelRequest() {
  if (!currentRequestId) {
    showToast("لا يوجد طلب لإلغائه", "error");
    return;
  }

  if (!confirm("هل أنت متأكد من إلغاء الطلب؟")) return;

  try {
    const result = await safeFetch(
      `${API_BASE_URL}/Requests/${currentRequestId}/cancel?reason=إلغاء من العميل`,
      {
        method: "POST",
        headers: { Authorization: `Bearer ${getToken()}` },
      }
    );

    if (result.success) {
      showToast("تم إلغاء الطلب بنجاح");
      stopStatusCheck();
      resetUI();
    } else {
      showToast(result.error || "فشل إلغاء الطلب", "error");
    }
  } catch (e) {
    console.error(e);
    showToast("حدث خطأ أثناء الإلغاء", "error");
  }
}

/* =========================================================
           STATUS POLLING
========================================================= */
function startStatusCheck() {
  stopStatusCheck();
  statusCheckInterval = setInterval(getRequestDetails, 3000);
}

function stopStatusCheck() {
  if (statusCheckInterval) {
    clearInterval(statusCheckInterval);
    statusCheckInterval = null;
  }
}

/* =========================================================
           DRIVER MARKER
========================================================= */
function updateDriverMarker(lat, lng) {
  if (driverMarker) map.removeLayer(driverMarker);

  driverMarker = L.marker([lat, lng], {
    icon: L.divIcon({
      className: "driver-marker",
      html: '<div style="font-size:24px;">🚗</div>',
      iconSize: [30, 30],
      iconAnchor: [15, 15],
    }),
  }).addTo(map);
}

/* =========================================================
           UI HELPERS
========================================================= */
function showToast(message, type = "success") {
  const toast = document.getElementById("toast");
  const text = document.getElementById("toastText");
  toast.className = `toast ${type}`;
  text.textContent = message;
  toast.classList.add("show");
  setTimeout(() => toast.classList.remove("show"), 3000);
}

function resetUI() {
  currentRequestId = null;
  currentDriverPhone = null;
  pickupLocation = null;
  dropoffLocation = null;

  if (pickupMarker) map.removeLayer(pickupMarker);
  if (dropoffMarker) map.removeLayer(dropoffMarker);
  if (driverMarker) map.removeLayer(driverMarker);
  if (routeLine) map.removeLayer(routeLine);

  pickupMarker = null;
  dropoffMarker = null;
  driverMarker = null;
  routeLine = null;

  document.getElementById("pickupInput").value = "";
  document.getElementById("dropoffInput").value = "";
  document.getElementById("activeRequest").classList.remove("show");
  document.getElementById("tripInfo").classList.remove("show");

  resetRequestButton();
  hideDriverCard();

  selectingLocation = "pickup";
}

function resetRequestButton() {
  const btn = document.getElementById("requestBtn");
  btn.disabled = true;
  btn.innerHTML = "<span>🚗</span><span>طلب رحلة</span>";
  btn.style.display = "flex";
}

function showActiveRequest(status, requestData = null) {
  const activeBox = document.getElementById("activeRequest");
  const cancelBtn = activeBox.querySelector(".cancel-btn");

  activeBox.classList.add("show");
  document.getElementById("requestBtn").style.display = "none";
  document.getElementById("tripInfo").classList.remove("show");

  const statusBar = document.getElementById("statusBar");
  const indicator = document.getElementById("statusIndicator");
  const text = document.getElementById("statusText");

  statusBar.className = "request-status-bar " + status.toLowerCase();
  indicator.className = "status-indicator " + status.toLowerCase();

  const statusTexts = {
    Pending: "قيد الانتظار...",
    Searching: "جاري البحث عن سائق...",
    Accepted: "تم قبول طلبك! السائق في الطريق 🚗",
    Arrived: "السائق وصل! 📍",
    InProgress: "الرحلة جارية... 🚗",
    Completed: "تم إكمال الرحلة ✅",
    Canceled: "تم إلغاء الطلب ❌",
  };

  text.textContent = statusTexts[status] || status;

  // عرض/إخفاء بطاقة السائق
  if (
    ["Accepted", "Arrived", "InProgress"].includes(status) &&
    requestData?.driverId
  ) {
    showDriverCard(requestData);
  } else {
    hideDriverCard();
  }

  // عرض ملخص الرحلة عند الاكتمال
  if (status === "Completed" && requestData) {
    showTripSummary(requestData);
  }

  // التحكم بزر الإلغاء
  if (["Pending", "Searching", "Accepted"].includes(status)) {
    cancelBtn.style.display = "block";
  } else {
    cancelBtn.style.display = "none";
  }
}

function showDriverCard(requestData) {
  const driverCard = document.getElementById("driverCard");
  if (!driverCard) return;

  const driverName = requestData.driverName || "السائق";
  const driverPhone = requestData.driverPhone || "";
  const carModel = requestData.carModel || "";
  const carColor = requestData.carColor || "";
  const plateNumber = requestData.plateNumber || "";

  currentDriverPhone = driverPhone;

  document.getElementById("driverCardAvatar").textContent =
    driverName.charAt(0);
  document.getElementById("driverCardName").textContent = driverName;

  const vehicleInfo = document.getElementById("vehicleInfo");
  if (carModel || plateNumber) {
    document.getElementById("driverCardVehicle").textContent = carModel
      ? `🚗 ${carModel} ${carColor}`
      : "";
    document.getElementById("driverCardPlate").textContent = plateNumber || "";
    vehicleInfo.style.display = "flex";
  } else {
    vehicleInfo.style.display = "none";
  }

  driverCard.classList.add("show");
}

function hideDriverCard() {
  const driverCard = document.getElementById("driverCard");
  if (driverCard) {
    driverCard.classList.remove("show");
  }
  currentDriverPhone = null;
}

function showTripSummary(requestData) {
  const totalCost = requestData.actualCost || requestData.estimatedCost || 0;
  if (totalCost > 0) {
    showToast(
      `تم إكمال الرحلة! المبلغ: ${totalCost.toFixed(2)} ر.س`,
      "success"
    );
  }
}

function callDriver() {
  if (currentDriverPhone) {
    window.location.href = `tel:${currentDriverPhone}`;
  } else {
    showToast("رقم الهاتف غير متوفر", "error");
  }
}

function messageDriver() {
  if (currentDriverPhone) {
    window.location.href = `sms:${currentDriverPhone}`;
  } else {
    showToast("رقم الهاتف غير متوفر", "error");
  }
}
