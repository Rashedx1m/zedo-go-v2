let API_BASE_URL =
  // localStorage.getItem("apiBaseUrl") ||
  "http://localhost:5000/api";
let allCustomers = [],
  allDrivers = [],
  currentPage = 1,
  currentModalType = "",
  currentEditId = null;

function getToken() {
  return localStorage.getItem("token") || sessionStorage.getItem("token");
}


function getUser() {
  return JSON.parse(
    localStorage.getItem("user") || sessionStorage.getItem("user") || "{}"
  );
}

async function apiRequest(endpoint, options = {}) {
  const url = `${API_BASE_URL}${endpoint}`;
  const token = getToken();
  const config = {
    ...options,
    headers: { "Content-Type": "application/json", ...options.headers },
  };
  if (token) config.headers["Authorization"] = `Bearer ${token}`;
  if (options.body && typeof options.body === "object")
    config.body = JSON.stringify(options.body);
  try {
    const response = await fetch(url, config);
    const text = await response.text();
    let data = {};
    if (text) {
      try {
        data = JSON.parse(text);
      } catch {
        data = { message: text };
      }
    }
    if (!response.ok)
      throw new Error(data.message || data.Message || "حدث خطأ");
    return data;
  } catch (error) {
    console.error("API Error:", error);
    throw error;
  }
}

document.addEventListener("DOMContentLoaded", () => {
  const savedUrl = localStorage.getItem("apiBaseUrl");
  if (savedUrl) {
    API_BASE_URL = savedUrl;
    document.getElementById("apiBaseUrl").value = savedUrl;
  }
  const user = getUser();
  if (user) {
    document.getElementById("adminName").textContent =
      user.fullName || "المدير";
    document.getElementById("adminAvatar").textContent = (
      user.fullName || "م"
    ).charAt(0);
  }
  loadDashboardData();
});

function showSection(section, element) {
  document
    .querySelectorAll(".nav-item")
    .forEach((i) => i.classList.remove("active"));
  if (element) element.classList.add("active");
  document
    .querySelectorAll(".page-section")
    .forEach((s) => s.classList.remove("active"));
  document.getElementById(section + "Section").classList.add("active");
  switch (section) {
    case "dashboard":
      loadDashboardData();
      break;
    case "customers":
      loadCustomers();
      break;
    case "drivers":
      loadDrivers();
      break;
    case "trips":
      loadRequests();
      break;
  }
}

async function loadDashboardData() {
 try {
  const r = await apiRequest("/Customers");
  if (r.success) {
    const count = Array.isArray(r.data) ? r.data.length : 0;

    document.getElementById("totalCustomers").textContent = count;
    document.getElementById("customersCount").textContent = count;
  }
} catch (e) {
  document.getElementById("totalCustomers").textContent = "0";
  document.getElementById("customersCount").textContent = "0";
}
try {
  const r = await apiRequest("/Drivers");
  if (r.success) {
    const count = Array.isArray(r.data) ? r.data.length : 0;

    document.getElementById("totalDrivers").textContent = count;
    document.getElementById("driversCount").textContent = count;
  }
} catch (e) {
  document.getElementById("totalDrivers").textContent = "0";
  document.getElementById("driversCount").textContent = "0";
}

try {
  const r = await apiRequest("/Requests/status/6");
  if (r.success) {
    const trips = Array.isArray(r.data) ? r.data : [];

    document.getElementById("totalTrips").textContent = trips.length;
    updateRecentTrips(trips);
  }
} catch (e) {
  document.getElementById("totalTrips").textContent = "0";
  document.getElementById("recentTripsTable").innerHTML =
    '<tr><td colspan="5" style="text-align:center;padding:30px;">لا توجد طلبات</td></tr>';
}

try {
  const r = await apiRequest("/Requests/status/1");
  if (r.success) {
    const requests = Array.isArray(r.data) ? r.data : [];
    const count = requests.length;

    document.getElementById("pendingRequests").textContent = count;
    document.getElementById("activeTripsCount").textContent = count;
  }
} catch (e) {
  document.getElementById("pendingRequests").textContent = "0";
  document.getElementById("activeTripsCount").textContent = "0";
}


  try {
    const r = await apiRequest("/Drivers/available");
    if (r.success) updateAvailableDrivers(r.data || []);
  } catch (e) {
    document.getElementById("availableDriversList").innerHTML =
      '<li class="activity-item"><div class="activity-text">لا يوجد سائقين متاحين</div></li>';
  }
}

