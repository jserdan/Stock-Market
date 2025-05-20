// Admin functionality for managing stocks and users
document.addEventListener('DOMContentLoaded', function() {
    // Initialize any admin-specific components
    console.log('Admin dashboard initialized');
    
    // Initialize pagination if on the AllStocks page
    if (document.getElementById('stocksTableBody')) {
        initializeStocksPagination();
    }
});

/**
 * Initialize pagination for the stocks table
 */
function initializeStocksPagination() {
    // This function will be called by the inline script in AllStocks.cshtml
    // It's defined here to keep the admin.js file as the central place for admin functionality
    console.log('Stocks pagination initialized');
}

/**
 * Toggle stock availability status
 * @param {number} stockId - The ID of the stock to update
 * @param {boolean} isAvailable - Whether the stock should be available or not
 */
function toggleStockAvailability(stockId, isAvailable) {
    // Show loading state
    const statusElement = document.getElementById(`availabilityStatus_${stockId}`);
    const originalText = statusElement.textContent;
    statusElement.textContent = 'Updating...';
    
    // Send request to update stock availability
    fetch('/Admin/UpdateStockAvailability', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `stockId=${stockId}&isAvailable=${isAvailable}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update the status text
            statusElement.textContent = isAvailable ? 'Available' : 'Unavailable';
            
            // Show success message
            const toast = new bootstrap.Toast(document.getElementById('successToast'));
            document.getElementById('toastMessage').textContent = data.message;
            toast.show();
        } else {
            // Revert the toggle if there was an error
            const toggleElement = document.getElementById(`availabilityToggle_${stockId}`);
            toggleElement.checked = !isAvailable;
            statusElement.textContent = !isAvailable ? 'Available' : 'Unavailable';
            
            // Show error message
            const toast = new bootstrap.Toast(document.getElementById('errorToast'));
            document.getElementById('errorToastMessage').textContent = data.message;
            toast.show();
        }
    })
    .catch(error => {
        console.error('Error:', error);
        
        // Revert the toggle if there was an error
        const toggleElement = document.getElementById(`availabilityToggle_${stockId}`);
        toggleElement.checked = !isAvailable;
        statusElement.textContent = !isAvailable ? 'Available' : 'Unavailable';
        
        // Show error message
        const toast = new bootstrap.Toast(document.getElementById('errorToast'));
        document.getElementById('errorToastMessage').textContent = 'An error occurred. Please try again.';
        toast.show();
    });
}
