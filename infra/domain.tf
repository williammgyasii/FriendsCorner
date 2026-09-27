# Wrangler deploys the Worker's code; Terraform owns which hostname serves it.
# The web Worker (edge/web/wrangler.jsonc) must exist before this applies.
# The API Worker gets no hostname; it is reachable only through the web Worker's binding.
resource "cloudflare_workers_custom_domain" "site" {
  account_id = var.account_id
  zone_id    = data.cloudflare_zone.site.id
  hostname   = var.domain
  service    = var.worker_name
}
