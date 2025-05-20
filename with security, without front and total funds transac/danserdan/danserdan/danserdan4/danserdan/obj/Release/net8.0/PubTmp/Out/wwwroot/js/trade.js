// Functions for stock trading
function incrementQuantity(stockId) {
    const quantityInput = document.getElementById(`quantity_${stockId}`);
    const currentPrice = parseFloat(document.getElementById(`currentPrice_${stockId}`).textContent);
    let quantity = parseInt(quantityInput.value) || 1;
    quantity++;
    quantityInput.value = quantity;
    updateTotalAmount(stockId, quantity, currentPrice);
}

function decrementQuantity(stockId) {
    const quantityInput = document.getElementById(`quantity_${stockId}`);
    const currentPrice = parseFloat(document.getElementById(`currentPrice_${stockId}`).textContent);
    let quantity = parseInt(quantityInput.value) || 2;
    if (quantity > 1) {
        quantity--;
        quantityInput.value = quantity;
        updateTotalAmount(stockId, quantity, currentPrice);
    }
}

function updateTotalAmount(stockId, quantity, price) {
    const totalAmountElement = document.getElementById(`totalAmount_${stockId}`);
    const totalAmount = (quantity * price).toFixed(2);
    totalAmountElement.textContent = totalAmount;
}

// Check if user is logged in
function isUserLoggedIn() {
    // Check if there's a user email in the session (visible in the navbar)
    return document.querySelector('.navbar .dropdown-toggle[id="userDropdown"]') !== null;
}

// Show login modal when user is not logged in
function showLoginModal() {
    console.log('User not logged in, showing login modal');
    // Find the login modal and show it using Bootstrap
    const loginModal = document.getElementById('loginModal');
    if (loginModal) {
        const bsModal = new bootstrap.Modal(loginModal);
        bsModal.show();
    } else {
        console.error('Login modal not found');
        alert('Please log in to trade stocks');
    }
}

function tradeStock(symbol, transactionType, stockId) {
    console.log(`Trade stock called: ${symbol}, ${transactionType}, ${stockId}`);
    
    // Check if user is logged in before proceeding
    if (!isUserLoggedIn()) {
        showLoginModal();
        return; // Stop execution if not logged in
    }
    
    // Use a fixed quantity of 1 for simplicity
    let quantity = 1;
    
    // Try to get the quantity from the input if it exists
    try {
        const quantityInput = document.getElementById(`quantity_${stockId}`);
        if (quantityInput) {
            quantity = parseInt(quantityInput.value) || 1;
        }
    } catch (error) {
        console.error('Error getting quantity:', error);
    }
    
    // Create a custom styled confirmation dialog
    const actionText = transactionType === 'buy' ? 'buy' : 'sell';
    const actionColor = transactionType === 'buy' ? '#4ade80' : '#ef4444';
    const iconClass = transactionType === 'buy' ? 'bi-cart-plus' : 'bi-cart-dash';
    const gradientBg = transactionType === 'buy' ? 
        'linear-gradient(135deg, rgba(74, 222, 128, 0.9), rgba(34, 197, 94, 0.9))' : 
        'linear-gradient(135deg, rgba(239, 68, 68, 0.9), rgba(220, 38, 38, 0.9))';
    
    // Remove any existing custom modals
    const existingModal = document.getElementById('customTradeConfirmModal');
    if (existingModal) {
        existingModal.remove();
    }
    
    // Create modal HTML with modern UI
    const modalHTML = `
    <div class="modal fade" id="customTradeConfirmModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content" style="background-color: #2a2d3a; border-radius: 16px; border: 1px solid rgba(255,255,255,0.1); box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                <div class="modal-header border-0 pb-0">
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body pt-0">
                    <div class="text-center mb-4">
                        <div class="mx-auto mb-3" style="width: 60px; height: 60px; background: ${gradientBg}; border-radius: 50%; display: flex; align-items: center; justify-content: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);">
                            <i class="bi ${iconClass} text-white" style="font-size: 1.5rem;"></i>
                        </div>
                        <h4 class="text-white mb-1">Confirm ${actionText.charAt(0).toUpperCase() + actionText.slice(1)}</h4>
                        <p class="text-muted">Transaction Details</p>
                    </div>
                    
                    <div class="card mb-4" style="background-color: rgba(0,0,0,0.2); border-radius: 12px; border: 1px solid rgba(255,255,255,0.05);">
                        <div class="card-body p-3">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span class="text-white-50">Symbol</span>
                                <span class="text-white fw-bold">${symbol}</span>
                            </div>
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span class="text-white-50">Quantity</span>
                                <span class="text-white fw-bold">${quantity}</span>
                            </div>
                            <div class="d-flex justify-content-between align-items-center">
                                <span class="text-white-50">Action</span>
                                <span class="text-white fw-bold text-capitalize">${actionText}</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer border-0 pt-0 d-flex justify-content-between">
                    <button type="button" class="btn btn-outline-light" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn" id="confirmTradeBtn" style="background: ${gradientBg}; color: white; border-radius: 8px; padding: 8px 16px; font-weight: 500; box-shadow: 0 4px 10px rgba(0,0,0,0.2);">
                        <i class="bi ${iconClass} me-1"></i> Confirm ${actionText.charAt(0).toUpperCase() + actionText.slice(1)}
                    </button>
                </div>
            </div>
        </div>
    </div>
    `;
    
    // Append modal to body
    document.body.insertAdjacentHTML('beforeend', modalHTML);
    
    // Get modal element and create Bootstrap modal instance
    const modalElement = document.getElementById('customTradeConfirmModal');
    const modal = new bootstrap.Modal(modalElement);
    
    // Add event listener to confirm button
    const confirmButton = document.getElementById('confirmTradeBtn');
    confirmButton.addEventListener('click', function() {
        // Call directProcessTransaction with the provided parameters
        directProcessTransaction(symbol, transactionType, stockId, quantity);
        modal.hide();
    });
    
    // Show the modal
    modal.show();
}

