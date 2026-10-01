# Wrangler and vinext deploy Worker code. Terraform owns which hostname serves each one.
# friendscorner-web and friendscorner-website must already exist before this applies.
# The API Worker gets no hostname; it is reachable only through the web Worker's binding.
# The game moves off the apex first so the marketing site can take friendscorner.app.
resource "cloudflare_workers_custom_domain" "game" {
  account_id = var.account_id
  zone_id    = data.cloudflare_zone.site.id
  hostname   = "play.${var.domain}"
  service    = var.worker_name
}

resource "cloudflare_workers_custom_domain" "marketing" {
  account_id = var.account_id
  zone_id    = data.cloudflare_zone.site.id
  hostname   = var.domain
  service    = var.marketing_worker_name

  depends_on = [cloudflare_workers_custom_domain.game]
}

moved {
  from = cloudflare_workers_custom_domain.site
  to   = cloudflare_workers_custom_domain.game
}