function updateRecentTrips(requests) {
  const tbody = document.getElementById("recentTripsTable");
  if (!requests.length) {
    tbody.innerHTML =
      '<tr><td colspan="5" style="text-align:center;padding:30px;">لا توجد طلبات</td></tr>';
    return;
  }
  tbody.innerHTML = requests
    .map(
      (r) =>
        `<tr><td><strong>#${r.requestID}</strong></td><td>${
          r.customerName || "عميل #" + r.customerID
        }</td><td>${
          r.driverName|| r.driverPhone || (r.driverID ? "سائق #" + r.driverID : "-")
        }</td><td><span class="status-badge ${r.status}">${getStatusText(
          r.status
        )}</span></td><td>${
          r.minutesSinceCreated !== undefined
            ? "منذ " + r.minutesSinceCreated + " د"
            : formatDate(r.createdAt)
        }</td></tr>`
    )
    .join("");
}

function updateAvailableDrivers(drivers) {
  const list = document.getElementById("availableDriversList");
  if (!drivers.length) {
    list.innerHTML =
      '<li class="activity-item"><div class="activity-text">لا يوجد سائقين متاحين</div></li>';
    return;
  }
  list.innerHTML = drivers
    .map(
      (d) =>
        `<li class="activity-item"><div class="activity-icon success"><svg viewBox="0 0 24 24"><path d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99z"/></svg></div><div><div class="activity-text"><strong>سائق #${
          d.driverID || d.DriverID
        }</strong></div><div class="activity-time">متاح الآن</div></div></li>`
    )
    .join("");
}

async function loadCustomers() {
  const tbody = document.getElementById("customersTable");
  tbody.innerHTML =
    '<tr><td colspan="5" class="loading"><div class="spinner"></div></td></tr>';
  try {
    const r = await apiRequest("/Customers");
    if (r.success && r.data) {
      allCustomers = r.data;
      displayCustomers(allCustomers);
    } else
      tbody.innerHTML =
        '<tr><td colspan="5" style="text-align:center;padding:30px;">لا يوجد عملاء</td></tr>';
  } catch (e) {
    tbody.innerHTML =
      '<tr><td colspan="5" style="text-align:center;padding:30px;color:var(--error);">خطأ: ' +
      e.message +
      "</td></tr>";
  }
}

function displayCustomers(customers) {
  const tbody = document.getElementById("customersTable");

  if (!customers.length) {
    tbody.innerHTML = `
            <tr>
                <td colspan="5" style="text-align:center;padding:30px;">
                    لا يوجد عملاء
                </td>
            </tr>`;
    return;
  }

  tbody.innerHTML = customers
    .map((c) => {
      const id = c.userID || c.UserID;
      const name = c.fullName || c.FullName || "-";
      const email = c.email || c.Email || "-";
      const phone = c.phone || c.Phone || "-";

      // ✅ تحويل الحالة إلى Boolean حقيقي
      const rawActive = c.isActive !== undefined ? c.isActive : c.IsActive;
      const active = rawActive === true || rawActive === 1 || rawActive === "1";

      return `
        <tr>
            <!-- العميل -->
            <td>
                <div class="user-cell">
                    <div class="user-cell-avatar">${name.charAt(0)}</div>
                    <div>
                        <div class="user-cell-name">${name}</div>
                        <div class="user-cell-sub">ID: ${id}</div>
                    </div>
                </div>
            </td>

            <!-- الهاتف -->
            <td>${phone}</td>

            <!-- البريد -->
            <td>${email}</td>

            <!-- الحالة -->
            <td>
                <span class="status-badge ${active ? "active" : "inactive"}">
                    ${active ? "نشط" : "غير نشط"}
                </span>
            </td>

            <!-- الإجراءات -->
            <td>
                <div class="action-btns">
                    <button
                        class="action-btn view"
                        onclick="viewCustomer(${id})"
                        title="عرض">
                        <svg viewBox="0 0 24 24">
                            <path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5z"/>
                        </svg>
                    </button>

                    <button
                        class="action-btn ${active ? "delete" : "success"}"
                        onclick="toggleCustomerStatus(${id}, ${!active})"
                        title="${active ? "تعطيل" : "تفعيل"}">
                        <svg viewBox="0 0 24 24">
                            <path d="${
                              active
                                ? "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z"
                                : "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"
                            }"/>
                        </svg>
                    </button>
                </div>
            </td>
        </tr>`;
    })
    .join("");
}