function directProcessTransaction(symbol, transactionType, stockId, quantity) {
    console.log(`Direct process transaction: ${symbol}, ${transactionType}, ${stockId}, ${quantity}`);
    
    // Check if user is logged in before proceeding
    if (!isUserLoggedIn()) {
        showLoginModal();
        return; // Stop execution if not logged in
    }
    
    // Show loading indicator
    const loadingHTML = `
    <div id="loadingOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
        <div style="background-color: #2a2d3a; padding: 20px; border-radius: 12px; text-align: center; box-shadow: 0 5px 15px rgba(0,0,0,0.5);">
            <div class="spinner-border text-light mb-3" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="text-white mb-0">Processing your transaction...</p>
        </div>
    </div>
    `;
    document.body.insertAdjacentHTML('beforeend', loadingHTML);
    
    // Send trade request to server
    fetch('/Stock/TradeStock', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `symbol=${symbol}&quantity=${quantity}&transactionType=${transactionType}`
    })
    .then(response => {
        console.log('Response received:', response);
        return response.json();
    })
    .then(data => {
        console.log('Data received:', data);
        
        // Remove loading overlay
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) loadingOverlay.remove();
        
        if (data.success) {
            // Show success message with modern UI
            const actionText = transactionType === 'buy' ? 'bought' : 'sold';
            const successColor = transactionType === 'buy' ? '#4ade80' : '#ef4444';
            const successIcon = transactionType === 'buy' ? 'bi-check-circle' : 'bi-check-circle';
            
            const successHTML = `
            <div id="successOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
                <div style="background-color: #2a2d3a; padding: 30px; border-radius: 16px; text-align: center; max-width: 400px; box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                    <div style="width: 70px; height: 70px; background-color: ${successColor}; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 20px;">
                        <i class="bi ${successIcon} text-white" style="font-size: 2rem;"></i>
                    </div>
                    <h4 class="text-white mb-3">Transaction Successful</h4>
                    <p class="text-white-50 mb-4">You have successfully ${actionText} ${quantity} shares of ${symbol}.</p>
                    <p class="text-white-50 mb-4">${data.message}</p>
                    <button id="successDismissBtn" class="btn btn-light px-4 py-2">Continue</button>
                </div>
            </div>
            `;
            
            document.body.insertAdjacentHTML('beforeend', successHTML);
            
            // Add event listener to dismiss button
            document.getElementById('successDismissBtn').addEventListener('click', function() {
                document.getElementById('successOverlay').remove();
                
                // Update user balance if displayed on page
                const userBalanceElement = document.getElementById('userBalance');
                if (userBalanceElement) {
                    userBalanceElement.textContent = parseFloat(data.newBalance).toFixed(2);
                }
                
                // After selling, redirect to profile page; after buying, refresh current page
                if (transactionType === 'sell') {
                    console.log('Redirecting to profile page after selling stock');
                    window.location.href = '/Account/Profile';
                } else {
                    console.log('Refreshing page after buying stock');
                    window.location.reload();
                }
            });
        } else {
            // Show error message with modern UI
            const errorHTML = `
            <div id="errorOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
                <div style="background-color: #2a2d3a; padding: 30px; border-radius: 16px; text-align: center; max-width: 400px; box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                    <div style="width: 70px; height: 70px; background-color: #ef4444; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 20px;">
                        <i class="bi bi-exclamation-circle text-white" style="font-size: 2rem;"></i>
                    </div>
                    <h4 class="text-white mb-3">Transaction Failed</h4>
                    <p class="text-white-50 mb-4">${data.message}</p>
                    <button id="errorDismissBtn" class="btn btn-light px-4 py-2">Try Again</button>
                </div>
            </div>
            `;
            
            document.body.insertAdjacentHTML('beforeend', errorHTML);
            
            // Add event listener to dismiss button
            document.getElementById('errorDismissBtn').addEventListener('click', function() {
                document.getElementById('errorOverlay').remove();
            });
        }
    })
    .catch(error => {
        console.error('Error:', error);
        
        // Remove loading overlay
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) loadingOverlay.remove();
        
        // Show error message with modern UI
        const errorHTML = `
        <div id="errorOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
            <div style="background-color: #2a2d3a; padding: 30px; border-radius: 16px; text-align: center; max-width: 400px; box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                <div style="width: 70px; height: 70px; background-color: #ef4444; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 20px;">
                    <i class="bi bi-exclamation-circle text-white" style="font-size: 2rem;"></i>
                </div>
                <h4 class="text-white mb-3">Error</h4>
                <p class="text-white-50 mb-4">An error occurred while processing your transaction. Please try again.</p>
                <button id="errorDismissBtn" class="btn btn-light px-4 py-2">Close</button>
            </div>
        </div>
        `;
        
        document.body.insertAdjacentHTML('beforeend', errorHTML);
        
        // Add event listener to dismiss button
        document.getElementById('errorDismissBtn').addEventListener('click', function() {
            document.getElementById('errorOverlay').remove();
        });
    });
}

