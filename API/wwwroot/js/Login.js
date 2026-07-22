// API Configuration
const API_BASE_URL = "http://localhost:5000/api";

// Toggle Password Visibility
function togglePassword() {
  const passwordInput = document.getElementById("password");
  const eyeIcon = document.getElementById("eyeIcon");

  if (passwordInput.type === "password") {
    passwordInput.type = "text";
    eyeIcon.innerHTML =
      '<path d="M12 7c2.76 0 5 2.24 5 5 0 .65-.13 1.26-.36 1.83l2.92 2.92c1.51-1.26 2.7-2.89 3.43-4.75-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7zM2 4.27l2.28 2.28.46.46C3.08 8.3 1.78 10.02 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84l.42.42L19.73 22 21 20.73 3.27 3 2 4.27zM7.53 9.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm4.31-.78l3.15 3.15.02-.16c0-1.66-1.34-3-3-3l-.17.01z"/>';
  } else {
    passwordInput.type = "password";
    eyeIcon.innerHTML =
      '<path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/>';
  }
}

// Show Error
function showError(message) {
  const errorDiv = document.getElementById("errorMessage");
  const errorText = document.getElementById("errorText");
  errorText.textContent = message;
  errorDiv.classList.add("show");

  setTimeout(() => {
    errorDiv.classList.remove("show");
  }, 5000);
}

// Show Success
function showSuccess(message) {
  const successDiv = document.getElementById("successMessage");
  const successText = document.getElementById("successText");
  successText.textContent = message;
  successDiv.classList.add("show");
}

// Login Form Submit
document
  .getElementById("loginForm")
  .addEventListener("submit", async function (e) {
    e.preventDefault();

    const submitBtn = document.getElementById("submitBtn");
    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;
    const rememberMe = document.getElementById("rememberMe").checked;

    // Validation
    if (!email || !password) {
      showError("يرجى ملء جميع الحقول");
      return;
    }

    // Start loading
    submitBtn.classList.add("loading");
    submitBtn.disabled = true;

    try {
      const response = await fetch(`${API_BASE_URL}/Auth/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: email,
          password: password,
        }),
      });

      const result = await response.json(); // ✅ غيّرنا الاسم لـ result

      if (response.ok && result.success) {
        // ✅ تحقق من success
        const userData = result.data; // ✅ البيانات داخل data

        showSuccess("تم تسجيل الدخول بنجاح!");

        // Store token
        const storage = rememberMe ? localStorage : sessionStorage;
        storage.setItem("token", userData.token); // ✅ userData.token
        storage.setItem(
          "user",
          JSON.stringify({
            userId: userData.userId,
            email: userData.email,
            fullName: userData.fullName,
            role: userData.role,
            customerId: userData.customerId, // ✅ جديد
            driverId: userData.driverId, // ✅ جديد
          })
        );

        // Redirect based on role
        setTimeout(() => {
          switch (
            userData.role // ✅ userData.role
          ) {
            case "Driver":
              window.location.href = "driver-dashboard.html";
              break;
            case "Admin":
              window.location.href = "admin-dashboard.html";
              break;
            default:
              window.location.href = "customer-dashboard.html";
          }
        }, 1000);
      } else {
        // ✅ عرض رسالة الخطأ من الـ API
        showError(result.error || "فشل تسجيل الدخول");
      }
    } catch (error) {
      console.error("Login error:", error);
      showError("حدث خطأ في الاتصال بالسيرفر");
    } finally {
      submitBtn.classList.remove("loading");
      submitBtn.disabled = false;
    }
  });

// Social Login Functions
function loginWithGoogle() {
  alert("سيتم إضافة تسجيل الدخول بـ Google قريباً");
}

function loginWithApple() {
  alert("سيتم إضافة تسجيل الدخول بـ Apple قريباً");
}

// Check if already logged in
document.addEventListener("DOMContentLoaded", function () {
  const token =
    localStorage.getItem("token") || sessionStorage.getItem("token");
  if (token) {
    const user = JSON.parse(
      localStorage.getItem("user") || sessionStorage.getItem("user") || "{}"
    );
    if (user.role) {
      switch (user.role) {
        case "Driver":
          window.location.href = "driver-dashboard.html";
          break;
        case "Admin":
          window.location.href = "admin-dashboard.html";
          break;
        default:
          window.location.href = "customer-dashboard.html";
      }
    }
  }
});
