output "zone_id" {
  value = data.cloudflare_zone.site.id
}

output "zone_status" {
  value = data.cloudflare_zone.site.status
}

output "site_url" {
  value = "https://${cloudflare_workers_custom_domain.marketing.hostname}"
}

output "game_url" {
  value = "https://${cloudflare_workers_custom_domain.game.hostname}"
}