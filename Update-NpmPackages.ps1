$null = & npm install -g npm-check-updates@latest
Push-Location './Web/ClientApp'
try {
    & npm-check-updates -u
    & npm install --audit false
    & npm audit --production
} finally {
    Pop-Location
}
