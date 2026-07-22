const API_BASE_URL = "http://localhost:5220/api";

// Switch Registration Type
function switchRegType(type) {
  const tabs = document.querySelectorAll(".reg-type-tab");
  const driverFields = document.getElementById("driverFields");
  const regTypeInput = document.getElementById("regType");

  tabs.forEach((tab) => {
    tab.classList.toggle("active", tab.dataset.type === type);
  });

  regTypeInput.value = type;

  if (type === "driver") {
    driverFields.classList.add("show");
    // Make driver fields required
    document.getElementById("carModel").required = true;
    document.getElementById("carColor").required = true;
    document.getElementById("plateNumber").required = true;
    document.getElementById("licenseNumber").required = true;
  } else {
    driverFields.classList.remove("show");
    // Remove required from driver fields
    document.getElementById("carModel").required = false;
    document.getElementById("carColor").required = false;
    document.getElementById("plateNumber").required = false;
    document.getElementById("licenseNumber").required = false;
  }
}

// Password Strength Check
function checkPasswordStrength() {
  const password = document.getElementById("password").value;
  const strengthFill = document.getElementById("strengthFill");
  const strengthText = document.getElementById("strengthText");

  let strength = 0;
  let text = "";

  if (password.length >= 8) strength++;
  if (password.match(/[a-z]/)) strength++;
  if (password.match(/[A-Z]/)) strength++;
  if (password.match(/[0-9]/)) strength++;
  if (password.match(/[^a-zA-Z0-9]/)) strength++;

  strengthFill.className = "strength-fill";
  strengthText.className = "strength-text";

  if (password.length === 0) {
    text = "";
  } else if (strength <= 2) {
    strengthFill.classList.add("weak");
    strengthText.classList.add("weak");
    text = "ضعيفة جداً";
  } else if (strength === 3) {
    strengthFill.classList.add("fair");
    strengthText.classList.add("fair");
    text = "متوسطة";
  } else if (strength === 4) {
    strengthFill.classList.add("good");
    strengthText.classList.add("good");
    text = "جيدة";
  } else {
    strengthFill.classList.add("strong");
    strengthText.classList.add("strong");
    text = "قوية جداً";
  }

  strengthText.textContent = text;
}

// Show Messages
function showError(message) {
  const el = document.getElementById("errorMessage");
  document.getElementById("errorText").textContent = message;
  el.classList.add("show");
  setTimeout(() => el.classList.remove("show"), 5000);
}

function showSuccess(message) {
  const el = document.getElementById("successMessage");
  document.getElementById("successText").textContent = message;
  el.classList.add("show");
}

// Form Submit
document
  .getElementById("registerForm")
  .addEventListener("submit", async function (e) {
    e.preventDefault();

    const submitBtn = document.getElementById("submitBtn");
    const regType = document.getElementById("regType").value;

    // Get form values
    const fullName = document.getElementById("fullName").value.trim();
    const email = document.getElementById("email").value.trim();
    const phone = document.getElementById("phone").value.trim();
    const password = document.getElementById("password").value;
    const confirmPassword = document.getElementById("confirmPassword").value;
    const terms = document.getElementById("terms").checked;

    // Validation
    if (!fullName || !email || !password) {
      showError("يرجى ملء جميع الحقول المطلوبة");
      return;
    }

    if (password !== confirmPassword) {
      showError("كلمتا المرور غير متطابقتين");
      return;
    }

    if (password.length < 8) {
      showError("كلمة المرور يجب أن تكون 8 أحرف على الأقل");
      return;
    }

    if (!terms) {
      showError("يجب الموافقة على الشروط والأحكام");
      return;
    }

    // Driver validation
    if (regType === "driver") {
      const carModel = document.getElementById("carModel").value.trim();
      const carColor = document.getElementById("carColor").value.trim();
      const plateNumber = document.getElementById("plateNumber").value.trim();
      const licenseNumber = document
        .getElementById("licenseNumber")
        .value.trim();

      if (!carModel || !carColor || !plateNumber || !licenseNumber) {
        showError("يرجى ملء جميع معلومات السيارة");
        return;
      }
    }

    // Start loading
    submitBtn.classList.add("loading");
    submitBtn.disabled = true;

    try {
      let endpoint, body;

      if (regType === "driver") {
        endpoint = `${API_BASE_URL}/AuthController/register-driver`;
        body = {
          fullName,
          email,
          phone,
          password,
          carModel: document.getElementById("carModel").value.trim(),
          carColor: document.getElementById("carColor").value.trim(),
          plateNumber: document.getElementById("plateNumber").value.trim(),
          licenseNumber: document.getElementById("licenseNumber").value.trim(),
        };
      } else {
        endpoint = `${API_BASE_URL}/AuthController/register`;
        body = {
          fullName,
          email,
          phone,
          password,
          role: "Customer",
        };
      }

      const response = await fetch(endpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });

      const data = await response.json();

      if (response.ok) {
        showSuccess("تم إنشاء الحساب بنجاح! جاري تحويلك...");

        // Store token
        localStorage.setItem("token", data.token);
        localStorage.setItem(
          "user",
          JSON.stringify({
            email: data.email,
            fullName: data.fullName,
            role: data.role,
          })
        );

        // Redirect
        setTimeout(() => {
          if (data.role === "Driver") {
            window.location.href = "driver-dashboard.html";
          } else {
            window.location.href = "customer-dashboard.html";
          }
        }, 1500);
      } else {
        showError(data.message || "فشل إنشاء الحساب");
      }
    } catch (error) {
      console.error("Registration error:", error);
      showError("حدث خطأ في الاتصال بالسيرفر");
    } finally {
      submitBtn.classList.remove("loading");
      submitBtn.disabled = false;
    }
  });
