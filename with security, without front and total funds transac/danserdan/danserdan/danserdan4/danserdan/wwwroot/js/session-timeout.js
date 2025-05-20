// Session Timeout Handler
(function () {
    // Configuration
    const sessionTimeoutMinutes = 20; // Session timeout in minutes
    const warningMinutes = 1; // Warning before timeout in minutes
    const checkInterval = 60000; // Check interval in milliseconds (1 minute)
    const logoutUrl = '/Account/Logout'; // Logout URL
    
    // Initialize with current time to ensure we don't show warning immediately
    let lastActivity = new Date();
    let warningShown = false;
    let warningModal = null;
    let countdownInterval = null;
    let sessionInitialized = false; // Flag to track if session is initialized
    
    // Initialize the session timeout handler
    function initSessionTimeout() {
        // Create the warning modal but keep it hidden
        createWarningModal();
        
        // Reset timer on user activity
        document.addEventListener('mousemove', resetTimer);
        document.addEventListener('mousedown', resetTimer);
        document.addEventListener('keypress', resetTimer);
        document.addEventListener('touchmove', resetTimer);
        document.addEventListener('scroll', resetTimer);
        
        // Start the timer after a delay to prevent immediate popup
        setTimeout(() => {
            sessionInitialized = true;
            console.log('Session timeout monitoring initialized');
            // Initial timer reset
            resetTimer();
        }, 5000); // 5 second delay before starting session monitoring
        
        // Start the interval check
        setInterval(checkSession, checkInterval);
    }
    
    // Reset the timer on user activity
    function resetTimer() {
        lastActivity = new Date();
        
        // If warning was shown but user is active again, hide it
        if (warningShown) {
            hideWarningModal();
        }
    }
    
    // Check if the session has timed out
    function checkSession() {
        // Only check session timeout if initialization period has passed
        if (!sessionInitialized) {
            return; // Skip checking if session monitoring isn't initialized yet
        }
        
        const currentTime = new Date();
        const elapsedMinutes = (currentTime - lastActivity) / 60000;
        
        // For debugging
        if (elapsedMinutes > 1) {
            console.log(`Session inactive for ${elapsedMinutes.toFixed(2)} minutes`);
        }
        
        // If session is about to expire and warning not shown yet
        if (elapsedMinutes >= (sessionTimeoutMinutes - warningMinutes) && !warningShown) {
            console.log('Showing session timeout warning');
            showWarningModal();
        }
        
        // If session has expired
        if (elapsedMinutes >= sessionTimeoutMinutes) {
            console.log('Session expired, logging out');
            logout();
        }
    }
    
    // Create the warning modal
    function createWarningModal() {
        // Create modal container
        warningModal = document.createElement('div');
        warningModal.className = 'session-timeout-modal';
        warningModal.style.position = 'fixed';
        warningModal.style.zIndex = '9999';
        warningModal.style.left = '0';
        warningModal.style.top = '0';
        warningModal.style.width = '100%';
        warningModal.style.height = '100%';
        warningModal.style.backgroundColor = 'rgba(0,0,0,0.5)';
        warningModal.style.alignItems = 'center';
        warningModal.style.justifyContent = 'center';
        // Ensure it's hidden initially
        warningModal.style.display = 'none';
        
        // Create modal content
        const modalContent = document.createElement('div');
        modalContent.className = 'session-timeout-modal-content';
        modalContent.style.backgroundColor = '#fff';
        modalContent.style.padding = '20px';
        modalContent.style.borderRadius = '5px';
        modalContent.style.width = '400px';
        modalContent.style.maxWidth = '90%';
        modalContent.style.boxShadow = '0 4px 8px rgba(0,0,0,0.2)';
        
        // Create modal header
        const modalHeader = document.createElement('div');
        modalHeader.className = 'session-timeout-modal-header';
        modalHeader.innerHTML = '<h4 style="margin-top: 0; color: #dc3545;"><i class="bi bi-exclamation-triangle me-2"></i>Session Timeout Warning</h4>';
        
        // Create modal body
        const modalBody = document.createElement('div');
        modalBody.className = 'session-timeout-modal-body';
        modalBody.innerHTML = '<p>Your session is about to expire due to inactivity.</p>' +
                              '<p>You will be logged out in <span id="session-timeout-countdown">60</span> seconds.</p>';
        
        // Create modal footer
        const modalFooter = document.createElement('div');
        modalFooter.className = 'session-timeout-modal-footer';
        modalFooter.style.display = 'flex';
        modalFooter.style.justifyContent = 'space-between';
        modalFooter.style.marginTop = '15px';
        
        // Create stay logged in button
        const stayLoggedInBtn = document.createElement('button');
        stayLoggedInBtn.className = 'btn btn-primary';
        stayLoggedInBtn.textContent = 'Stay Logged In';
        stayLoggedInBtn.onclick = resetTimer;
        
        // Create logout button
        const logoutBtn = document.createElement('button');
        logoutBtn.className = 'btn btn-secondary';
        logoutBtn.textContent = 'Logout Now';
        logoutBtn.onclick = logout;
        
        // Assemble modal
        modalFooter.appendChild(stayLoggedInBtn);
        modalFooter.appendChild(logoutBtn);
        
        modalContent.appendChild(modalHeader);
        modalContent.appendChild(modalBody);
        modalContent.appendChild(modalFooter);
        
        warningModal.appendChild(modalContent);
        
        // Add to document
        document.body.appendChild(warningModal);
    }
    
    // Show the warning modal
    function showWarningModal() {
        warningShown = true;
        warningModal.style.display = 'flex';
        
        // Start countdown
        let countdown = warningMinutes * 60;
        const countdownElement = document.getElementById('session-timeout-countdown');
        
        countdownInterval = setInterval(function() {
            countdown--;
            if (countdownElement) {
                countdownElement.textContent = countdown;
            }
            
            if (countdown <= 0) {
                clearInterval(countdownInterval);
                logout();
            }
        }, 1000);
    }
    
    // Hide the warning modal
    function hideWarningModal() {
        warningShown = false;
        warningModal.style.display = 'none';
        
        // Clear countdown interval
        if (countdownInterval) {
            clearInterval(countdownInterval);
            countdownInterval = null;
        }
    }
    
    // Logout the user
    function logout() {
        window.location.href = logoutUrl;
    }
    
    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSessionTimeout);
    } else {
        initSessionTimeout();
    }
})();