function processTransaction(symbol, transactionType, stockId, quantity) {
    console.log(`Process transaction: ${symbol}, ${transactionType}, ${stockId}, ${quantity}`);
    
    // Check if user is logged in before proceeding
    if (!isUserLoggedIn()) {
        showLoginModal();
        return; // Stop execution if not logged in
    }
    
    // First, safely get all the elements we need, with proper error handling
    let elements = {};
    
    try {
        // Get quantity input
        elements.quantityInput = document.getElementById(`quantity_${stockId}`);
        if (!elements.quantityInput) {
            console.warn(`Quantity input not found for stock ID: ${stockId}, using default quantity of 1`);
            quantity = 1; // Use default quantity if input not found
        }
        
        // Get message element (optional)
        elements.messageElement = document.getElementById(`tradeMessage_${stockId}`);
        if (elements.messageElement) {
            elements.messageElement.textContent = "Processing transaction...";
            elements.messageElement.classList.remove("d-none", "alert-success", "alert-danger");
            elements.messageElement.classList.add("alert-info");
        }
        
        // Get modal element (optional)
        elements.modalElement = document.getElementById(`tradeModal_${stockId}`);
        if (elements.modalElement) {
            // Try to find buttons
            elements.buyButton = elements.modalElement.querySelector('.btn-success');
            elements.sellButton = elements.modalElement.querySelector('.btn-danger');
            
            // Disable buttons if found
            if (elements.buyButton) elements.buyButton.disabled = true;
            if (elements.sellButton) elements.sellButton.disabled = true;
        } else {
            console.warn(`Modal element not found for stock ID: ${stockId}, continuing without UI updates`);
        }
    } catch (error) {
        console.error('Error accessing DOM elements:', error);
        // Continue with the transaction even if we can't update the UI
    }
    
    console.log(`Sending trade request to server: symbol=${symbol}, quantity=${quantity}, transactionType=${transactionType}`);
    
    // Send trade request to server
    fetch('/Stock/TradeStock', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `symbol=${symbol}&quantity=${quantity}&transactionType=${transactionType}`
    })
    .then(response => {
        console.log('Response received:', response);
        return response.json();
    })
    .then(data => {
        console.log('Data received:', data);
        if (data.success) {
            // Show success message with confirmation modal
            const actionText = transactionType === 'buy' ? 'bought' : 'sold';
            showConfirmationModal({
                title: 'Transaction Successful',
                message: `You have successfully ${actionText} ${quantity} shares of ${symbol}. ${data.message}`,
                confirmButtonText: 'OK',
                type: 'success',
                confirmCallback: () => {
                    try {
                        // Update user balance if displayed on page
                        const userBalanceElement = document.getElementById('userBalance');
                        if (userBalanceElement) {
                            userBalanceElement.textContent = parseFloat(data.newBalance).toFixed(2);
                        }
                        
                        // Reset quantity to 1 if the input exists
                        if (elements.quantityInput) {
                            elements.quantityInput.value = 1;
                            
                            // Try to update total amount
                            const priceElement = document.getElementById(`currentPrice_${stockId}`);
                            if (priceElement) {
                                updateTotalAmount(stockId, 1, parseFloat(priceElement.textContent));
                            }
                        }
                        
                        // Close the trade modal if it exists
                        if (elements.modalElement) {
                            const modal = bootstrap.Modal.getInstance(elements.modalElement);
                            if (modal) {
                                modal.hide();
                            }
                        }
                        
                        // Reset message if it exists
                        if (elements.messageElement) {
                            elements.messageElement.classList.add("d-none");
                        }
                    } catch (error) {
                        console.error('Error updating UI after successful transaction:', error);
                        // Continue even if UI updates fail
                    }
                    
                    // If we're on the profile page and there's a portfolio, refresh it
                    if (window.location.pathname.includes('/Account/Profile') && typeof refreshPortfolio === 'function') {
                        refreshPortfolio();
                    } else if (transactionType === 'sell') {
                        // Always refresh the page after selling a stock
                        console.log('Selling stock - will refresh page');
                        setTimeout(() => {
                            window.location.reload();
                        }, 1000);
                    } else {
                        // Redirect to profile page to see updated portfolio after buying
                        setTimeout(() => {
                            window.location.href = '/Account/Profile';
                        }, 1500);
                    }
                }
            });
        } else {
            // Show error message with appropriate details
            let errorMessage = data.message;
            
            // If it's a sell transaction with not enough shares
            if (transactionType === 'sell' && data.currentShares !== undefined) {
                errorMessage = `You don't have enough shares to complete this transaction. You currently own ${data.currentShares} shares of ${symbol}, but you're trying to sell ${data.requestedShares} shares.`;
            }
            
            showConfirmationModal({
                title: 'Transaction Failed',
                message: errorMessage,
                confirmButtonText: 'OK',
                type: 'error'
            });
            
            // Also update the message in the modal if it exists
            if (elements.messageElement) {
                elements.messageElement.textContent = data.message;
                elements.messageElement.classList.remove("alert-info", "alert-success");
                elements.messageElement.classList.add("alert-danger");
            }
        }
    })
    .catch(error => {
        console.error('Error:', error);
        if (elements.messageElement) {
            elements.messageElement.textContent = "An error occurred. Please try again.";
            elements.messageElement.classList.remove("alert-info", "alert-success");
            elements.messageElement.classList.add("alert-danger");
        }
    })
    .finally(() => {
        try {
            // Re-enable buttons if they exist
            if (elements.buyButton) elements.buyButton.disabled = false;
            if (elements.sellButton) elements.sellButton.disabled = false;
            
            console.log('Transaction processing completed');
            
            // If we're on the stocks page, refresh the page after a short delay to show updated balance
            if (window.location.pathname.includes('/Home/Stocks')) {
                console.log('On stocks page, will refresh to show updated balance');
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            }
        } catch (error) {
            console.error('Error in finally block:', error);
            // Don't let errors in the finally block prevent completion
        }
    });
}

