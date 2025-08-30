// JavaScript interop functions for OrchestratorChat

window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy text: ', err);
        return false;
    }
};

window.downloadFile = (filename, content, contentType = 'text/plain') => {
    const blob = new Blob([content], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};

window.highlightCode = (element) => {
    if (element && typeof hljs !== 'undefined') {
        const codeBlocks = element.querySelectorAll('pre code');
        codeBlocks.forEach(block => {
            hljs.highlightElement(block);
        });
    }
};

window.playNotificationSound = () => {
    try {
        const audio = new Audio('/sounds/notification.mp3');
        audio.play().catch(e => console.log('Could not play notification sound:', e));
    } catch (e) {
        console.log('Notification sound not available:', e);
    }
};

window.setFocus = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.focus();
    }
};

window.getElementBounds = (element) => {
    if (element) {
        const rect = element.getBoundingClientRect();
        return {
            left: rect.left,
            top: rect.top,
            width: rect.width,
            height: rect.height
        };
    }
    return null;
};

window.observeElementResize = (element, dotNetObject, methodName) => {
    if (window.ResizeObserver) {
        const observer = new ResizeObserver(entries => {
            for (let entry of entries) {
                const { width, height } = entry.contentRect;
                dotNetObject.invokeMethodAsync(methodName, width, height);
            }
        });
        
        observer.observe(element);
        
        // Return cleanup function
        return () => observer.disconnect();
    }
};

window.preventDefaultDrop = () => {
    document.addEventListener('dragover', (e) => {
        e.preventDefault();
    });
    
    document.addEventListener('drop', (e) => {
        e.preventDefault();
    });
};

window.setupDragAndDrop = (element, dotNetObject, methodName) => {
    if (!element) return;
    
    element.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
        element.classList.add('drag-over');
    });
    
    element.addEventListener('dragleave', (e) => {
        e.preventDefault();
        e.stopPropagation();
        element.classList.remove('drag-over');
    });
    
    element.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        element.classList.remove('drag-over');
        
        const files = Array.from(e.dataTransfer.files);
        const fileData = [];
        
        for (let file of files) {
            const data = {
                name: file.name,
                size: file.size,
                type: file.type,
                content: await readFileAsBase64(file)
            };
            fileData.push(data);
        }
        
        dotNetObject.invokeMethodAsync(methodName, fileData);
    });
};

function readFileAsBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

// Auto-resize textarea
window.autoResizeTextarea = (element) => {
    if (element) {
        element.style.height = 'auto';
        element.style.height = element.scrollHeight + 'px';
    }
};

// LocalStorage helpers
window.setLocalStorageItem = (key, value) => {
    try {
        localStorage.setItem(key, JSON.stringify(value));
        return true;
    } catch (e) {
        console.error('Error saving to localStorage:', e);
        return false;
    }
};

window.getLocalStorageItem = (key) => {
    try {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : null;
    } catch (e) {
        console.error('Error reading from localStorage:', e);
        return null;
    }
};

window.removeLocalStorageItem = (key) => {
    try {
        localStorage.removeItem(key);
        return true;
    } catch (e) {
        console.error('Error removing from localStorage:', e);
        return false;
    }
};

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    // Prevent default drag and drop behavior
    window.preventDefaultDrop();
    
    // Set up keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        // Ctrl+/ or Cmd+/ to focus search
        if ((e.ctrlKey || e.metaKey) && e.key === '/') {
            e.preventDefault();
            const searchInput = document.querySelector('[placeholder*="search" i]');
            if (searchInput) {
                searchInput.focus();
            }
        }
        
        // Escape to clear focus
        if (e.key === 'Escape') {
            document.activeElement?.blur();
        }
    });
});

// Error handling
window.addEventListener('error', (e) => {
    console.error('JavaScript error:', e.error);
});

window.addEventListener('unhandledrejection', (e) => {
    console.error('Unhandled promise rejection:', e.reason);
});