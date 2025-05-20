// Portfolio management functions

/**
 * Refresh the prices of all stocks in the portfolio
 */
function refreshPortfolio() {
    // Find all stock IDs in the portfolio
    const priceElements = document.querySelectorAll('[id^="currentPrice_"]');
    
    // If no portfolio items, return
    if (priceElements.length === 0) return;
    
    // Show loading indicator for all price elements
    priceElements.forEach(element => {
        element.innerHTML = '<i class="bi bi-arrow-repeat spin"></i>';
    });
    
    // Show toast notification
    showToast('Refreshing stock prices...', 'info');
    
    // Use the new endpoint to refresh all prices at once
    fetch('/Stock/RefreshPortfolioPrices')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update each stock with the refreshed price
                priceElements.forEach(element => {
                    const stockId = element.id.split('_')[1];
                    
                    // Find the updated stock data
                    const updatedStock = data.updatedStocks.find(s => s.stockId == stockId);
                    
                    if (updatedStock) {
                        // Update price display
                        const currentPrice = parseFloat(updatedStock.price);
                        element.textContent = `$${currentPrice.toFixed(2)}`;
                        
                        // Update total value
                        const quantity = parseInt(element.closest('tr').querySelector('td:nth-child(2)').textContent);
                        const totalValue = currentPrice * quantity;
                        const totalValueElement = document.getElementById(`totalValue_${stockId}`);
                        if (totalValueElement) {
                            totalValueElement.textContent = `$${totalValue.toFixed(2)}`;
                        }
                        
                        // Update profit/loss
                        const purchasePriceText = element.closest('tr').querySelector('td:nth-child(3)').textContent;
                        const purchasePrice = parseFloat(purchasePriceText.replace('$', ''));
                        const profitLoss = (currentPrice - purchasePrice) * quantity;
                        const profitLossPercentage = ((currentPrice - purchasePrice) / purchasePrice * 100).toFixed(2);
                        
                        const profitLossElement = document.getElementById(`profitLoss_${stockId}`);
                        if (profitLossElement) {
                            const sign = profitLoss >= 0 ? '+' : '';
                            profitLossElement.textContent = `${sign}$${Math.abs(profitLoss).toFixed(2)} (${sign}${profitLossPercentage}%)`;
                            
                            // Update class for color
                            const tdElement = profitLossElement.closest('td');
                            tdElement.className = profitLoss >= 0 ? 'text-success' : 'text-danger';
                            
                            // Update icon
                            const iconElement = tdElement.querySelector('i');
                            iconElement.className = profitLoss >= 0 ? 'bi bi-graph-up-arrow me-1' : 'bi bi-graph-down-arrow me-1';
                        }
                    } else {
                        // If we didn't get updated data for this stock, show the current price
                        element.textContent = element.getAttribute('data-original-price') || 'N/A';
                    }
                });
                
                showToast('Stock prices updated successfully!', 'success');
            } else {
                // Show error for all price elements
                priceElements.forEach(element => {
                    element.textContent = element.getAttribute('data-original-price') || 'Error';
                });
                
                showToast('Failed to update stock prices', 'error');
            }
        })
        .catch(error => {
            console.error('Error refreshing portfolio prices:', error);
            
            // Show error for all price elements
            priceElements.forEach(element => {
                element.textContent = element.getAttribute('data-original-price') || 'Error';
            });
            
            showToast('Error refreshing portfolio prices', 'error');
        });
}

/**
 * Show a toast notification
 * @param {string} message - Message to display
 * @param {string} type - Type of toast (success, error, info, warning)
 */
function showToast(message, type = 'info') {
    // Check if toast container exists, if not create it
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'position-fixed bottom-0 end-0 p-3';
        toastContainer.style.zIndex = '1050';
        document.body.appendChild(toastContainer);
    }
    
    // Create toast element
    const toastId = `toast-${Date.now()}`;
    const toastElement = document.createElement('div');
    toastElement.id = toastId;
    toastElement.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : type}`;
    toastElement.setAttribute('role', 'alert');
    toastElement.setAttribute('aria-live', 'assertive');
    toastElement.setAttribute('aria-atomic', 'true');
    
    // Create toast content
    toastElement.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    // Add toast to container
    toastContainer.appendChild(toastElement);
    
    // Initialize and show the toast
    const toast = new bootstrap.Toast(toastElement, { delay: 3000 });
    toast.show();
}

/**
 * Show a confirmation modal dialog
 * @param {Object} options - Modal options
 * @param {string} options.title - Modal title
 * @param {string} options.body - Modal body HTML
 * @param {string} options.confirmButtonText - Text for confirm button
 * @param {string} options.confirmButtonClass - Class for confirm button
 * @param {Function} options.onConfirm - Callback when confirmed
 */
function showConfirmationModal(options) {
    // Create modal element
    const modalId = `modal-${Date.now()}`;
    const modalHTML = `
        <div class="modal fade" id="${modalId}" tabindex="-1" aria-labelledby="${modalId}-label" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content bg-dark text-white">
                    <div class="modal-header">
                        <h5 class="modal-title" id="${modalId}-label">${options.title || 'Confirm'}</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        ${options.body || ''}
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn ${options.confirmButtonClass || 'btn-primary'}" id="${modalId}-confirm">
                            ${options.confirmButtonText || 'Confirm'}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHTML);
    
    // Get modal element
    const modalElement = document.getElementById(modalId);
    const modal = new bootstrap.Modal(modalElement);
    
    // Add event listener to confirm button
    const confirmButton = document.getElementById(`${modalId}-confirm`);
    confirmButton.addEventListener('click', () => {
        if (typeof options.onConfirm === 'function') {
            options.onConfirm();
        }
        modal.hide();
    });
    
    // Show modal
    modal.show();
    
    // Remove modal from DOM when hidden
    modalElement.addEventListener('hidden.bs.modal', () => {
        modalElement.remove();
    });
}