/**
 * Show a confirmation modal dialog
 * @param {Object} options - Configuration options for the modal
 * @param {string} options.title - Modal title
 * @param {string} options.message - Modal message
 * @param {string} options.confirmButtonText - Text for confirm button
 * @param {string} options.cancelButtonText - Text for cancel button
 * @param {Function} options.confirmCallback - Function to call when confirmed
 * @param {string} options.type - Modal type (success, danger, warning, info, error)
 */
function showConfirmationModal(options) {
    console.log('Showing confirmation modal with options:', options);
    
    // Check if modal already exists, remove if it does
    let existingModal = document.getElementById('confirmationModal');
    if (existingModal) {
        existingModal.remove();
    }
    
    // Create modal element
    const modalHTML = `
    <div class="modal fade" id="confirmationModal" tabindex="-1" aria-labelledby="confirmationModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content" style="background-color: #2a2d3a; border-radius: 12px; border: 1px solid rgba(255,255,255,0.1);">
                <div class="modal-header" style="border-bottom: 1px solid rgba(255,255,255,0.1);">
                    <h5 class="modal-title text-white" id="confirmationModalLabel">${options.title}</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <p class="text-white">${options.message}</p>
                </div>
                <div class="modal-footer" style="border-top: 1px solid rgba(255,255,255,0.1);">
                    ${options.cancelButtonText ? `<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${options.cancelButtonText}</button>` : ''}
                    <button type="button" class="btn btn-${options.type === 'error' ? 'danger' : options.type || 'primary'}" id="confirmModalBtn">${options.confirmButtonText || 'Confirm'}</button>
                </div>
            </div>
        </div>
    </div>
    `;
    
    // Append modal to body
    document.body.insertAdjacentHTML('beforeend', modalHTML);
    
    // Get modal element and create Bootstrap modal instance
    const modalElement = document.getElementById('confirmationModal');
    let modal;
    
    // Check if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        modal = new bootstrap.Modal(modalElement);
    } else {
        console.error('Bootstrap is not loaded. Cannot create modal.');
        // Fallback to direct function call if modal can't be created
        if (typeof options.confirmCallback === 'function') {
            options.confirmCallback();
        }
        return null;
    }
    
    // Add event listener to confirm button
    const confirmButton = document.getElementById('confirmModalBtn');
    if (confirmButton) {
        console.log('Adding event listener to confirm button');
        confirmButton.addEventListener('click', function() {
            console.log('Confirm button clicked');
            if (typeof options.confirmCallback === 'function') {
                console.log('Executing confirm callback');
                options.confirmCallback();
            }
            modal.hide();
        });
    } else {
        console.error('Confirm button not found in modal');
    }
    
    // Add event listener for when modal is fully shown
    modalElement.addEventListener('shown.bs.modal', function() {
        console.log('Modal fully shown');
    });
    
    // Add event listener for when modal is hidden
    modalElement.addEventListener('hidden.bs.modal', function() {
        console.log('Modal hidden');
    });
    
    // Show modal
    modal.show();
    
    // Return modal instance for further manipulation
    return modal;
}

