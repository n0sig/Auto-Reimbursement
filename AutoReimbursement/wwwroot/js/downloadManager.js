/**
 * Smart Download Manager
 * Handles file downloads with auto-compression logic:
 * - 3+ files: Compress into ZIP archive before downloading
 * - Less than 3 files: Download independently
 */

// JSZip will be loaded dynamically when needed
let jsZipLoaded = false;

/**
 * Dynamically loads JSZip library if not already loaded
 * @returns {Promise} Promise that resolves when JSZip is loaded
 */
async function ensureJSZipLoaded() {
    if (jsZipLoaded && window.JSZip) {
        return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        // Using JSZip 3.7.1 - a stable and widely used version
        script.src = 'https://unpkg.com/jszip@3.7.1/dist/jszip.min.js';
        script.crossOrigin = 'anonymous';
        script.onload = () => {
            jsZipLoaded = true;
            resolve();
        };
        script.onerror = () => reject(new Error('Failed to load JSZip library'));
        document.head.appendChild(script);
    });
}

/**
 * Downloads a single file from a byte array
 * @param {string} fileName - Name of the file to download
 * @param {Uint8Array} fileContent - File content as byte array
 * @param {string} contentType - MIME type of the file
 */
function downloadSingleFile(fileName, fileContent, contentType) {
    const blob = new Blob([fileContent], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

/**
 * Downloads multiple files independently
 * @param {Array} files - Array of file objects with fileName, content, and contentType
 */
async function downloadFilesIndependently(files) {
    for (const file of files) {
        downloadSingleFile(file.fileName, file.content, file.contentType);
        // Small delay between downloads to prevent browser blocking
        await new Promise(resolve => setTimeout(resolve, 200));
    }
}

/**
 * Creates a ZIP archive from multiple files and downloads it
 * @param {Array} files - Array of file objects with fileName and content
 * @param {string} archiveName - Name of the ZIP archive
 */
async function downloadAsZipArchive(files, archiveName) {
    await ensureJSZipLoaded();
    
    const zip = new JSZip();
    
    for (const file of files) {
        zip.file(file.fileName, file.content);
    }
    
    const content = await zip.generateAsync({ type: 'blob' });
    const url = URL.createObjectURL(content);
    const link = document.createElement('a');
    link.href = url;
    link.download = archiveName || 'documents.zip';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

/**
 * Smart download function that decides whether to compress or download independently
 * @param {Array} files - Array of file objects with fileName, content, and contentType
 * @param {string} archiveName - Name of the ZIP archive if compression is used
 * @param {number} compressionThreshold - Number of files threshold for compression (default: 3)
 */
window.smartDownload = async function(files, archiveName, compressionThreshold = 3) {
    if (!files || files.length === 0) {
        console.warn('No files provided for download');
        return;
    }

    try {
        if (files.length >= compressionThreshold) {
            await downloadAsZipArchive(files, archiveName);
        } else {
            await downloadFilesIndependently(files);
        }
    } catch (error) {
        console.error('Download failed:', error);
        throw error;
    }
};

/**
 * Downloads files from URLs with smart compression logic
 * @param {Array} fileInfos - Array of objects with url, fileName, and contentType
 * @param {string} archiveName - Name of the ZIP archive if compression is used
 * @param {number} compressionThreshold - Number of files threshold for compression (default: 3)
 */
window.smartDownloadFromUrls = async function(fileInfos, archiveName, compressionThreshold = 3) {
    if (!fileInfos || fileInfos.length === 0) {
        console.warn('No files provided for download');
        return;
    }

    try {
        // Fetch all files
        const files = await Promise.all(fileInfos.map(async (info) => {
            const response = await fetch(info.url);
            if (!response.ok) {
                throw new Error(`Failed to fetch ${info.fileName}: ${response.statusText}`);
            }
            const arrayBuffer = await response.arrayBuffer();
            return {
                fileName: info.fileName,
                content: new Uint8Array(arrayBuffer),
                contentType: info.contentType || response.headers.get('content-type') || 'application/octet-stream'
            };
        }));

        await window.smartDownload(files, archiveName, compressionThreshold);
    } catch (error) {
        console.error('Download from URLs failed:', error);
        throw error;
    }
};

/**
 * Downloads a single file from a URL
 * @param {string} url - URL of the file
 * @param {string} fileName - Name of the file to save as
 */
window.downloadFromUrl = async function(url, fileName) {
    try {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Failed to fetch file: ${response.statusText}`);
        }
        const blob = await response.blob();
        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = objectUrl;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(objectUrl);
    } catch (error) {
        console.error('Download from URL failed:', error);
        throw error;
    }
};

/**
 * Downloads a file from a base64 encoded string
 * @param {string} base64Content - Base64 encoded file content
 * @param {string} fileName - Name of the file to save as
 * @param {string} contentType - MIME type of the file
 */
window.downloadFromBase64 = function(base64Content, fileName, contentType) {
    try {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        downloadSingleFile(fileName, byteArray, contentType);
    } catch (error) {
        console.error('Download from base64 failed:', error);
        throw error;
    }
};

/**
 * Smart download from base64 encoded files with auto-compression
 * @param {Array} files - Array of objects with fileName, content (base64), and contentType
 * @param {string} archiveName - Name of the ZIP archive if compression is used
 * @param {number} compressionThreshold - Number of files threshold for compression (default: 3)
 */
window.smartDownloadFromBase64 = async function(files, archiveName, compressionThreshold = 3) {
    if (!files || files.length === 0) {
        console.warn('No files provided for download');
        return;
    }

    try {
        // Convert base64 to byte arrays
        const decodedFiles = files.map(file => {
            const byteCharacters = atob(file.content);
            const byteNumbers = new Array(byteCharacters.length);
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            return {
                fileName: file.fileName,
                content: new Uint8Array(byteNumbers),
                contentType: file.contentType
            };
        });

        await window.smartDownload(decodedFiles, archiveName, compressionThreshold);
    } catch (error) {
        console.error('Smart download from base64 failed:', error);
        throw error;
    }
};
