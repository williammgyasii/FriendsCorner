output "zone_id" {
  value = data.cloudflare_zone.site.id
}

output "zone_status" {
  value = data.cloudflare_zone.site.status
}
