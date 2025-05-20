// Stock chart and real-time update functionality
// Functions for quantity increment/decrement
function increment(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        let value = parseInt(input.value) || 1;
        input.value = value + 1;
    }
}

function decrement(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        let value = parseInt(input.value) || 2;
        if (value > 1) {
            input.value = value - 1;
        }
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Store chart instances and stock data
    const chartInstances = {};
    const stockData = {};
    const previousPrices = {};
    // Use localStorage to persist base prices per symbol
    const basePrices = {};
    
    // Setup search functionality
    const searchInput = document.getElementById('stockSearch');
    if (searchInput) {
        searchInput.addEventListener('input', filterStocks);
    }
    
    // Setup sector filter functionality
    const sectorDropdown = document.getElementById('sectorDropdown');
    const filterForm = document.getElementById('filterForm');
    
    if (sectorDropdown && filterForm) {
        // Make the dropdown interactive
        sectorDropdown.addEventListener('change', function() {
            console.log('Sector selected:', this.value);
        });
        
        // Ensure the dropdown is clickable and selectable
        sectorDropdown.addEventListener('click', function(e) {
            e.stopPropagation();
        });
        
        // Collect all unique sectors from the stock items
        const stockSectors = new Set();
        document.querySelectorAll('.stock-sector-badge').forEach(badge => {
            if (badge.textContent && badge.textContent.trim()) {
                stockSectors.add(badge.textContent.trim());
            }
        });
        
        // Log the sectors found in stock items
        console.log('Sectors found in stock items:', Array.from(stockSectors));
        
        // Check if all sectors are in the dropdown
        const dropdownSectors = new Set(Array.from(sectorDropdown.options).map(opt => opt.value));
        console.log('Sectors in dropdown:', Array.from(dropdownSectors));
        
        // Add any missing sectors to the dropdown
        stockSectors.forEach(sector => {
            if (sector && !dropdownSectors.has(sector)) {
                console.log('Adding missing sector to dropdown:', sector);
                const option = document.createElement('option');
                option.value = sector;
                option.textContent = sector;
                sectorDropdown.appendChild(option);
            }
        });
        
        // Ensure the form submission includes the sector value
        filterForm.addEventListener('submit', function(e) {
            console.log('Form submitted with sector:', sectorDropdown.value);
            
            // Ensure the sector value is included in the form submission
            if (sectorDropdown.value) {
                // Create a hidden input if it doesn't exist
                let sectorInput = document.getElementById('hiddenSectorInput');
                if (!sectorInput) {
                    sectorInput = document.createElement('input');
                    sectorInput.type = 'hidden';
                    sectorInput.id = 'hiddenSectorInput';
                    sectorInput.name = 'sector';
                    this.appendChild(sectorInput);
                }
                sectorInput.value = sectorDropdown.value;
            }
        });
    }
    function getBasePrice(symbol) {
        const key = `basePrice_${symbol}`;
        const val = localStorage.getItem(key);
        return val ? parseFloat(val) : undefined;
    }
    function setBasePrice(symbol, price) {
        const key = `basePrice_${symbol}`;
        localStorage.setItem(key, price);
        basePrices[symbol] = price;
    }
    let currentOpenModal = null;
    let updateInterval = null; // Track the update interval
    
    // Currency conversion constants and variables
    const USD_TO_PHP_RATE = 56.5; // Fixed exchange rate
    let currentCurrency = localStorage.getItem('preferredCurrency') || 'USD';
    
    // Listen for currency changes from the layout
    document.addEventListener('currencyChanged', function(event) {
        currentCurrency = event.detail.currency;
        // Update all stock prices with the new currency
        updateAllStockPrices();
    });
    
    // Function to convert price based on selected currency
    function convertPrice(usdPrice, currency) {
        // Handle different input formats and ensure we have a clean number
        let cleanPrice;
        if (!usdPrice) return '0.00';
        
        // If usdPrice is a string that might contain currency symbols or commas
        if (typeof usdPrice === 'string') {
            // Remove any currency symbols and commas
            cleanPrice = usdPrice.replace(/[$₱,]/g, '');
        } else {
            cleanPrice = usdPrice;
        }
        
        // Parse to float and check if it's a valid number
        const numericPrice = parseFloat(cleanPrice);
        if (isNaN(numericPrice)) return '0.00';
        
        // Convert based on currency
        if (currency === 'PHP') {
            return (numericPrice * USD_TO_PHP_RATE).toFixed(2);
        } else {
            return numericPrice.toFixed(2);
        }
    }
    
    // Function to format price with currency symbol
    function formatPrice(price, currency) {
        const symbol = currency === 'USD' ? '$' : '₱';
        return symbol + price;
    }
    
    // Function to update all stock prices with current currency
    function updateAllStockPrices() {
        Object.keys(stockData).forEach(symbol => {
            const priceElement = document.querySelector(`.stock-item[data-symbol="${symbol}"] .stock-price`);
            if (priceElement && stockData[symbol].priceUsd) {
                const convertedPrice = convertPrice(stockData[symbol].priceUsd, currentCurrency);
                priceElement.textContent = formatPrice(convertedPrice, currentCurrency);
                
                // Also update the price in the modal if it's open
                if (currentOpenModal === symbol) {
                    const modalPriceElement = document.querySelector(`#chartModal${Object.keys(stockData).indexOf(symbol) + 1} .modal-price`);
                    if (modalPriceElement) {
                        modalPriceElement.textContent = formatPrice(convertedPrice, currentCurrency);
                    }
                }
            }
        });
    }
    
    // Function to create a new chart
    function createChart(canvasId, chartData) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        // Generate time labels for real-time updates
        const now = new Date();
        const labels = [];
        for (let i = 6; i >= 0; i--) {
            const time = new Date(now);
            time.setSeconds(now.getSeconds() - i);
            labels.push(time.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
        }
        
        // Determine if price is going up or down by comparing first and last values
        let priceDirection = 'same';
        if (chartData.length > 0 && chartData[0].data.length > 1) {
            const firstValue = chartData[0].data[0];
            const lastValue = chartData[0].data[chartData[0].data.length - 1];
            priceDirection = lastValue > firstValue ? 'up' : lastValue < firstValue ? 'down' : 'same';
        }
        
        // Set the chart color based on price direction
        const chartColor = priceDirection === 'up' ? '#22c55e' : priceDirection === 'down' ? '#ef4444' : '#6366f1';
        
        // Process datasets
        const datasets = chartData.map(dataset => ({
            label: dataset.label,
            data: dataset.data,
            borderColor: chartColor, // Use the dynamic color based on price direction
            borderWidth: dataset.borderWidth,
            tension: dataset.tension || 0.4,
            fill: dataset.fill || false,
            pointRadius: 0
        }));
        
        // Calculate min and max for Y axis
        let allValues = [];
        datasets.forEach(ds => {
            ds.data.forEach(val => {
                if (val !== null) allValues.push(val);
            });
        });
        
        const min = allValues.length ? Math.floor(Math.min(...allValues) * 0.95) : 0;
        const max = allValues.length ? Math.ceil(Math.max(...allValues) * 1.05) : 100;
        
        return new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 0 // Disable animation for better performance
                },
                scales: {
                    x: {
                        grid: {
                            color: '#2a2d3a'
                        },
                        ticks: {
                            color: '#94a3b8'
                        }
                    },
                    y: {
                        min: min,
                        max: max,
                        grid: {
                            color: '#2a2d3a'
                        },
                        ticks: {
                            color: '#94a3b8'
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        backgroundColor: '#1a1a24',
                        titleColor: '#fff',
                        bodyColor: '#94a3b8',
                        borderColor: '#2a2d3a',
                        borderWidth: 1
                    }
                }
            }
        });
    }
    
    // Function to update existing chart
    function updateChart(chart, symbol, currentPrice) {
        // Generate new time labels
        const now = new Date();
        const newLabels = [];
        for (let i = 6; i >= 0; i--) {
            const time = new Date(now);
            time.setSeconds(now.getSeconds() - i);
            newLabels.push(time.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
        }
        
        // Get the first dataset to determine price direction
        const firstDataset = chart.data.datasets[0];
        let priceDirection = 'same';
        
        if (firstDataset && firstDataset.data && firstDataset.data.length > 0) {
            const lastDataPoint = firstDataset.data[firstDataset.data.length - 1];
            priceDirection = currentPrice > lastDataPoint ? 'up' : 
                           currentPrice < lastDataPoint ? 'down' : 'same';
        }
        
        // Set chart color based on price direction
        const chartColor = priceDirection === 'up' ? '#22c55e' : 
                          priceDirection === 'down' ? '#ef4444' : '#6366f1';
        
        // Update datasets with the new data point
        const datasets = chart.data.datasets.map(dataset => {
            if (dataset.data && dataset.data.length > 0) {
                // Shift data points left (remove oldest point)
                const newData = [...dataset.data.slice(1)];
                
                // Add new data point based on current price
                if (dataset.label === 'Price') {
                    if (currentPrice) {
                        newData.push(currentPrice);
                        console.log(`Updated chart for ${symbol} with latest price: ${currentPrice}`);
                    } else {
                        // Fallback to the last value with a small variation if we can't get the current price
                        const lastValue = dataset.data[dataset.data.length - 1] || 100;
                        const variation = (Math.random() * 4) - 2; // Small variation
                        newData.push(Math.max(lastValue + variation, 1));
                    }
                } else {
                    // For other datasets, keep their data as is
                    newData.push(dataset.data[dataset.data.length - 1]);
                }
                
                return {
                    label: dataset.label,
                    data: newData,
                    borderColor: chartColor, // Use dynamic color based on price direction
                    borderWidth: dataset.borderWidth,
                    tension: dataset.tension || 0.4,
                    fill: dataset.fill || false,
                    pointRadius: 0
                };
            } else {
                // If no data, return the dataset as is but update the color
                return {
                    ...dataset,
                    borderColor: chartColor
                };
            }
        });
        
        // Calculate min and max for Y axis with more padding to show price changes better
        let allValues = [];
        datasets.forEach(ds => {
            ds.data.forEach(val => {
                if (val !== null) allValues.push(val);
            });
        });
        
        // Use a wider range to better show the ±$30 price changes
        const min = allValues.length ? Math.floor(Math.min(...allValues) * 0.9) : 0;
        const max = allValues.length ? Math.ceil(Math.max(...allValues) * 1.1) : 100;
        
        // Update chart
        chart.data.labels = newLabels;
        chart.data.datasets = datasets;
        chart.options.scales.y.min = min;
        chart.options.scales.y.max = max;
        chart.update('none'); // Update without animation for better performance
    }
    
    // Function to update stock UI with pulse effect
    function updateStockUI(symbol, data) {
        // Find the stock item elements
        const priceElement = document.querySelector(`.stock-item[data-symbol="${symbol}"] .stock-price`);
        const changeElement = document.querySelector(`.stock-item[data-symbol="${symbol}"] .stock-change`);
        
        // Get current and previous price for comparison (always in USD)
        let currentPrice = 0;
        if (data.priceUsd) {
            // If we already have the USD price stored, use it
            currentPrice = parseFloat(data.priceUsd);
        } else if (data.price) {
            // Extract the numeric value from the price string, regardless of currency
            const priceString = data.price.toString();
            // Check if the price is in PHP (has ₱ symbol)
            const isPhp = priceString.includes('₱');
            // Remove all currency symbols and commas
            const cleanPrice = priceString.replace(/[$₱,]/g, '');
            // Parse to float
            const numericPrice = parseFloat(cleanPrice);
            // Convert back to USD if it was in PHP
            currentPrice = isPhp ? (numericPrice / USD_TO_PHP_RATE) : numericPrice;
        }
        
        const prevPrice = previousPrices[symbol] || currentPrice;
        
        // Determine if price went up or down
        const priceDirection = currentPrice > prevPrice ? 'up' : 
                            currentPrice < prevPrice ? 'down' : 'same';
        
        // Update previous price for next comparison
        previousPrices[symbol] = currentPrice;
        
        // Store the USD price for future reference
        data.priceUsd = currentPrice.toString();
        
        // Convert price based on selected currency
        const convertedPrice = convertPrice(data.priceUsd, currentCurrency);
        const formattedPrice = formatPrice(convertedPrice, currentCurrency);
        
        // Update price with pulse effect
        if (priceElement) {
            priceElement.textContent = formattedPrice;
            priceElement.setAttribute('data-price-usd', currentPrice.toString());
            priceElement.classList.remove('pulse-up', 'pulse-down');
            if (priceDirection !== 'same') {
                priceElement.classList.add(priceDirection === 'up' ? 'pulse-up' : 'pulse-down');
                setTimeout(() => {
                    priceElement.classList.remove('pulse-up', 'pulse-down');
                }, 1000);
            }
        }
        
        // Calculate the actual change percentage based on the price direction
        let changeValue;
        let formattedChange;
        
        // Check if data.change is a valid string that can be parsed
        if (data.change && typeof data.change === 'string') {
            // Try to extract just the percentage value from the change string
            const percentMatch = data.change.match(/\(([-+]?[0-9]*\.?[0-9]+)%\)/);
            if (percentMatch && percentMatch[1]) {
                // If we found a percentage in parentheses, use that
                changeValue = parseFloat(percentMatch[1]);
            } else {
                // Otherwise try to parse the whole string
                changeValue = parseFloat(data.change.replace('%', '').replace('+', '').replace('-', ''));
            }
        } else {
            // If data.change is not a valid string, default to 0
            changeValue = 0;
        }
        
        // Ensure we have a valid number
        if (isNaN(changeValue)) changeValue = 0;
        
        const changePrefix = priceDirection === 'up' ? '+' : priceDirection === 'down' ? '-' : '';
        formattedChange = `${changePrefix}${Math.abs(changeValue).toFixed(2)}%`;
        
        // Set the correct class based on actual price direction
        // Force color based on the actual change value, not just the price direction
        const actualChangeValue = changeValue || 0;
        const changeClass = actualChangeValue > 0 ? 'text-success' : 
                           actualChangeValue < 0 ? 'text-danger' : 'text-muted';
        
        // Update change percentage
        if (changeElement) {
            changeElement.textContent = formattedChange;
            // Ensure we completely replace the class list to avoid any previous classes lingering
            changeElement.className = '';
            changeElement.classList.add('stock-change', changeClass);
            console.log(`Updated ${symbol} change element with class: ${changeClass}, value: ${formattedChange}`);
        }
        
        // Also update modal if it's open
        if (currentOpenModal === symbol) {
            // Find the index of this symbol in the stockData object
            const symbolIndex = Object.keys(stockData).indexOf(symbol) + 1;
            
            // Update modal elements
            const modalPriceElement = document.querySelector(`#chartModal${symbolIndex} .modal-price`);
            const modalChangeElement = document.querySelector(`#chartModal${symbolIndex} .modal-change`);
            
            if (modalPriceElement) {
                modalPriceElement.textContent = formattedPrice;
                modalPriceElement.setAttribute('data-price-usd', currentPrice.toString());
                modalPriceElement.classList.remove('text-success', 'text-danger');
                modalPriceElement.classList.add(priceDirection === 'up' ? 'text-success' : priceDirection === 'down' ? 'text-danger' : '');
            }
            
            if (modalChangeElement) {
                modalChangeElement.textContent = formattedChange;
                // Ensure we completely replace the class list to avoid any previous classes lingering
                modalChangeElement.className = '';
                modalChangeElement.classList.add('modal-change', changeClass);
                console.log(`Updated modal ${symbol} change element with class: ${changeClass}, value: ${formattedChange}`);
            }
            
            // Update buy/sell form price display if it exists
            const buyPriceElement = document.querySelector(`#chartModal${symbolIndex} .buy-price`);
            const sellPriceElement = document.querySelector(`#chartModal${symbolIndex} .sell-price`);
            
            if (buyPriceElement) {
                buyPriceElement.textContent = formattedPrice;
                buyPriceElement.setAttribute('data-price-usd', currentPrice.toString());
            }
            
            if (sellPriceElement) {
                sellPriceElement.textContent = formattedPrice;
                sellPriceElement.setAttribute('data-price-usd', currentPrice.toString());
            }
            
            // Update chart if it exists
            const chartInstance = chartInstances[`chart${symbolIndex}`];
            if (chartInstance) {
                updateChart(chartInstance, symbol, currentPrice);
            }
        }
    }
    
    // Initialize stock data from the page
    function initializeStockData() {
        const stockItems = document.querySelectorAll('.stock-item');
        stockItems.forEach(item => {
            const symbol = item.getAttribute('data-symbol');
            const priceElement = item.querySelector('.stock-price');
            const changeElement = item.querySelector('.stock-change');
            
            if (symbol && priceElement) {
                stockData[symbol] = {
                    price: priceElement.textContent,
                    change: changeElement ? changeElement.textContent : '',
                    changeClass: changeElement ? changeElement.className.replace('stock-change', '').trim() : '',
                    chartData: [] // Will be populated when modal opens
                };
                
                previousPrices[symbol] = parseFloat(priceElement.textContent.replace('$', '').replace(',', ''));
            }
        });
    }
    
    // Set up modal event listeners
    function setupModalListeners() {
        const modals = document.querySelectorAll('.modal');
        modals.forEach(modal => {
            const symbol = modal.getAttribute('data-symbol');
            const modalId = modal.id;
            const chartId = modalId.replace('Modal', '');
            
            modal.addEventListener('shown.bs.modal', function() {
                currentOpenModal = symbol;
                // Fetch fresh data for the chart
                fetchStockData(symbol, chartId);
            });
            
            modal.addEventListener('hidden.bs.modal', function() {
                currentOpenModal = null;
            });
        });
    }
    
    // Function to fetch stock data for a specific symbol
    function fetchStockData(symbol, chartId) {
        fetch('/Home/GetStockUpdates')
            .then(response => response.json())
            .then(data => {
                if (data.success && data.stocks) {
                    const stockInfo = data.stocks.find(s => s.symbol === symbol);
                    if (stockInfo) {
                        // Update stored data
                        stockData[symbol] = {
                            price: stockInfo.price,
                            change: stockInfo.change,
                            changeClass: stockInfo.changeClass,
                            chartData: stockInfo.chartData
                        };
                        
                        // Store the price for future mock data generation
                        previousPrices[symbol] = parseFloat(stockInfo.price.replace('$', '').replace(',', ''));
                        
                        // Update UI
                        updateStockUI(symbol, stockData[symbol]);
                        
                        // Create or update chart
                        if (!chartInstances[chartId]) {
                            chartInstances[chartId] = createChart(chartId, stockData[symbol].chartData);
                        } else {
                            updateChart(chartInstances[chartId], stockData[symbol].chartData);
                        }
                    }
                }
            })
            .catch(error => {
                console.error('Error fetching stock data:', error);
                // Generate mock data if API fails
                generateMockData(symbol, chartId);
            });
    }
    
    // Function to fetch real-time stock updates for all stocks
    function fetchStockUpdates() {
        fetch('/Home/GetStockUpdates')
            .then(response => response.json())
            .then(data => {
                if (data.success && data.stocks) {
                    data.stocks.forEach(stock => {
                        if (stockData[stock.symbol]) {
                            // Parse the new price for tracking purposes
                            const newPrice = parseFloat(stock.price.replace('$', '').replace(',', ''));
                            
                            // Get the open price from the backend
                            const openPrice = stock.open_price ? parseFloat(stock.open_price) : null;
                            
                            // Calculate the percentage change if we have both prices
                            let changeValue, changeClass;
                            if (openPrice && !isNaN(openPrice) && openPrice > 0) {
                                // Store the open price for reference
                                setBasePrice(stock.symbol, openPrice);
                                
                                // Calculate actual difference and percentage
                                const diff = newPrice - openPrice;
                                const percentChange = (diff / openPrice) * 100;
                                
                                // Format the change string with dollar amount and percentage
                                changeValue = `${diff >= 0 ? "+" : "-"}$${Math.abs(diff).toFixed(2)} (${diff >= 0 ? "+" : "-"}${Math.abs(percentChange).toFixed(2)}%)`;
                                changeClass = diff >= 0 ? 'text-success' : 'text-danger';
                                
                                // Debug logging
                                console.log(`[API] ${stock.symbol}: price=${newPrice}, openPrice=${openPrice}, diff=${diff}, percentChange=${percentChange.toFixed(2)}%, changeValue=${changeValue}`);
                            } else {
                                // Fallback to backend values if calculation fails
                                changeValue = stock.change;
                                changeClass = stock.changeClass;
                                console.log(`[API] ${stock.symbol}: Using backend values - price=${stock.price}, change=${changeValue}`);
                            }
                            
                            // Store the current price for future reference
                            previousPrices[stock.symbol] = newPrice;
                            
                            // Update stored data
                            stockData[stock.symbol] = {
                                price: stock.price,
                                priceUsd: stock.priceUsd, // Store the raw USD price for currency conversion
                                change: changeValue,
                                changeClass: changeClass,
                                chartData: stock.chartData
                            };
                            
                            // Store the price for future mock data generation
                            previousPrices[stock.symbol] = newPrice;
                            
                            // Update UI
                            updateStockUI(stock.symbol, stockData[stock.symbol]);
                            
                            // Update chart if this stock's modal is open
                            if (currentOpenModal === stock.symbol) {
                                const modalElement = document.querySelector(`.modal[data-symbol="${stock.symbol}"]`);
                                if (modalElement) {
                                    const chartId = modalElement.id.replace('Modal', '');
                                    if (chartInstances[chartId]) {
                                        updateChart(chartInstances[chartId], stock.chartData);
                                    }
                                }
                            }
                        }
                    });
                }
            })
            .catch(error => {
                console.error('Error fetching stock updates:', error);
                // Generate mock data updates if API fails
                generateMockUpdates();
            })
            .finally(() => {
                // Schedule next update only if not stopped
                if (updateInterval !== null) {
                    updateInterval = setTimeout(fetchStockUpdates, 6000);
                }
            });
    }
    
    // Function to generate mock data for a specific stock
    function generateMockData(symbol, chartId) {
        // Generate random price change (±10 from recent price)
        const currentPrice = previousPrices[symbol] || 100;
        const variation = (Math.random() * 20) - 10; // ±10 variation
        const newPrice = Math.max(currentPrice + variation, 1); // Ensure price stays positive
        const formattedPrice = `$${newPrice.toFixed(2)}`;
        
        // Generate change percentage
        const percentChange = (variation / currentPrice * 100);
        // Format with consistent style: +$1.23 (+4.56%) or -$1.23 (-4.56%)
        const changeValue = `${variation >= 0 ? '+' : '-'}$${Math.abs(variation).toFixed(2)} (${variation >= 0 ? '+' : '-'}${Math.abs(percentChange).toFixed(2)}%)`;
        const changeClass = variation >= 0 ? 'text-success' : 'text-danger';
        
        // Generate mock chart data
        const mockChartData = [];
        const colors = [variation >= 0 ? '#4ade80' : '#ef4444'];
        const dataPoints = [];
        
        // Generate 7 data points for the last 7 seconds
        for (let i = 0; i < 7; i++) {
            const pointVariation = (Math.random() * 20) - 10;
            dataPoints.push(Math.max(currentPrice + pointVariation, 1));
        }
        
        mockChartData.push({
            label: symbol,
            data: dataPoints,
            borderColor: colors[0],
            borderWidth: 2
        });
        
        // Update stock data
        stockData[symbol] = {
            ...stockData[symbol],
            price: formattedPrice,
            change: changeValue,
            changeClass: changeClass,
            chartData: mockChartData
        };
        
        // Store the price for future mock data generation
        previousPrices[symbol] = newPrice;
        
        // Update UI
        updateStockUI(symbol, stockData[symbol]);
        
        // Create or update chart
        if (!chartInstances[chartId]) {
            chartInstances[chartId] = createChart(chartId, mockChartData);
        } else {
            updateChart(chartInstances[chartId], mockChartData);
        }
    }
    
    // Function to generate mock updates for all stocks
    function generateMockUpdates() {
        console.log('Generating mock updates for all stocks');
        Object.keys(stockData).forEach(symbol => {
            // Get previous price or use a default
            const prevPrice = previousPrices[symbol] || 100;
            
            // Generate new price with small random change
            const changePercent = (Math.random() * 2 - 1) * 0.5; // -0.5% to +0.5%
            const newPrice = Math.max(prevPrice * (1 + changePercent / 100), 0.01);
            const formattedPrice = '$' + newPrice.toFixed(2);
            
            // Calculate change percentage
            const basePrice = getBasePrice(symbol) || prevPrice;
            const changeFromBase = ((newPrice - basePrice) / basePrice) * 100;
            const changeValue = (changeFromBase >= 0 ? '+' : '') + changeFromBase.toFixed(2) + '%';
            const changeClass = changeFromBase >= 0 ? 'text-success' : 'text-danger';
            
            // Generate mock chart data
            const mockChartData = [];
            const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444'];
            
            // Create data points with some randomness but following a trend
            const dataPoints = [];
            const trend = Math.random() > 0.5 ? 1 : -1; // Random trend direction
            
            // Get base price for this symbol or set it if not exists
            if (!basePrices[symbol]) {
                setBasePrice(symbol, prevPrice);
            }
            
            // Generate 7 data points for the chart
            for (let i = 0; i < 7; i++) {
                if (i === 0) {
                    // First point is the previous price
                    dataPoints.push(prevPrice);
                } else {
                    // Subsequent points follow the trend with some randomness
                    const lastPoint = dataPoints[i - 1];
                    const randomFactor = Math.random() * 0.5 + 0.75; // 0.75 to 1.25
                    const pointChange = (Math.random() * 0.5) * trend * randomFactor;
                    const pointVariation = lastPoint * (1 + pointChange / 100);
                    dataPoints.push(Math.max(pointVariation, 1));
                }
            }
            
            mockChartData.push({
                label: symbol,
                data: dataPoints,
                borderColor: colors[0],
                borderWidth: 2
            });
            
            // Update stock data
            stockData[symbol] = {
                ...stockData[symbol],
                price: formattedPrice,
                change: changeValue,
                changeClass: changeClass,
                chartData: mockChartData
            };
            
            // Store the price for future mock data generation
            previousPrices[symbol] = newPrice;
            
            // Update UI
            updateStockUI(symbol, stockData[symbol]);
            
            // Update chart if this stock's modal is open
            if (currentOpenModal === symbol) {
                const modalElement = document.querySelector(`.modal[data-symbol="${symbol}"]`);
                if (modalElement) {
                    const chartId = modalElement.id.replace('Modal', '');
                    if (chartInstances[chartId]) {
                        updateChart(chartInstances[chartId], mockChartData);
                    }
                }
            }
        });
    }
    
    // Initialize the page
    initializeStockData();
    setupModalListeners();
    
    // Functions to start and stop updates
    function startUpdates() {
        if (updateInterval === null) {
            console.log('Starting stock updates');
            // Clear any existing timeout to be safe
            clearTimeout(updateInterval);
            // Set the flag and start updates
            updateInterval = setTimeout(fetchStockUpdates, 100);
        }
    }
    
    function stopUpdates() {
        console.log('Stopping stock updates');
        clearTimeout(updateInterval);
        updateInterval = null;
    }
    
    // Start fetching updates immediately
    startUpdates();
    
    // Functions for quantity increment/decrement buttons
    function increment(id) {
        const input = document.getElementById(id);
        if (input) {
            const currentValue = parseInt(input.value) || 1;
            input.value = currentValue + 1;
        }
    }
    
    function decrement(id) {
        const input = document.getElementById(id);
        if (input) {
            const currentValue = parseInt(input.value) || 1;
            if (currentValue > 1) {
                input.value = currentValue - 1;
            }
        }
    }
    
    // Expose functions to global scope
    window.increment = increment;
    window.decrement = decrement;
    
    // Function to filter stocks based on search input
    function filterStocks() {
        const searchTerm = document.getElementById('stockSearch').value.toLowerCase();
        const stockItems = document.querySelectorAll('.stock-item');
        let visibleCount = 0;
        
        stockItems.forEach(item => {
            const stockName = item.querySelector('.stock-name').textContent.toLowerCase();
            const stockSymbol = item.getAttribute('data-symbol').toLowerCase();
            const stockSector = item.querySelector('.sector-badge-pill') ? 
                               item.querySelector('.sector-badge-pill').textContent.toLowerCase() : '';
            const stockCompanyName = item.querySelector('.stock-sector') ? 
                                    item.querySelector('.stock-sector').textContent.toLowerCase() : '';
            
            // Check if the search term matches any of the stock properties
            const matches = stockName.includes(searchTerm) || 
                          stockSymbol.includes(searchTerm) || 
                          stockSector.includes(searchTerm) || 
                          stockCompanyName.includes(searchTerm);
            
            // Show or hide the stock item based on the search result
            if (matches) {
                item.style.display = '';
                visibleCount++;
            } else {
                item.style.display = 'none';
            }
        });
        
        // Update the stocks title to show the number of visible stocks
        const stocksTitle = document.getElementById('stocksTitle');
        if (stocksTitle) {
            if (searchTerm) {
                stocksTitle.textContent = `Showing ${visibleCount} matching stocks`;
            } else {
                stocksTitle.textContent = 'All Stocks';
            }
        }
    }
    
    // Expose functions to global scope for debugging
    window.stocksApp = {
        chartInstances,
        stockData,
        previousPrices,
        basePrices,
        updateChart,
        createChart,
        fetchStockData,
        generateMockData,
        startUpdates,
        stopUpdates,
        filterStocks
    };
});