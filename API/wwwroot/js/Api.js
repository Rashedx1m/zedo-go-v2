// API Configuration
const API_CONFIG = {
  // ⚠️ قم بتغيير هذا إلى رابط API الخاص بك
  BASE_URL: "http://localhost:5000/api",

  // Endpoints
  ENDPOINTS: {
    // Auth
    LOGIN: "/Auth/login",
    REGISTER: "/Auth/register",
    REGISTER_DRIVER: "/Auth/register-driver",
    CHANGE_PASSWORD: "/Auth/change-password",

    // Customer Requests
    CUSTOMER_REQUESTS: "/customer/requests",
    CUSTOMER_ACTIVE_REQUEST: "/customer/requests/active",

    // Driver Requests
    DRIVER_AVAILABLE_REQUESTS: "/driver/requests/available",
    DRIVER_REQUESTS: "/driver/requests",
  },
};

// API Helper Functions
class ZedoGoAPI {
  constructor() {
    this.baseUrl = API_CONFIG.BASE_URL;
  }

  // Get auth token
  getToken() {
    return localStorage.getItem("token") || sessionStorage.getItem("token");
  }

  // Get current user
  getUser() {
    const user = localStorage.getItem("user") || sessionStorage.getItem("user");
    return user ? JSON.parse(user) : null;
  }

  // Save auth data
  saveAuth(data, remember = false) {
    const storage = remember ? localStorage : sessionStorage;
    storage.setItem("token", data.token);
    storage.setItem(
      "user",
      JSON.stringify({
        userId: data.userId,
        email: data.email,
        fullName: data.fullName,
        role: data.role,
      })
    );
  }

  // Clear auth data
  clearAuth() {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    sessionStorage.removeItem("token");
    sessionStorage.removeItem("user");
  }

  // Check if authenticated
  isAuthenticated() {
    return !!this.getToken();
  }

  // Make API request
  async request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const token = this.getToken();

    const config = {
      ...options,
      headers: {
        "Content-Type": "application/json",
        ...options.headers,
      },
    };

    if (token) {
      config.headers["Authorization"] = `Bearer ${token}`;
    }

    if (options.body && typeof options.body === "object") {
      config.body = JSON.stringify(options.body);
    }

    try {
      const response = await fetch(url, config);
      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || "حدث خطأ في الطلب");
      }

      return data;
    } catch (error) {
      console.error("API Error:", error);
      throw error;
    }
  }

  // Auth endpoints
  async login(email, password) {
    return this.request(API_CONFIG.ENDPOINTS.LOGIN, {
      method: "POST",
      body: { email, password },
    });
  }

  async register(userData) {
    return this.request(API_CONFIG.ENDPOINTS.REGISTER, {
      method: "POST",
      body: userData,
    });
  }

  async registerDriver(driverData) {
    return this.request(API_CONFIG.ENDPOINTS.REGISTER_DRIVER, {
      method: "POST",
      body: driverData,
    });
  }

  async changePassword(oldPassword, newPassword) {
    return this.request(API_CONFIG.ENDPOINTS.CHANGE_PASSWORD, {
      method: "POST",
      body: { oldPassword, newPassword },
    });
  }

  // Customer endpoints
  async createRequest(pickupLat, pickupLon, dropoffLat, dropoffLon) {
    return this.request(API_CONFIG.ENDPOINTS.CUSTOMER_REQUESTS, {
      method: "POST",
      body: {
        pickupLatitude: pickupLat,
        pickupLongitude: pickupLon,
        dropoffLatitude: dropoffLat,
        dropoffLongitude: dropoffLon,
      },
    });
  }

  async getActiveRequest() {
    return this.request(API_CONFIG.ENDPOINTS.CUSTOMER_ACTIVE_REQUEST);
  }

  async getCustomerRequest(requestId) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.CUSTOMER_REQUESTS}/${requestId}`
    );
  }

  async searchDriver(requestId, maxDistance = 10, timeout = 2) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.CUSTOMER_REQUESTS}/${requestId}/search-driver?maxDistance=${maxDistance}&timeoutMinutes=${timeout}`,
      { method: "POST" }
    );
  }

  async cancelCustomerRequest(requestId) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.CUSTOMER_REQUESTS}/${requestId}/cancel`,
      { method: "PUT" }
    );
  }

  // Driver endpoints
  async getAvailableRequests(radiusKm = 15) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.DRIVER_AVAILABLE_REQUESTS}?radiusKm=${radiusKm}`
    );
  }

  async getDriverRequest(requestId) {
    return this.request(`${API_CONFIG.ENDPOINTS.DRIVER_REQUESTS}/${requestId}`);
  }

  async acceptRequest(requestId) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.DRIVER_REQUESTS}/${requestId}/accept`,
      { method: "PUT" }
    );
  }

  async markArrived(requestId) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.DRIVER_REQUESTS}/${requestId}/arrived`,
      { method: "PUT" }
    );
  }

  async updateRequestStatus(requestId, status) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.DRIVER_REQUESTS}/${requestId}/status`,
      { method: "PUT", body: { status } }
    );
  }

  async completeTrip(requestId) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.DRIVER_REQUESTS}/${requestId}/complete`,
      { method: "PUT" }
    );
  }

  async cancelDriverRequest(requestId) {
    return this.request(
      `${API_CONFIG.ENDPOINTS.DRIVER_REQUESTS}/${requestId}/cancel`,
      { method: "PUT" }
    );
  }

  async updateDriverLocation(latitude, longitude) {
    return this.request("/driver/location", {
      method: "PUT",
      body: { latitude, longitude },
    });
  }
}

// Create global instance
const api = new ZedoGoAPI();

// Export for module usage
if (typeof module !== "undefined" && module.exports) {
  module.exports = { API_CONFIG, ZedoGoAPI, api };
}
