const fs = require('fs');
const path = require('path');

function copyFolderRecursiveSync(source, target) {
    if (!fs.existsSync(target)) {
        fs.mkdirSync(target, { recursive: true });
    }

    if (fs.lstatSync(source).isDirectory()) {
        const files = fs.readdirSync(source);
        files.forEach((file) => {
            const curSource = path.join(source, file);
            const curTarget = path.join(target, file);
            if (fs.lstatSync(curSource).isDirectory()) {
                copyFolderRecursiveSync(curSource, curTarget);
            } else {
                fs.copyFileSync(curSource, curTarget);
            }
        });
    }
}

try {
    // Sync unit tests
    console.log('Syncing unit tests...');
    copyFolderRecursiveSync(
        path.join(__dirname, '../tests/frontend/unit'),
        path.join(__dirname, 'src/tests')
    );

    // Sync E2E tests
    console.log('Syncing E2E tests...');
    copyFolderRecursiveSync(
        path.join(__dirname, '../tests/frontend/e2e'),
        path.join(__dirname, 'test/e2e')
    );
    console.log('Sync complete!');
} catch (err) {
    console.error('Error syncing tests:', err.message);
    process.exit(1);
}
