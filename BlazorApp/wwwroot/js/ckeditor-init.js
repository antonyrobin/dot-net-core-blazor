window.initCKEditor = function (elementId) {
    const {
        ClassicEditor,
        Essentials,
        Bold,
        Italic,
        Font,
        Paragraph
    } = CKEDITOR;
    ClassicEditor
        .create(document.getElementById(elementId), {
            licenseKey: 'eyJhbGciOiJFUzI1NiJ9.eyJleHAiOjE3NzQ3NDIzOTksImp0aSI6IjAyMDUwMTgyLWUyZDctNDQ5Ni05YTQ2LTMyOWY3MGE5MjdlNSIsInVzYWdlRW5kcG9pbnQiOiJodHRwczovL3Byb3h5LWV2ZW50LmNrZWRpdG9yLmNvbSIsImRpc3RyaWJ1dGlvbkNoYW5uZWwiOlsiY2xvdWQiLCJkcnVwYWwiLCJzaCJdLCJ3aGl0ZUxhYmVsIjp0cnVlLCJsaWNlbnNlVHlwZSI6InRyaWFsIiwiZmVhdHVyZXMiOlsiKiJdLCJ2YyI6Ijg0MzBkZTBiIn0.Czf4Ap4DkkdoWJ2QaRIXb5vo-crha9SCPF0Guh6ZJ7mo4ss6A9KjRUXhNLMUgOVfhsHDeyuI7Vm9qf7SqcnH1w',
            toolbar: [
                'heading', '|',
                'bold', 'italic', 'link', 'bulletedList', 'numberedList',
                'insertTable', 'imageUpload', 'mediaEmbed', '|',
                'undo', 'redo'
            ],
            // Allow images, tables, etc.
            table: {
                contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells']
            },
            image: {
                toolbar: ['imageTextAlternative', 'imageStyle:inline', 'imageStyle:block', 'imageStyle:side']
            }
        })
        .catch(error => {
            console.error('CKEditor error:', error);
        });
};