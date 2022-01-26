$null = & npm install -g npm-check-updates@latest
Push-Location './Web/ClientApp'
try {
    & ncu -u
    & npm install --audit false
    & npm audit --production
} finally {
    Pop-Location
}
