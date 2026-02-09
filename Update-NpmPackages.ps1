$null = & npm install -g npm-check-updates@latest
Push-Location './Web'
try {
    & npm-check-updates -u
    & npm install --audit false
    & npm audit --production
} finally {
    Pop-Location
}