function filterCustomers() {
  const search = document.getElementById("customerSearch").value.toLowerCase();
  const status = document.getElementById("customerStatusFilter").value;
  let filtered = allCustomers.filter((c) => {
    const name = (c.fullName || c.FullName || "").toLowerCase(),
      email = (c.email || c.Email || "").toLowerCase(),
      phone = (c.phone || c.Phone || "").toLowerCase();
    const match =
      name.includes(search) || email.includes(search) || phone.includes(search);
    if (status === "") return match;
    const active = c.isActive !== undefined ? c.isActive : c.IsActive;
    return match && String(active) === status;
  });
  displayCustomers(filtered);
}

async function toggleCustomerStatus(userId, newStatus) {
  try {
    const endpoint = newStatus
      ? `/UserController/${userId}/activate`
      : `/UserController/${userId}/deactivate`;
    const r = await apiRequest(endpoint, { method: "PUT" });
    showToast(r.Message || r.message || "تم التحديث", "success");
    loadCustomers();
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function toggleDriverStatus(driverId, newStatus) {
  try {
    if (!newStatus && !confirm("هل أنت متأكد من تعطيل السائق؟")) return;

    // ❗ استخدم UserController بدل AdminDriver
    const endpoint = newStatus
      ? `/UserController/${driverId}/activate`
      : `/UserController/${driverId}/deactivate`;

    const r = await apiRequest(endpoint, { method: "PUT" });
    showToast(r.message || r.Message || "تم التحديث", "success");
    loadDrivers();
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function viewCustomer(userId) {
  try {
    const r = await apiRequest(`/Users/${userId}`);
    if (r.success && r.data) openModal("viewCustomer", r.data);
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

let driversUsersMap = {}; // لتخزين بيانات المستخدمين للسائقين

async function loadDrivers() {
  const tbody = document.getElementById("driversTable");
  tbody.innerHTML =
    '<tr><td colspan="6" class="loading"><div class="spinner"></div></td></tr>';
  try {
    // جلب جميع السائقين
    const driversRes = await apiRequest("/Drivers");
    if (!driversRes.success || !driversRes.data) {
      tbody.innerHTML =
        '<tr><td colspan="6" style="text-align:center;padding:30px;">لا يوجد سائقين</td></tr>';
      return;
    }
    allDrivers = driversRes.data;

    // جلب بيانات المستخدمين (السائقين)
    try {
      const usersRes = await apiRequest("/Drivers");
      if (usersRes.success && usersRes.data) {
        // إنشاء خريطة للوصول السريع لبيانات المستخدم عبر UserID
        driversUsersMap = {};
        usersRes.data.forEach((u) => {
          const uid = u.userID || u.UserID;
          driversUsersMap[uid] = {
            fullName: u.fullName || u.FullName || "",
            email: u.email || u.Email || "",
            phone: u.phone || u.Phone || "",
            isActive: u.isActive !== undefined ? u.isActive : u.IsActive,
          };
        });
      }
    } catch (e) {
      console.log("Could not load driver users:", e);
    }

    displayDrivers(allDrivers);
  } catch (e) {
    tbody.innerHTML =
      '<tr><td colspan="6" style="text-align:center;padding:30px;color:var(--error);">خطأ: ' +
      e.message +
      "</td></tr>";
  }
}

function displayDrivers(drivers) {
  const tbody = document.getElementById("driversTable");

  if (!drivers.length) {
    tbody.innerHTML =
      '<tr><td colspan="7" style="text-align:center;padding:30px;">لا يوجد سائقين</td></tr>';
    return;
  }

  tbody.innerHTML = drivers
    .map((d) => {
      const did = d.fullName || d.FullName || "-";
      const uid = d.DriverID || d.userID || "-";
      const car = d.carModel || d.CarModel || "-";
      const color = d.carColor || d.CarColor || "-";
      const plate = d.plateNumber || d.PlateNumber || "-";
      const license = d.licenseNumber || d.LicenseNumber || "-";

      // بيانات المستخدم
      const userInfo = driversUsersMap[uid] || {};
      const driverName = d.fullName || userInfo.FullName || "سائق #" + did;
      const driverPhone = d.phone || "";
      const firstChar = driverName.charAt(0) || "س";

      // ✅ حالة السائق (Boolean حقيقي)
      const rawActive =
        d.isActive ??
        d.IsActive ??
        driversUsersMap[uid]?.isActive ??
        driversUsersMap[uid]?.IsActive;

      const active = String(rawActive) === "true" || String(rawActive) === "1";

      return `
        <tr>
            <td>
                <div class="user-cell">
                    <div class="user-cell-avatar driver">${firstChar}</div>
                    <div>
                        <div class="user-cell-name">${driverName}</div>
                        <div class="user-cell-sub">${
                          driverPhone || "ID: " + uid
                        }</div>
                    </div>
                </div>
            </td>

            <td>${car}</td>
            <td>${color}</td>
            <td>${plate}</td>
            <td>${license}</td>

            <!-- ✅ حالة السائق -->
            <td>
                <span class="status-badge ${active ? "active" : "inactive"}">
                    ${active ? "نشط" : "غير نشط"}
                </span>
            </td>

            <!-- ✅ الإجراءات -->
            <td>
                <div class="action-btns">
                    <button class="action-btn view" onclick="viewDriver(${did})" title="عرض">
                        <svg viewBox="0 0 24 24"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5z"/></svg>
                    </button>

                    <button class="action-btn edit" onclick="editDriver(${did})" title="تعديل">
                        <svg viewBox="0 0 24 24"><path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25z"/></svg>
                    </button>

                    <!-- ✅ زر تفعيل / تعطيل -->
                    <button
                        class="action-btn ${active ? "delete" : "success"}"
                        onclick="toggleDriverStatus(${uid}, ${!active})"
                        title="${active ? "تعطيل" : "تفعيل"}">

                        <svg viewBox="0 0 24 24">
                            <path d="${
                              active
                                ? "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z"
                                : "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"
                            }"/>
                        </svg>
                    </button>
                </div>
            </td>
        </tr>`;
    })
    .join("");
}

function filterDrivers() {
  const search = document.getElementById("driverSearch").value.toLowerCase();
  let filtered = allDrivers.filter((d) => {
    const did = d.driverID || d.DriverID;
    const uid = d.userID || d.UserID;
    const car = (d.carModel || d.CarModel || "").toLowerCase();
    const plate = (d.plateNumber || d.PlateNumber || "").toLowerCase();

    // البحث أيضاً في اسم السائق
    const userInfo = driversUsersMap[uid] || {};
    const driverName = (userInfo.fullName || "").toLowerCase();
    const driverPhone = (userInfo.phone || "").toLowerCase();

    return (
      car.includes(search) ||
      plate.includes(search) ||
      String(did).includes(search) ||
      driverName.includes(search) ||
      driverPhone.includes(search)
    );
  });
  displayDrivers(filtered);
}

async function viewDriver(driverId) {
  try {
    const r = await apiRequest(`/Drivers/${driverId}`);
    if (r.success && r.data) {
      // إضافة اسم السائق من الخريطة
      const d = r.data.driver || r.data;
      const uid = d.userID || d.UserID;
      const userInfo = driversUsersMap[uid] || {};
      r.data.driverName = userInfo.fullName || "";
      r.data.driverPhone = userInfo.phone || "";
      r.data.driverEmail = userInfo.email || "";
      openModal("viewDriver", r.data);
    }
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function editDriver(driverId) {
  try {
    const r = await apiRequest(`/AdminDriver/${driverId}`);
    if (r.success && r.data) {
      // إضافة اسم السائق من الخريطة
      const d = r.data.driver || r.data;
      const uid = d.userID || d.UserID;
      const userInfo = driversUsersMap[uid] || {};
      r.data.driverName = userInfo.fullName || "";
      openModal("editDriver", r.data);
    }
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function deleteDriver(driverId) {
  if (!confirm("هل أنت متأكد من حذف هذا السائق؟")) return;
  try {
    const r = await apiRequest(`/AdminDriver/${driverId}`, {
      method: "DELETE",
    });
    if (r.success) {
      showToast("تم الحذف", "success");
      loadDrivers();
      loadDashboardData();
    } else showToast(r.message || "فشل الحذف", "error");
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function loadRequests(page = 1) {
  const tbody = document.getElementById("tripsTable");
  tbody.innerHTML =
    '<tr><td colspan="6" class="loading"><div class="spinner"></div></td></tr>';
  const status = document.getElementById("tripStatusFilter")?.value || "";
  currentPage = page;
  try {
    let endpoint = `/requests/6`;
    if (status) endpoint += `&status=${status}`;
    const r = await apiRequest(endpoint);
    if (r.success && r.data) {
      displayRequests(r.data.data || []);
      updatePagination(r.data);
    } else
      tbody.innerHTML =
        '<tr><td colspan="6" style="text-align:center;padding:30px;">لا توجد طلبات</td></tr>';
  } catch (e) {
    tbody.innerHTML =
      '<tr><td colspan="6" style="text-align:center;padding:30px;color:var(--error);">خطأ: ' +
      e.message +
      "</td></tr>";
  }
}

function displayRequests(requests) {
  const tbody = document.getElementById("tripsTable");
  if (!requests.length) {
    tbody.innerHTML =
      '<tr><td colspan="6" style="text-align:center;padding:30px;">لا توجد طلبات</td></tr>';
    return;
  }
  tbody.innerHTML = requests
    .map(
      (r) =>
        `<tr><td><strong>#${r.requestID}</strong></td><td>${
          r.customerName || "عميل #" + r.customerID
        }</td><td>${
          r.driverName || (r.driverID ? "سائق #" + r.driverID : "-")
        }</td><td><span class="status-badge ${r.status}">${getStatusText(
          r.status
        )}</span></td><td>${
          r.minutesSinceCreated !== undefined
            ? r.minutesSinceCreated + " د"
            : formatDate(r.createdAt)
        }</td><td><div class="action-btns"><button class="action-btn view" onclick="viewRequest(${
          r.requestID
        })" title="عرض"><svg viewBox="0 0 24 24"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5z"/></svg></button>${
          r.status === "Pending"
            ? `<button class="action-btn edit" onclick="searchDriverForRequest(${r.requestID})" title="بحث عن سائق"><svg viewBox="0 0 24 24"><path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5z"/></svg></button><button class="action-btn delete" onclick="cancelRequest(${r.requestID})" title="إلغاء"><svg viewBox="0 0 24 24"><path d="M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z"/></svg></button>`
            : ""
        }</div></td></tr>`
    )
    .join("");
}

function updatePagination(data) {
  const p = document.getElementById("tripsPagination");
  if (data.totalPages <= 1) {
    p.innerHTML = "";
    return;
  }
  p.innerHTML = `<button ${
    !data.hasPreviousPage ? "disabled" : ""
  } onclick="loadRequests(${data.page - 1})">السابق</button><span>صفحة ${
    data.page
  } من ${data.totalPages}</span><button ${
    !data.hasNextPage ? "disabled" : ""
  } onclick="loadRequests(${data.page + 1})">التالي</button>`;
}

async function viewRequest(requestId) {
  try {
    const r = await apiRequest(`/requests/${requestId}`);
    if (r.success && r.data) openModal("viewRequest", r.data);
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function searchDriverForRequest(requestId) {
  try {
    showToast("جاري البحث...", "success");
    const r = await apiRequest(
      `/admin/requests/${requestId}/search-driver?maxDistance=10&timeoutMinutes=1`,
      { method: "POST" }
    );
    showToast(
      r.message || (r.success ? "تم العثور على سائق" : "لم يتم العثور"),
      r.success ? "success" : "error"
    );
    loadRequests(currentPage);
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function cancelRequest(requestId) {
  if (!confirm("هل أنت متأكد من الإلغاء؟")) return;
  try {
    const r = await apiRequest(
      `/admin/requests/${requestId}/cancel?canceledBy=Admin`,
      { method: "PUT" }
    );
    if (r.success) {
      showToast("تم الإلغاء", "success");
      loadRequests(currentPage);
    } else showToast(r.message || "فشل الإلغاء", "error");
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

function openModal(type, data = null) {
  currentModalType = type;
  const overlay = document.getElementById("modalOverlay"),
    title = document.getElementById("modalTitle"),
    body = document.getElementById("modalBody"),
    footer = document.getElementById("modalFooter");
  footer.innerHTML =
    '<button class="btn btn-secondary" onclick="closeModal()">إغلاق</button>';

  if (type === "addDriver") {
    title.textContent = "إضافة سائق جديد";
    body.innerHTML = `<div class="form-group"><label class="form-label">معرف المستخدم (UserID) *</label><input type="number" class="form-input" id="driverUserID" placeholder="أدخل معرف المستخدم"></div><div class="form-group"><label class="form-label">موديل السيارة</label><input type="text" class="form-input" id="driverCarModel" placeholder="تويوتا كامري 2022"></div><div class="form-group"><label class="form-label">لون السيارة</label><input type="text" class="form-input" id="driverCarColor" placeholder="أبيض"></div><div class="form-group"><label class="form-label">رقم اللوحة</label><input type="text" class="form-input" id="driverPlateNumber" placeholder="أ ب ج 1234"></div><div class="form-group"><label class="form-label">رقم الرخصة *</label><input type="text" class="form-input" id="driverLicenseNumber" placeholder="رقم الرخصة"></div>`;
    footer.innerHTML =
      '<button class="btn btn-secondary" onclick="closeModal()">إلغاء</button><button class="btn btn-primary" onclick="submitAddDriver()">إضافة</button>';
  } else if (type === "editDriver" && data) {
    const d = data.driver || data;
    currentEditId = d.driverID || d.DriverID;
    title.textContent = "تعديل السائق";
    body.innerHTML = `<div class="form-group"><label class="form-label">موديل السيارة</label><input type="text" class="form-input" id="editCarModel" value="${
      d.carModel || d.CarModel || ""
    }"></div><div class="form-group"><label class="form-label">لون السيارة</label><input type="text" class="form-input" id="editCarColor" value="${
      d.carColor || d.CarColor || ""
    }"></div><div class="form-group"><label class="form-label">رقم اللوحة</label><input type="text" class="form-input" id="editPlateNumber" value="${
      d.plateNumber || d.PlateNumber || ""
    }"></div><div class="form-group"><label class="form-label">رقم الرخصة</label><input type="text" class="form-input" id="editLicenseNumber" value="${
      d.licenseNumber || d.LicenseNumber || ""
    }"></div>`;
    footer.innerHTML =
      '<button class="btn btn-secondary" onclick="closeModal()">إلغاء</button><button class="btn btn-primary" onclick="submitEditDriver()">حفظ</button>';
  } else if (type === "viewDriver" && data) {
    const d = data.driver || data,
      s = data.status;
    const uid = d.userID || d.UserID;
    const driverName = data.driverName || driversUsersMap[uid]?.fullName || "-";
    const driverPhone = data.driverPhone || driversUsersMap[uid]?.phone || "-";
    const driverEmail = data.driverEmail || driversUsersMap[uid]?.email || "-";
    title.textContent = "تفاصيل السائق";
    body.innerHTML = `<div class="detail-grid">
                        <div class="detail-item" style="grid-column: span 2;">
                            <div class="detail-label">اسم السائق</div>
                            <div class="detail-value" style="font-size:1.2rem;">${driverName}</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">رقم السائق</div>
                            <div class="detail-value">#${
                              d.driverID || d.DriverID
                            }</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">رقم المستخدم</div>
                            <div class="detail-value">#${uid}</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">الهاتف</div>
                            <div class="detail-value">${driverPhone}</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">البريد</div>
                            <div class="detail-value">${driverEmail}</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">السيارة</div>
                            <div class="detail-value">${
                              d.carModel || d.CarModel || "-"
                            }</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">اللون</div>
                            <div class="detail-value">${
                              d.carColor || d.CarColor || "-"
                            }</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">رقم اللوحة</div>
                            <div class="detail-value">${
                              d.plateNumber || d.PlateNumber || "-"
                            }</div>
                        </div>
                        <div class="detail-item">
                            <div class="detail-label">رقم الرخصة</div>
                            <div class="detail-value">${
                              d.licenseNumber || d.LicenseNumber || "-"
                            }</div>
                        </div>
                        ${
                          s
                            ? `<div class="detail-item" style="grid-column: span 2;">
                            <div class="detail-label">حالة السائق</div>
                            <div class="detail-value"><span class="status-badge ${
                              s.status || s.Status
                            }">${getDriverStatusText(
                                s.status || s.Status
                              )}</span></div>
                        </div>`
                            : ""
                        }
                    </div>`;
  } else if (type === "viewCustomer" && data) {
    title.textContent = "تفاصيل العميل";
    body.innerHTML = `<div class="detail-grid"><div class="detail-item"><div class="detail-label">رقم المستخدم</div><div class="detail-value">#${
      data.userID || data.UserID
    }</div></div><div class="detail-item"><div class="detail-label">الاسم</div><div class="detail-value">${
      data.fullName || data.FullName || "-"
    }</div></div><div class="detail-item"><div class="detail-label">البريد</div><div class="detail-value">${
      data.email || data.Email || "-"
    }</div></div><div class="detail-item"><div class="detail-label">الهاتف</div><div class="detail-value">${
      data.phone || data.Phone || "-"
    }</div></div><div class="detail-item"><div class="detail-label">الحالة</div><div class="detail-value"><span class="status-badge ${
      data.isActive || data.IsActive ? "active" : "inactive"
    }">${
      data.isActive || data.IsActive ? "نشط" : "غير نشط"
    }</span></div></div></div>`;
  } else if (type === "viewRequest" && data) {
    title.textContent = "تفاصيل الطلب";
    body.innerHTML = `<div class="detail-grid"><div class="detail-item"><div class="detail-label">رقم الطلب</div><div class="detail-value">#${
      data.requestID
    }</div></div><div class="detail-item"><div class="detail-label">الحالة</div><div class="detail-value"><span class="status-badge ${
      data.status
    }">${getStatusText(
      data.status
    )}</span></div></div><div class="detail-item"><div class="detail-label">العميل</div><div class="detail-value">${
      data.customerName || "#" + data.customerID
    }</div></div><div class="detail-item"><div class="detail-label">السائق</div><div class="detail-value">${
      data.driverName ||
      (data.driverID ? "#" + data.driverID : "لم يتم التعيين")
    }</div></div><div class="detail-item"><div class="detail-label">الانطلاق</div><div class="detail-value">${
      data.pickupLat?.toFixed(4) || "-"
    }, ${
      data.pickupLon?.toFixed(4) || "-"
    }</div></div><div class="detail-item"><div class="detail-label">الوصول</div><div class="detail-value">${
      data.dropLat?.toFixed(4) || "-"
    }, ${data.dropLon?.toFixed(4) || "-"}</div></div></div>`;
  }
  overlay.classList.add("show");
}

function closeModal() {
  document.getElementById("modalOverlay").classList.remove("show");
  currentModalType = "";
  currentEditId = null;
}

async function submitAddDriver() {
  const userID = parseInt(document.getElementById("driverUserID").value),
    carModel = document.getElementById("driverCarModel").value,
    carColor = document.getElementById("driverCarColor").value,
    plateNumber = document.getElementById("driverPlateNumber").value,
    licenseNumber = document.getElementById("driverLicenseNumber").value;
  if (!userID) {
    showToast("معرف المستخدم مطلوب", "error");
    return;
  }
  if (!licenseNumber) {
    showToast("رقم الرخصة مطلوب", "error");
    return;
  }
  try {
    const r = await apiRequest("/AdminDriver", {
      method: "POST",
      body: { userID, carModel, carColor, plateNumber, licenseNumber },
    });
    if (r.success) {
      showToast(r.message || "تمت الإضافة", "success");
      closeModal();
      loadDrivers();
      loadDashboardData();
    } else showToast(r.message || "فشلت الإضافة", "error");
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

async function submitEditDriver() {
  if (!currentEditId) return;
  const carModel = document.getElementById("editCarModel").value,
    carColor = document.getElementById("editCarColor").value,
    plateNumber = document.getElementById("editPlateNumber").value,
    licenseNumber = document.getElementById("editLicenseNumber").value;
  try {
    const r = await apiRequest(`/AdminDriver/${currentEditId}`, {
      method: "PUT",
      body: { carModel, carColor, plateNumber, licenseNumber },
    });
    if (r.success) {
      showToast(r.message || "تم التحديث", "success");
      closeModal();
      loadDrivers();
    } else showToast(r.message || "فشل التحديث", "error");
  } catch (e) {
    showToast("خطأ: " + e.message, "error");
  }
}

function getStatusText(s) {
  return (
    {
      Pending: "قيد الانتظار",
      Accepted: "مقبول",
      InProgress: "جارية",
      Completed: "مكتمل",
      Cancelled: "ملغي",
    }[s] || s
  );
}
function getDriverStatusText(s) {
  return (
    {
      Online: "متصل",
      Offline: "غير متصل",
      InRide: "في رحلة",
      Available: "متاح",
    }[s] || s
  );
}
function formatDate(d) {
  if (!d) return "-";
  const date = new Date(d);
  return date.toLocaleDateString("ar-SA");
}
function refreshData() {
  loadDashboardData();
  showToast("تم التحديث", "success");
}
function showToast(msg, type = "success") {
  const t = document.getElementById("toast");
  t.className = "toast " + type;
  document.getElementById("toastText").textContent = msg;
  t.classList.add("show");
  setTimeout(() => t.classList.remove("show"), 3000);
}
function saveSettings() {
  const url = document.getElementById("apiBaseUrl").value;
  if (url) {
    localStorage.setItem("apiBaseUrl", url);
    API_BASE_URL = url;
    showToast("تم الحفظ", "success");
  }
}
async function testConnection() {
  const url = document.getElementById("apiBaseUrl").value;
  API_BASE_URL = url;
  try {
    showToast("جاري الاختبار...", "success");
    await apiRequest("/AdminDriver/all");
    showToast("تم الاتصال بنجاح!", "success");
    localStorage.setItem("apiBaseUrl", url);
  } catch (e) {
    showToast("فشل الاتصال: " + e.message, "error");
  }
}
function logout() {
  localStorage.removeItem("token");
  localStorage.removeItem("user");
  sessionStorage.removeItem("token");
  sessionStorage.removeItem("user");
  window.location.href = "login.html";
}