/**
 * Open sell modal for a stock
 * @param {string} symbol - Stock symbol
 * @param {number} stockId - Stock ID
 * @param {number} sharesOwned - Number of shares owned
 */
function sellStock(symbol, stockId, sharesOwned) {
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
                        <h4 class="text-white mb-1">Sell ${symbol}</h4>
                        <p class="text-muted">You own ${sharesOwned} shares</p>
                    </div>
                    
                    <div class="card mb-4" style="background-color: rgba(0,0,0,0.2); border-radius: 12px; border: 1px solid rgba(255,255,255,0.05);">
                        <div class="card-body p-3">
                            <div class="form-group mb-3">
                                <label for="sellQuantity_${stockId}" class="text-white-50 mb-2">Quantity to sell:</label>
                                <div class="input-group">
                                    <button class="btn" style="background-color: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.1); color: white;" type="button" onclick="decrementSellQuantity('${stockId}', ${sharesOwned})">
                                        <i class="bi bi-dash"></i>
                                    </button>
                                    <input type="number" class="form-control" id="sellQuantity_${stockId}" min="1" max="${sharesOwned}" value="1" style="background-color: rgba(0,0,0,0.2); border: 1px solid rgba(255,255,255,0.1); color: white; text-align: center;">
                                    <button class="btn" style="background-color: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.1); color: white;" type="button" onclick="incrementSellQuantity('${stockId}', ${sharesOwned})">
                                        <i class="bi bi-plus"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="alert" style="background-color: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.2); border-radius: 8px;">
                        <div class="d-flex">
                            <div class="me-3">
                                <i class="bi bi-info-circle text-danger"></i>
                            </div>
                            <div>
                                <p class="text-white-50 mb-0">This action will sell your shares of ${symbol}. The transaction cannot be undone.</p>
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
        const quantity = parseInt(document.getElementById(`sellQuantity_${stockId}`).value) || 1;
        if (quantity <= 0 || quantity > sharesOwned) {
            showToast(`Invalid quantity. You own ${sharesOwned} shares.`, 'error');
            return;
        }
        
        // Process the sell transaction
        processStockTransaction(symbol, 'sell', stockId, quantity);
        modal.hide();
    });
    
    // Show the modal
    modal.show();
}

/**
 * Increment sell quantity
 */
function incrementSellQuantity(stockId, maxShares) {
    const input = document.getElementById(`sellQuantity_${stockId}`);
    let value = parseInt(input.value) || 0;
    if (value < maxShares) {
        input.value = value + 1;
    }
}

/**
 * Decrement sell quantity
 */
function decrementSellQuantity(stockId, maxShares) {
    const input = document.getElementById(`sellQuantity_${stockId}`);
    let value = parseInt(input.value) || 2;
    if (value > 1) {
        input.value = value - 1;
    }
}

/**
 * Process a stock transaction (buy or sell)
 * @param {string} symbol - Stock symbol
 * @param {string} transactionType - Transaction type (buy or sell)
 * @param {number} stockId - Stock ID
 * @param {number} quantity - Quantity to buy or sell
 */
function processStockTransaction(symbol, transactionType, stockId, quantity) {
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
    
    console.log('Transaction details:', { symbol, transactionType, stockId, quantity });
    
    // Call the TradeStock endpoint
    const formData = new FormData();
    formData.append('symbol', symbol);
    formData.append('quantity', quantity);
    formData.append('transactionType', transactionType);
    formData.append('stockId', stockId);
    
    fetch('/Stock/TradeStock', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        console.log('Transaction response:', data);
        
        // Remove loading overlay
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) loadingOverlay.remove();
        
        if (data.success) {
            // Show success message with modern UI
            const successColor = transactionType === 'buy' ? '#4ade80' : '#ef4444';
            const successIcon = 'bi-check-circle';
            const actionText = transactionType === 'buy' ? 'bought' : 'sold';
            
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
                if (userBalanceElement && data.newBalance) {
                    userBalanceElement.textContent = `$${parseFloat(data.newBalance).toFixed(2)}`;
                }
                
                // Refresh the page after selling a stock
                if (transactionType === 'sell') {
                    console.log('Refreshing page after selling stock');
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
        console.error('Error processing transaction:', error);
        
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

// Add spin animation for refresh icon
const style = document.createElement('style');
style.textContent = `
    .spin {
        animation: spin 1s linear infinite;
    }
    @keyframes spin {
        0% { transform: rotate(0deg); }
        100% { transform: rotate(360deg); }
    }
`;
document.head.appendChild(style);

// Initialize when the document is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Portfolio.js initialized');
});