/**
 * Sell stock from portfolio with modern UI
 */
function sellStock(symbol, stockId, quantity) {
    console.log(`Sell stock: ${symbol}, ${stockId}, ${quantity}`);
    
    // Check if user is logged in before proceeding
    if (!isUserLoggedIn()) {
        showLoginModal();
        return; // Stop execution if not logged in
    }
    
    // Create modern sell confirmation dialog
    const sellGradient = 'linear-gradient(135deg, rgba(239, 68, 68, 0.9), rgba(220, 38, 38, 0.9))';
    
    // Remove any existing modals
    const existingModal = document.getElementById('sellStockModal');
    if (existingModal) {
        existingModal.remove();
    }
    
    // Create modal HTML with modern UI
    const modalHTML = `
    <div class="modal fade" id="sellStockModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content" style="background-color: #2a2d3a; border-radius: 16px; border: 1px solid rgba(255,255,255,0.1); box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                <div class="modal-header border-0 pb-0">
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body pt-0">
                    <div class="text-center mb-4">
                        <div class="mx-auto mb-3" style="width: 60px; height: 60px; background: ${sellGradient}; border-radius: 50%; display: flex; align-items: center; justify-content: center; box-shadow: 0 4px 15px rgba(0,0,0,0.2);">
                            <i class="bi bi-cart-dash text-white" style="font-size: 1.5rem;"></i>
                        </div>
                        <h4 class="text-white mb-1">Confirm Sell</h4>
                        <p class="text-muted">You are about to sell your shares</p>
                    </div>
                    
                    <div class="card mb-4" style="background-color: rgba(0,0,0,0.2); border-radius: 12px; border: 1px solid rgba(255,255,255,0.05);">
                        <div class="card-body p-3">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span class="text-white-50">Symbol</span>
                                <span class="text-white fw-bold">${symbol}</span>
                            </div>
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span class="text-white-50">Quantity</span>
                                <span class="text-white fw-bold">${quantity}</span>
                            </div>
                            <div class="d-flex justify-content-between align-items-center">
                                <span class="text-white-50">Action</span>
                                <span class="text-white fw-bold text-capitalize">Sell All Shares</span>
                            </div>
                        </div>
                    </div>
                    
                    <div class="alert" style="background-color: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.2); border-radius: 8px;">
                        <div class="d-flex">
                            <div class="me-3">
                                <i class="bi bi-info-circle text-danger"></i>
                            </div>
                            <div>
                                <p class="text-white-50 mb-0">This action will sell all your shares of ${symbol}. The transaction cannot be undone.</p>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer border-0 pt-0 d-flex justify-content-between">
                    <button type="button" class="btn btn-outline-light" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn" id="confirmSellBtn" style="background: ${sellGradient}; color: white; border-radius: 8px; padding: 8px 16px; font-weight: 500; box-shadow: 0 4px 10px rgba(0,0,0,0.2);">
                        <i class="bi bi-cart-dash me-1"></i> Confirm Sell
                    </button>
                </div>
            </div>
        </div>
    </div>
    `;
    
    // Append modal to body
    document.body.insertAdjacentHTML('beforeend', modalHTML);
    
    // Get modal element and create Bootstrap modal instance
    const modalElement = document.getElementById('sellStockModal');
    const modal = new bootstrap.Modal(modalElement);
    
    // Add event listener to confirm button
    const confirmButton = document.getElementById('confirmSellBtn');
    confirmButton.addEventListener('click', function() {
        // Process the sell transaction
        processSellTransaction(symbol, stockId, quantity);
        modal.hide();
    });
    
    // Show the modal
    modal.show();
}

