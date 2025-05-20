document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('registerForm');
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirmPassword');

    form.addEventListener('submit', function (e) {
        const password = passwordInput.value;
        const confirmPassword = confirmPasswordInput.value;

        // Basic password strength validation
        const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\W).{6,}$/;

        if (!form.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
            alert("Please fill out all required fields.");
        } else if (!passwordRegex.test(password)) {
            e.preventDefault();
            alert("Password must be at least 8 characters, include uppercase, lowercase, and a special character.");
        } else if (password !== confirmPassword) {
            e.preventDefault();
            alert("Passwords do not match.");
        }
    });
});