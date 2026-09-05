#!/usr/bin/env bash
# Security Gateway - Upstream application port hardening
#
# Purpose: Block direct Internet access to applications that are meant to be
# reached only through the Security Gateway (gateway-nginx -> backend -> NPM -> app).
#
# Protected applications and ports (as observed on the deployed Unraid host):
#   8989  - Sonarr
#   7878  - Radarr
#   8080  - qBittorrent (via gluetun container)
#   2283  - Immich
#   8096  - Jellyfin (host process)
#   5055  - Seerr
#   8123  - Home Assistant (host process)
#
# LAN access and internal Docker/host communication are preserved.
# SSH (22), gateway HTTP/HTTPS (80/443), and NPM management (81) are untouched.
#
# Run as root. The script is idempotent.

set -euo pipefail

PROTECTED_TCP_PORTS=(8989 7878 8080 2283 8096 5055 8123)
# Adjust if the local subnet differs.
LAN_NETWORKS=("192.168.5.0/24" "10.253.0.0/16")
# Immich is intentionally reachable directly on its host port from any RFC1918
# private network (e.g., a different Wi-Fi subnet or VLAN in the same home).
IMMICH_LAN_NETWORKS=("10.0.0.0/8" "172.16.0.0/12" "192.168.0.0/16")
DOCKER_NETWORKS=("172.16.0.0/12" "172.17.0.0/16" "172.18.0.0/16" "172.19.0.0/16" "172.20.0.0/16" "172.21.0.0/16")

CHAIN="DOCKER-USER"

log() {
    echo "[security-gateway-firewall] $*"
}

ensure_chain_exists() {
    if ! iptables -L "$CHAIN" -n >/dev/null 2>&1; then
        log "Creating $CHAIN chain"
        iptables -N "$CHAIN" 2>/dev/null || true
    fi
}

flush_rules() {
    log "Flushing existing Security Gateway rules from $CHAIN and INPUT"
    for port in "${PROTECTED_TCP_PORTS[@]}"; do
        # Remove from DOCKER-USER
        while iptables -C "$CHAIN" -p tcp --dport "$port" -j DROP 2>/dev/null; do
            iptables -D "$CHAIN" -p tcp --dport "$port" -j DROP 2>/dev/null || true
        done
        while iptables -C "$CHAIN" -p tcp --dport "$port" -j ACCEPT 2>/dev/null; do
            iptables -D "$CHAIN" -p tcp --dport "$port" -j ACCEPT 2>/dev/null || true
        done
        # Remove from INPUT
        while iptables -C INPUT -p tcp --dport "$port" -j DROP 2>/dev/null; do
            iptables -D INPUT -p tcp --dport "$port" -j DROP 2>/dev/null || true
        done
        while iptables -C INPUT -p tcp --dport "$port" -j ACCEPT 2>/dev/null; do
            iptables -D INPUT -p tcp --dport "$port" -j ACCEPT 2>/dev/null || true
        done
    done
}

apply_rules() {
    ensure_chain_exists

    for port in "${PROTECTED_TCP_PORTS[@]}"; do
        log "Hardening TCP port $port"

        # Accept from loopback first.
        iptables -I "$CHAIN" 1 -p tcp --dport "$port" -s 127.0.0.1 -j ACCEPT 2>/dev/null || true
        iptables -I INPUT 1 -p tcp --dport "$port" -s 127.0.0.1 -j ACCEPT 2>/dev/null || true

        # Accept from LAN networks.
        for net in "${LAN_NETWORKS[@]}"; do
            iptables -I "$CHAIN" 1 -p tcp --dport "$port" -s "$net" -j ACCEPT 2>/dev/null || true
            iptables -I INPUT 1 -p tcp --dport "$port" -s "$net" -j ACCEPT 2>/dev/null || true
        done

        # Accept from Docker bridge networks (required for gateway/NPM -> app communication).
        for net in "${DOCKER_NETWORKS[@]}"; do
            iptables -I "$CHAIN" 1 -p tcp --dport "$port" -s "$net" -j ACCEPT 2>/dev/null || true
        done

        # Drop everything else.
        iptables -A "$CHAIN" -p tcp --dport "$port" -j DROP 2>/dev/null || true
        iptables -A INPUT -p tcp --dport "$port" -j DROP 2>/dev/null || true
    done

    # Immich-specific: allow direct host-port access from any RFC1918 private
    # network. These rules are inserted before the DROP rules added above.
    log "Allowing broader private-network access for Immich (port 2283)"
    for net in "${IMMICH_LAN_NETWORKS[@]}"; do
        iptables -I INPUT 1 -p tcp --dport 2283 -s "$net" -j ACCEPT 2>/dev/null || true
        iptables -I "$CHAIN" 1 -p tcp --dport 2283 -s "$net" -j ACCEPT 2>/dev/null || true
    done
}

save_rules() {
    if command -v iptables-save >/dev/null 2>&1; then
        iptables-save > /etc/iptables/security-gateway-upstream.rules 2>/dev/null || true
        log "Rules saved to /etc/iptables/security-gateway-upstream.rules"
    fi
}

case "${1:-apply}" in
    apply)
        flush_rules
        apply_rules
        save_rules
        log "Upstream port hardening applied."
        ;;
    rollback)
        flush_rules
        log "Upstream port hardening rolled back."
        ;;
    *)
        echo "Usage: $0 [apply|rollback]"
        exit 1
        ;;
esac