/**
 * Process sell transaction with modern UI
 */
function processSellTransaction(symbol, stockId, quantity) {
    console.log(`Processing sell transaction: ${symbol}, ID: ${stockId}, Quantity: ${quantity}`);
    
    // Show loading indicator
    const loadingHTML = `
    <div id="loadingOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
        <div style="background-color: #2a2d3a; padding: 20px; border-radius: 12px; text-align: center; box-shadow: 0 5px 15px rgba(0,0,0,0.5);">
            <div class="spinner-border text-light mb-3" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="text-white mb-0">Processing your transaction...</p>
        </div>
    </div>
    `;
    document.body.insertAdjacentHTML('beforeend', loadingHTML);
    
    // Send trade request to server
    fetch('/Stock/TradeStock', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `symbol=${symbol}&quantity=${quantity}&transactionType=sell`
    })
    .then(response => {
        console.log('Response received:', response);
        return response.json();
    })
    .then(data => {
        console.log('Data received:', data);
        
        // Remove loading overlay
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) loadingOverlay.remove();
        
        if (data.success) {
            // Show success message with modern UI
            const successHTML = `
            <div id="successOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
                <div style="background-color: #2a2d3a; padding: 30px; border-radius: 16px; text-align: center; max-width: 400px; box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                    <div style="width: 70px; height: 70px; background-color: #ef4444; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 20px;">
                        <i class="bi bi-check-circle text-white" style="font-size: 2rem;"></i>
                    </div>
                    <h4 class="text-white mb-3">Transaction Successful</h4>
                    <p class="text-white-50 mb-4">You have successfully sold ${quantity} shares of ${symbol}.</p>
                    <p class="text-white-50 mb-4">${data.message}</p>
                    <button id="successDismissBtn" class="btn btn-light px-4 py-2">Continue</button>
                </div>
            </div>
            `;
            
            document.body.insertAdjacentHTML('beforeend', successHTML);
            
            // Add event listener to dismiss button
            document.getElementById('successDismissBtn').addEventListener('click', function() {
                document.getElementById('successOverlay').remove();
                
                // Update user balance if displayed on page
                const userBalanceElement = document.getElementById('userBalance');
                if (userBalanceElement) {
                    userBalanceElement.textContent = parseFloat(data.newBalance).toFixed(2);
                }
                
                // Refresh the page to show updated portfolio
                console.log('Refreshing page after selling stock');
                window.location.reload();
            });
        } else {
            // Show error message with modern UI
            const errorHTML = `
            <div id="errorOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
                <div style="background-color: #2a2d3a; padding: 30px; border-radius: 16px; text-align: center; max-width: 400px; box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                    <div style="width: 70px; height: 70px; background-color: #ef4444; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 20px;">
                        <i class="bi bi-exclamation-circle text-white" style="font-size: 2rem;"></i>
                    </div>
                    <h4 class="text-white mb-3">Transaction Failed</h4>
                    <p class="text-white-50 mb-4">${data.message}</p>
                    <button id="errorDismissBtn" class="btn btn-light px-4 py-2">Try Again</button>
                </div>
            </div>
            `;
            
            document.body.insertAdjacentHTML('beforeend', errorHTML);
            
            // Add event listener to dismiss button
            document.getElementById('errorDismissBtn').addEventListener('click', function() {
                document.getElementById('errorOverlay').remove();
            });
        }
    })
    .catch(error => {
        console.error('Error:', error);
        
        // Remove loading overlay
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) loadingOverlay.remove();
        
        // Show error message with modern UI
        const errorHTML = `
        <div id="errorOverlay" style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">
            <div style="background-color: #2a2d3a; padding: 30px; border-radius: 16px; text-align: center; max-width: 400px; box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
                <div style="width: 70px; height: 70px; background-color: #ef4444; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 20px;">
                    <i class="bi bi-exclamation-circle text-white" style="font-size: 2rem;"></i>
                </div>
                <h4 class="text-white mb-3">Error</h4>
                <p class="text-white-50 mb-4">An error occurred while processing your transaction. Please try again.</p>
                <button id="errorDismissBtn" class="btn btn-light px-4 py-2">Close</button>
            </div>
        </div>
        `;
        
        document.body.insertAdjacentHTML('beforeend', errorHTML);
        
        // Add event listener to dismiss button
        document.getElementById('errorDismissBtn').addEventListener('click', function() {
            document.getElementById('errorOverlay').remove();
        });
    });
}

