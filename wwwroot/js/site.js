// Power BI Explorer - JavaScript

// Global state
let workspaces = [];
let recentRequests = [];

// Navigation
document.querySelectorAll('.nav-item').forEach(item => {
    item.addEventListener('click', function() {
        const section = this.dataset.section;
        navigateTo(section);
    });
});

function navigateTo(section) {
    // Update nav items
    document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
    document.querySelector(`[data-section="${section}"]`)?.classList.add('active');
    
    // Update sections
    document.querySelectorAll('.section').forEach(s => s.style.display = 'none');
    const targetSection = document.getElementById(`section-${section}`);
    if (targetSection) {
        targetSection.style.display = 'block';
        targetSection.classList.add('fade-in');
    }
}

// Toast notifications
function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    
    const icon = type === 'success' ? 'check-circle' : 
                 type === 'error' ? 'times-circle' : 'info-circle';
    
    toast.innerHTML = `
        <i class="fas fa-${icon}" style="color: var(--${type === 'info' ? 'info' : type})"></i>
        <span>${message}</span>
    `;
    
    container.appendChild(toast);
    
    setTimeout(() => {
        toast.style.animation = 'slideIn 0.3s ease reverse';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// API Helpers
async function apiCall(method, url, body = null) {
    const startTime = performance.now();
    
    try {
        const options = {
            method,
            headers: {
                'Content-Type': 'application/json'
            }
        };
        
        if (body) {
            options.body = JSON.stringify(body);
        }
        
        const response = await fetch(url, options);
        const data = await response.json();
        const endTime = performance.now();
        
        // Add to recent requests
        addRecentRequest(method, url, response.status, endTime - startTime);
        
        return { 
            success: response.ok, 
            data, 
            status: response.status,
            time: Math.round(endTime - startTime)
        };
    } catch (error) {
        const endTime = performance.now();
        addRecentRequest(method, url, 0, endTime - startTime);
        return { 
            success: false, 
            error: error.message,
            time: Math.round(endTime - startTime)
        };
    }
}

function addRecentRequest(method, url, status, time) {
    recentRequests.unshift({
        method,
        url,
        status,
        time: Math.round(time),
        timestamp: new Date()
    });
    
    // Keep only last 10 requests
    if (recentRequests.length > 10) {
        recentRequests.pop();
    }
    
    updateRecentRequestsUI();
}

function updateRecentRequestsUI() {
    const container = document.getElementById('recentRequests');
    if (recentRequests.length === 0) return;
    
    container.innerHTML = `
        <table class="data-table">
            <thead>
                <tr>
                    <th>Metoda</th>
                    <th>URL</th>
                    <th>Status</th>
                    <th>Czas</th>
                    <th>Kiedy</th>
                </tr>
            </thead>
            <tbody>
                ${recentRequests.map(r => `
                    <tr>
                        <td><span class="method-badge method-${r.method.toLowerCase()}">${r.method}</span></td>
                        <td><code>${r.url}</code></td>
                        <td><span class="badge ${r.status >= 200 && r.status < 300 ? 'badge-success' : 'badge-error'}">${r.status || 'Error'}</span></td>
                        <td>${r.time}ms</td>
                        <td>${formatTime(r.timestamp)}</td>
                    </tr>
                `).join('')}
            </tbody>
        </table>
    `;
}

function formatTime(date) {
    return date.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

// Syntax highlighting for JSON
function syntaxHighlight(json) {
    if (typeof json !== 'string') {
        json = JSON.stringify(json, null, 2);
    }
    
    json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    
    return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
        let cls = 'json-number';
        if (/^"/.test(match)) {
            if (/:$/.test(match)) {
                cls = 'json-key';
            } else {
                cls = 'json-string';
            }
        } else if (/true|false/.test(match)) {
            cls = 'json-boolean';
        } else if (/null/.test(match)) {
            cls = 'json-null';
        }
        return '<span class="' + cls + '">' + match + '</span>';
    });
}

// Token functions
async function getToken() {
    showToast('Pobieranie tokena...', 'info');
    
    const result = await apiCall('GET', '/api/powerbi/token');
    
    if (result.success && result.data.success) {
        document.getElementById('tokenValue').value = result.data.accessToken;
        document.getElementById('tokenExpiry').value = new Date(result.data.expiresOn).toLocaleString('pl-PL');
        document.getElementById('tokenStatus').className = 'badge badge-success';
        document.getElementById('tokenStatus').textContent = 'Aktywny';
        
        // Update connection status
        updateConnectionStatus(true);
        
        showToast('Token pobrany pomyślnie!', 'success');
    } else {
        document.getElementById('tokenStatus').className = 'badge badge-error';
        document.getElementById('tokenStatus').textContent = 'Błąd';
        document.getElementById('tokenValue').value = result.data?.error || result.error || 'Nieznany błąd';
        
        updateConnectionStatus(false);
        
        showToast('Błąd pobierania tokena', 'error');
    }
}

function updateConnectionStatus(connected) {
    const status = document.getElementById('connectionStatus');
    const dot = status.querySelector('.connection-dot');
    const text = status.querySelector('span');
    
    if (connected) {
        dot.className = 'connection-dot connected';
        text.textContent = 'Połączono';
    } else {
        dot.className = 'connection-dot disconnected';
        text.textContent = 'Nie połączono';
    }
}

// Workspaces
async function loadWorkspaces() {
    const container = document.getElementById('workspacesTable');
    container.innerHTML = '<div class="loading"><div class="spinner"></div></div>';
    
    const result = await apiCall('GET', '/api/powerbi/workspaces');
    
    if (result.success && result.data.success) {
        workspaces = result.data.data;
        document.getElementById('workspaceBadge').textContent = result.data.count;
        document.getElementById('workspaceCount').textContent = result.data.count;
        
        // Update workspace selects
        updateWorkspaceSelects();
        
        if (workspaces.length === 0) {
            container.innerHTML = '<p style="color: var(--text-muted); text-align: center; padding: 2rem;">Brak workspace\'ów</p>';
        } else {
            container.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nazwa</th>
                            <th>ID</th>
                            <th>Typ</th>
                            <th>Dedicated Capacity</th>
                            <th>Akcje</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${workspaces.map(w => `
                            <tr>
                                <td>${w.name}</td>
                                <td><code>${w.id}</code></td>
                                <td>${w.type || '-'}</td>
                                <td><span class="badge ${w.isOnDedicatedCapacity ? 'badge-success' : 'badge-warning'}">${w.isOnDedicatedCapacity ? 'Tak' : 'Nie'}</span></td>
                                <td>
                                    <button class="icon-btn" onclick="copyToClipboard('${w.id}')" title="Kopiuj ID">
                                        <i class="fas fa-copy"></i>
                                    </button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        }
        
        updateConnectionStatus(true);
        showToast(`Załadowano ${result.data.count} workspace'ów`, 'success');
    } else {
        container.innerHTML = `<p style="color: var(--error); text-align: center; padding: 2rem;">${result.data?.error || result.error}</p>`;
        showToast('Błąd ładowania workspace\'ów', 'error');
    }
}

function updateWorkspaceSelects() {
    const selects = ['reportWorkspaceSelect', 'datasetWorkspaceSelect', 'dashboardWorkspaceSelect'];
    
    selects.forEach(selectId => {
        const select = document.getElementById(selectId);
        // Keep first two options
        const firstOptions = Array.from(select.options).slice(0, 2);
        select.innerHTML = '';
        firstOptions.forEach(opt => select.appendChild(opt));
        
        workspaces.forEach(w => {
            const option = document.createElement('option');
            option.value = w.id;
            option.textContent = w.name;
            select.appendChild(option);
        });
    });
}

// Reports
async function loadReports() {
    const workspaceId = document.getElementById('reportWorkspaceSelect').value;
    const container = document.getElementById('reportsTable');
    
    if (!workspaceId) {
        showToast('Wybierz workspace', 'warning');
        return;
    }
    
    container.innerHTML = '<div class="loading"><div class="spinner"></div></div>';
    
    const url = workspaceId === 'my' 
        ? '/api/powerbi/reports' 
        : `/api/powerbi/workspaces/${workspaceId}/reports`;
    
    const result = await apiCall('GET', url);
    
    if (result.success && result.data.success) {
        const reports = result.data.data;
        document.getElementById('reportBadge').textContent = result.data.count;
        document.getElementById('reportCount').textContent = result.data.count;
        
        if (reports.length === 0) {
            container.innerHTML = '<p style="color: var(--text-muted); text-align: center; padding: 2rem;">Brak raportów</p>';
        } else {
            container.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nazwa</th>
                            <th>ID</th>
                            <th>Typ</th>
                            <th>Dataset ID</th>
                            <th>Akcje</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${reports.map(r => `
                            <tr>
                                <td>${r.name}</td>
                                <td><code>${r.id}</code></td>
                                <td>${r.reportType || '-'}</td>
                                <td><code>${r.datasetId || '-'}</code></td>
                                <td>
                                    <button class="icon-btn" onclick="copyToClipboard('${r.id}')" title="Kopiuj ID">
                                        <i class="fas fa-copy"></i>
                                    </button>
                                    ${r.webUrl ? `<a href="${r.webUrl}" target="_blank" class="icon-btn" title="Otwórz"><i class="fas fa-external-link-alt"></i></a>` : ''}
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        }
        
        showToast(`Załadowano ${result.data.count} raportów`, 'success');
    } else {
        container.innerHTML = `<p style="color: var(--error); text-align: center; padding: 2rem;">${result.data?.error || result.error}</p>`;
        showToast('Błąd ładowania raportów', 'error');
    }
}

// Datasets
async function loadDatasets() {
    const workspaceId = document.getElementById('datasetWorkspaceSelect').value;
    const container = document.getElementById('datasetsTable');
    
    if (!workspaceId) {
        showToast('Wybierz workspace', 'warning');
        return;
    }
    
    container.innerHTML = '<div class="loading"><div class="spinner"></div></div>';
    
    const url = workspaceId === 'my' 
        ? '/api/powerbi/datasets' 
        : `/api/powerbi/workspaces/${workspaceId}/datasets`;
    
    const result = await apiCall('GET', url);
    
    if (result.success && result.data.success) {
        const datasets = result.data.data;
        document.getElementById('datasetBadge').textContent = result.data.count;
        document.getElementById('datasetCount').textContent = result.data.count;
        
        if (datasets.length === 0) {
            container.innerHTML = '<p style="color: var(--text-muted); text-align: center; padding: 2rem;">Brak datasetów</p>';
        } else {
            container.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nazwa</th>
                            <th>ID</th>
                            <th>Refreshable</th>
                            <th>Configured By</th>
                            <th>Akcje</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${datasets.map(d => `
                            <tr>
                                <td>${d.name}</td>
                                <td><code>${d.id}</code></td>
                                <td><span class="badge ${d.isRefreshable ? 'badge-success' : 'badge-warning'}">${d.isRefreshable ? 'Tak' : 'Nie'}</span></td>
                                <td>${d.configuredBy || '-'}</td>
                                <td>
                                    <button class="icon-btn" onclick="copyToClipboard('${d.id}')" title="Kopiuj ID">
                                        <i class="fas fa-copy"></i>
                                    </button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        }
        
        showToast(`Załadowano ${result.data.count} datasetów`, 'success');
    } else {
        container.innerHTML = `<p style="color: var(--error); text-align: center; padding: 2rem;">${result.data?.error || result.error}</p>`;
        showToast('Błąd ładowania datasetów', 'error');
    }
}

// Dashboards
async function loadDashboards() {
    const workspaceId = document.getElementById('dashboardWorkspaceSelect').value;
    const container = document.getElementById('dashboardsTable');
    
    if (!workspaceId) {
        showToast('Wybierz workspace', 'warning');
        return;
    }
    
    container.innerHTML = '<div class="loading"><div class="spinner"></div></div>';
    
    const url = workspaceId === 'my' 
        ? '/api/powerbi/dashboards' 
        : `/api/powerbi/workspaces/${workspaceId}/dashboards`;
    
    const result = await apiCall('GET', url);
    
    if (result.success && result.data.success) {
        const dashboards = result.data.data;
        document.getElementById('dashboardBadge').textContent = result.data.count;
        document.getElementById('dashboardCount').textContent = result.data.count;
        
        if (dashboards.length === 0) {
            container.innerHTML = '<p style="color: var(--text-muted); text-align: center; padding: 2rem;">Brak dashboardów</p>';
        } else {
            container.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nazwa</th>
                            <th>ID</th>
                            <th>Read Only</th>
                            <th>Akcje</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${dashboards.map(d => `
                            <tr>
                                <td>${d.displayName}</td>
                                <td><code>${d.id}</code></td>
                                <td><span class="badge ${d.isReadOnly ? 'badge-warning' : 'badge-success'}">${d.isReadOnly ? 'Tak' : 'Nie'}</span></td>
                                <td>
                                    <button class="icon-btn" onclick="copyToClipboard('${d.id}')" title="Kopiuj ID">
                                        <i class="fas fa-copy"></i>
                                    </button>
                                    ${d.webUrl ? `<a href="${d.webUrl}" target="_blank" class="icon-btn" title="Otwórz"><i class="fas fa-external-link-alt"></i></a>` : ''}
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        }
        
        showToast(`Załadowano ${result.data.count} dashboardów`, 'success');
    } else {
        container.innerHTML = `<p style="color: var(--error); text-align: center; padding: 2rem;">${result.data?.error || result.error}</p>`;
        showToast('Błąd ładowania dashboardów', 'error');
    }
}

// Capacities
async function loadCapacities() {
    const container = document.getElementById('capacitiesTable');
    container.innerHTML = '<div class="loading"><div class="spinner"></div></div>';
    
    const result = await apiCall('GET', '/api/powerbi/capacities');
    
    if (result.success && result.data.success) {
        const capacities = result.data.data;
        
        if (capacities.length === 0) {
            container.innerHTML = '<p style="color: var(--text-muted); text-align: center; padding: 2rem;">Brak capacities</p>';
        } else {
            container.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nazwa</th>
                            <th>ID</th>
                            <th>SKU</th>
                            <th>Stan</th>
                            <th>Region</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${capacities.map(c => `
                            <tr>
                                <td>${c.displayName}</td>
                                <td><code>${c.id}</code></td>
                                <td>${c.sku || '-'}</td>
                                <td><span class="badge ${c.state === 'Active' ? 'badge-success' : 'badge-warning'}">${c.state || '-'}</span></td>
                                <td>${c.region || '-'}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        }
        
        showToast(`Załadowano ${result.data.count} capacities`, 'success');
    } else {
        container.innerHTML = `<p style="color: var(--error); text-align: center; padding: 2rem;">${result.data?.error || result.error}</p>`;
        showToast('Błąd ładowania capacities', 'error');
    }
}

// Gateways
async function loadGateways() {
    const container = document.getElementById('gatewaysTable');
    container.innerHTML = '<div class="loading"><div class="spinner"></div></div>';
    
    const result = await apiCall('GET', '/api/powerbi/gateways');
    
    if (result.success && result.data.success) {
        const gateways = result.data.data;
        
        if (gateways.length === 0) {
            container.innerHTML = '<p style="color: var(--text-muted); text-align: center; padding: 2rem;">Brak gateways</p>';
        } else {
            container.innerHTML = `
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nazwa</th>
                            <th>ID</th>
                            <th>Typ</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${gateways.map(g => `
                            <tr>
                                <td>${g.name}</td>
                                <td><code>${g.id}</code></td>
                                <td>${g.type || '-'}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        }
        
        showToast(`Załadowano ${result.data.count} gateways`, 'success');
    } else {
        container.innerHTML = `<p style="color: var(--error); text-align: center; padding: 2rem;">${result.data?.error || result.error}</p>`;
        showToast('Błąd ładowania gateways', 'error');
    }
}

// Embed
async function getEmbedConfig() {
    const workspaceId = document.getElementById('embedWorkspaceId').value;
    const reportId = document.getElementById('embedReportId').value;
    const container = document.getElementById('embedResult');
    
    if (!workspaceId || !reportId) {
        showToast('Wprowadź Workspace ID i Report ID', 'warning');
        return;
    }
    
    container.innerHTML = '<div class="loading"><div class="spinner"></div></div>';
    
    const result = await apiCall('GET', `/api/powerbi/workspaces/${workspaceId}/reports/${reportId}/embed`);
    
    if (result.success && result.data.success) {
        const config = result.data.data;
        
        container.innerHTML = `
            <div style="padding: 1rem;">
                <div class="form-group">
                    <label>Report Name</label>
                    <input type="text" value="${config.reportName}" readonly>
                </div>
                <div class="form-group">
                    <label>Embed URL</label>
                    <input type="text" value="${config.embedUrl}" readonly>
                </div>
                <div class="form-group">
                    <label>Embed Token</label>
                    <textarea rows="4" readonly>${config.embedToken}</textarea>
                </div>
                <div class="form-group">
                    <label>Token Expiry</label>
                    <input type="text" value="${new Date(config.tokenExpiry).toLocaleString('pl-PL')}" readonly>
                </div>
                <button class="btn btn-secondary" onclick="copyEmbedCode()">
                    <i class="fas fa-copy"></i>
                    Kopiuj kod embed
                </button>
            </div>
        `;
        
        showToast('Embed config pobrany pomyślnie!', 'success');
    } else {
        container.innerHTML = `<p style="color: var(--error); text-align: center; padding: 2rem;">${result.data?.error || result.error}</p>`;
        showToast('Błąd pobierania embed config', 'error');
    }
}

// API Tester
function selectEndpoint(method, path, desc) {
    document.getElementById('apiMethod').value = method;
    document.getElementById('apiUrl').value = path;
    
    // Highlight selected endpoint
    document.querySelectorAll('.endpoint-item').forEach(item => item.classList.remove('selected'));
    event.currentTarget.classList.add('selected');
}

async function executeApiCall() {
    const method = document.getElementById('apiMethod').value;
    const url = document.getElementById('apiUrl').value;
    
    if (!url) {
        showToast('Wprowadź URL', 'warning');
        return;
    }
    
    // Update status
    const statusDot = document.getElementById('responseStatusDot');
    const statusText = document.getElementById('responseStatusText');
    const responseTime = document.getElementById('responseTime');
    const responseBody = document.getElementById('responseBody');
    
    statusDot.className = 'status-dot status-pending';
    statusText.textContent = 'Ładowanie...';
    responseBody.innerHTML = '// Ładowanie...';
    
    const result = await apiCall(method, url);
    
    if (result.success) {
        statusDot.className = 'status-dot status-success';
        statusText.textContent = `Status: ${result.status}`;
    } else {
        statusDot.className = 'status-dot status-error';
        statusText.textContent = result.error ? 'Błąd' : `Status: ${result.status}`;
    }
    
    responseTime.textContent = `${result.time}ms`;
    responseBody.innerHTML = syntaxHighlight(result.data || result.error);
}

// Refresh all
async function refreshAll() {
    showToast('Odświeżanie wszystkich danych...', 'info');
    
    await loadWorkspaces();
    
    showToast('Odświeżono wszystkie dane', 'success');
}

// Utilities
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        showToast('Skopiowano do schowka!', 'success');
    }).catch(() => {
        showToast('Błąd kopiowania', 'error');
    });
}

function copyEmbedCode() {
    const embedUrl = document.querySelector('#embedResult input[type="text"]:nth-of-type(2)').value;
    const embedToken = document.querySelector('#embedResult textarea').value;
    
    const code = `
// Power BI Embed Configuration
const config = {
    type: 'report',
    embedUrl: '${embedUrl}',
    accessToken: '${embedToken}',
    tokenType: 1, // Embed
    settings: {
        panes: {
            filters: { visible: true },
            pageNavigation: { visible: true }
        }
    }
};

// Initialize embed
powerbi.embed(container, config);
`;
    
    copyToClipboard(code);
}

// Initialize
document.addEventListener('DOMContentLoaded', function() {
    // Try to get token on load to check connection
    apiCall('GET', '/api/powerbi/token').then(result => {
        if (result.success && result.data.success) {
            updateConnectionStatus(true);
        }
    });
});
