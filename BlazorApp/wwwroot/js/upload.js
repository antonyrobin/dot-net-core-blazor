window.uploadToAzureWithProgress = async (file, sasUrl, dotNetHelper) => {
    try {
        const response = await fetch(sasUrl, {
            method: 'PUT',
            body: file,
            headers: {
                'x-ms-blob-type': 'BlockBlob',
                'Content-Type': file.type || 'application/octet-stream'
            }
        });

        if (!response.ok) {
            throw new Error(`Upload failed: ${response.statusText}`);
        }

        await dotNetHelper.invokeMethodAsync('OnUploadSuccess');
        return true;
    } catch (err) {
        console.error(err);
        await dotNetHelper.invokeMethodAsync('OnUploadError', err.message);
        return false;
    }
};