// Initialize all trade functionality when the document is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Trade.js initialized');
    
    // Add event listeners to quantity inputs
    const quantityInputs = document.querySelectorAll('[id^="quantity_"]');
    console.log(`Found ${quantityInputs.length} quantity inputs`);
    
    quantityInputs.forEach(input => {
        const stockId = input.id.split('_')[1];
        const currentPriceElement = document.getElementById(`currentPrice_${stockId}`);
        
        if (currentPriceElement) {
            const currentPrice = parseFloat(currentPriceElement.textContent);
            
            input.addEventListener('change', function() {
                let quantity = parseInt(this.value) || 1;
                if (quantity < 1) {
                    quantity = 1;
                    this.value = 1;
                }
                updateTotalAmount(stockId, quantity, currentPrice);
            });
        }
    });
    
    // Add event listeners to buy and sell buttons
    const tradeButtons = document.querySelectorAll('[onclick^="tradeStock"]');
    console.log(`Found ${tradeButtons.length} trade buttons`);
    
    tradeButtons.forEach(button => {
        // Extract the original onclick attribute
        const onclickAttr = button.getAttribute('onclick');
        
        // Remove the original onclick attribute
        button.removeAttribute('onclick');
        
        // Add a click event listener instead
        button.addEventListener('click', function(event) {
            event.preventDefault();
            console.log(`Button clicked: ${onclickAttr}`);
            
            // Parse the parameters from the onclick attribute
            const match = onclickAttr.match(/tradeStock\('([^']+)',\s*'([^']+)',\s*'([^']+)'\)/);
            if (match) {
                const symbol = match[1];
                const transactionType = match[2];
                const stockId = match[3];
                
                console.log(`Calling tradeStock with: ${symbol}, ${transactionType}, ${stockId}`);
                tradeStock(symbol, transactionType, stockId);
            } else {
                console.error(`Could not parse onclick attribute: ${onclickAttr}`);
            }
        });
    });
});
